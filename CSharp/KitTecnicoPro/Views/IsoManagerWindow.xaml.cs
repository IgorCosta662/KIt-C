using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using KitTecnicoPro.Infrastructure;
using KitTecnicoPro.Models;
using KitTecnicoPro.Serialization;
using KitTecnicoPro.Services;

namespace KitTecnicoPro.Views
{
    public partial class IsoManagerWindow : Window
    {
        private readonly ResilientHttpService _http;
        private readonly bool _ownsHttp;
        private readonly IAppLogger? _logger;
        private readonly ObservableCollection<IsoItem> _allIsos;
        private CancellationTokenSource? _cts;
        private bool _isDownloading;

        public IsoManagerWindow() : this(null, null)
        {
        }

        public IsoManagerWindow(ResilientHttpService? http, IAppLogger? logger = null)
        {
            InitializeComponent();

            if (http != null)
            {
                _http = http;
                _ownsHttp = false;
            }
            else if (AppHost.IsInitialized && AppHost.Services.GetService(typeof(ResilientHttpService)) is ResilientHttpService hostHttp)
            {
                _http = hostHttp;
                _ownsHttp = false;
            }
            else
            {
                _http = new ResilientHttpService(logger);
                _ownsHttp = true;
            }

            _logger = logger ?? (AppHost.IsInitialized ? AppHost.Services.GetService(typeof(IAppLogger)) as IAppLogger : null);

            _allIsos = LoadIsoList();
            LstIsos.ItemsSource = _allIsos;
            CmbCategoria.SelectedIndex = 0;
            UpdateContagem();

            // Efeito Windows 11 Mica Glassmorphism + limpeza determinística
            this.SourceInitialized += (_, _) =>
            {
                KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this);
                this.Closed += (_, _) =>
                {
                    try { _cts?.Cancel(); } catch { }
                    _cts?.Dispose();
                    if (_ownsHttp) _http.Dispose();
                };
            };
        }

        // ── Persistência (System.Text.Json Source Generator) ──────────────────
        private ObservableCollection<IsoItem> LoadIsoList()
        {
            try
            {
                string path = AppPaths.GetDataFile("isos.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize(json, KitJsonContext.IndentedUtf8.ObservableCollectionIsoItem);
                    if (loaded != null && loaded.Count > 0)
                        return loaded;
                }
            }
            catch (Exception ex)
            {
                _logger?.Warn($"Falha ao ler isos.json, usando catálogo padrão: {ex.Message}", "IsoManager");
            }

            return BuildIsoList();
        }

        private void SaveIsoList()
        {
            try
            {
                string path = AppPaths.GetDataFile("isos.json");
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                string tmpPath = path + ".tmp";
                string json = JsonSerializer.Serialize(_allIsos, KitJsonContext.IndentedUtf8.ObservableCollectionIsoItem);
                File.WriteAllText(tmpPath, json);
                File.Move(tmpPath, path, overwrite: true);
            }
            catch (Exception ex)
            {
                _logger?.Warn($"Não foi possível persistir isos.json: {ex.Message}", "IsoManager");
            }
        }

        // ── Filtro ────────────────────────────────────────────────────────────
        private void OnCategoriaChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            if (LstIsos == null || _allIsos == null) return;

            string raw = (CmbCategoria.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
            int sp = raw.IndexOf(' ');
            string filtro = (sp > 0 && sp <= 4) ? raw.Substring(sp + 1).Trim() : raw.Trim();

            LstIsos.ItemsSource = filtro == "Todos"
                ? _allIsos
                : new ObservableCollection<IsoItem>(_allIsos.Where(i => i.Categoria == filtro));

            UpdateContagem();
        }

        private void UpdateContagem()
        {
            if (LstIsos == null || TxtContagem == null) return;
            int total = (LstIsos.ItemsSource as IEnumerable<IsoItem>)?.Count() ?? 0;
            TxtContagem.Text = $"{total} ISOs";
        }

        // ── Download Resiliente via Stream + SHA-256 ──────────────────────────
        private async void OnDownloadClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not IsoItem iso) return;

            if (_isDownloading)
            {
                MessageBox.Show("Aguarde o download atual terminar.", "Em andamento",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (iso.IsDownloaded)
            {
                OnOpenFileClick(sender, e);
                return;
            }

            if (!iso.IsDirectLink)
            {
                OnOpenSiteClick(sender, e);
                return;
            }

            _isDownloading = true;
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            BtnCancel.Visibility = Visibility.Visible;
            PrgGlobal.Visibility = Visibility.Visible;
            PrgGlobal.Value = 0;
            iso.IsDownloading = true;
            iso.Progresso = 0;

            try
            {
                var result = await DownloadAsync(iso, _cts.Token);
                string shortHash = result.Sha256Hex.Length >= 12 ? result.Sha256Hex[..12] : result.Sha256Hex;
                TxtStatus.Text = $"✅ {iso.Nome} baixado! (SHA-256: {shortHash}…)";
                _logger?.Success($"ISO baixada: {iso.Nome} ({result.TotalBytes / 1_048_576.0:F1} MB, SHA-256={result.Sha256Hex})", "IsoManager");
                SaveIsoList();
            }
            catch (OperationCanceledException)
            {
                TxtStatus.Text = "⛔ Download cancelado — arquivo temporário removido.";
                iso.Progresso = 0;
                iso.LocalPath = null;
                _logger?.Warn($"Download de ISO cancelado pelo usuário: {iso.Nome}", "IsoManager");
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Erro: {ex.Message}";
                _logger?.Error($"Falha ao baixar ISO '{iso.Nome}'", ex, "IsoManager");
                MessageBox.Show($"Erro ao baixar:\n{ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                iso.IsDownloading = false;
                _isDownloading = false;
                BtnCancel.Visibility = Visibility.Collapsed;
                PrgGlobal.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<DownloadFileResult> DownloadAsync(IsoItem iso, CancellationToken ct)
        {
            string safeCategory = string.Join("_", iso.Categoria.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            string folder = Path.Combine(AppPaths.IsosDir, string.IsNullOrWhiteSpace(safeCategory) ? "Outros" : safeCategory);
            Directory.CreateDirectory(folder);

            string fileName = Path.GetFileName(new Uri(iso.Url).LocalPath);
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Length < 4)
                fileName = string.Join("_", iso.Nome.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)) + ".iso";

            string filePath = Path.Combine(folder, fileName);

            var progress = new Progress<DownloadProgress>(p =>
            {
                int pct = p.Percentage >= 0 ? p.Percentage : 0;
                iso.Progresso = pct;
                PrgGlobal.Value = pct;

                string totalTxt = p.TotalBytes > 0 ? $"{p.TotalBytes / 1_048_576.0:F0} MB" : "? MB";
                TxtStatus.Text =
                    $"⬇  {iso.Nome}  —  {p.BytesRead / 1_048_576.0:F0} MB / {totalTxt}  ({pct}% • {p.SpeedMBps:F1} MB/s)";
            });

            var result = await _http.DownloadFileAsync(iso.Url, filePath, progress, expectedSha256: null, ct);
            iso.LocalPath = result.FilePath;
            return result;
        }

        // ── Ações inline ──────────────────────────────────────────────────────
        private void OnCancelClick(object sender, RoutedEventArgs e) => _cts?.Cancel();

        private void OnOpenSiteClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not IsoItem iso) return;
            if (!Uri.TryCreate(iso.Url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return;

            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }

        private void OnOpenFileClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not IsoItem iso || iso.LocalPath == null) return;
            string? dir = Path.GetDirectoryName(iso.LocalPath);
            if (dir != null && Directory.Exists(dir))
                Process.Start("explorer.exe", dir);
        }

        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not IsoItem iso) return;
            iso.LocalPath = null;
            iso.Progresso = 0;
            SaveIsoList();
            TxtStatus.Text = "Vínculo removido.";
        }

        private void OnOpenFolderClick(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(AppPaths.IsosDir);
            Process.Start("explorer.exe", AppPaths.IsosDir);
        }

        // ── Adicionar ISO personalizado ───────────────────────────────────────
        private void OnAddCustomIsoClick(object sender, RoutedEventArgs e)
        {
            var dlg = new AddIsoDialog { Owner = this };
            if (dlg.ShowDialog() == true && dlg.Result != null)
            {
                _allIsos.Add(dlg.Result);
                SaveIsoList();
                ApplyFilter();
            }
        }

        // ── Lista de ISOs (110+ sistemas) ────────────────────────────────────
        private static ObservableCollection<IsoItem> BuildIsoList()
        {
            static IsoItem W(string n, string v, string u, string d = "", long m = 0, string a = "x64") =>
                new() { Nome=n, Versao=v, Arq=a, Categoria="Windows",     IsDirectLink=false, TamanhoMB=m, Url=u, Descricao=d };
            static IsoItem L(string n, string v, string u, string d = "", long m = 0, bool dl = true, string a = "x64") =>
                new() { Nome=n, Versao=v, Arq=a, Categoria="Linux",       IsDirectLink=dl,   TamanhoMB=m, Url=u, Descricao=d };
            static IsoItem B(string n, string v, string u, string d = "", long m = 0, bool dl = true) =>
                new() { Nome=n, Versao=v, Arq="x64", Categoria="BSD",      IsDirectLink=dl,   TamanhoMB=m, Url=u, Descricao=d };
            static IsoItem U(string n, string v, string u, string d = "", long m = 0, bool dl = true) =>
                new() { Nome=n, Versao=v, Arq="x64", Categoria="Utilitários", IsDirectLink=dl, TamanhoMB=m, Url=u, Descricao=d };
            static IsoItem O(string n, string v, string u, string d = "", long m = 0, bool dl = false) =>
                new() { Nome=n, Versao=v, Arq="x64", Categoria="Outros",   IsDirectLink=dl,   TamanhoMB=m, Url=u, Descricao=d };

            return new ObservableCollection<IsoItem>
            {
            // ══ WINDOWS ═══════════════════════════════════════════════════════
            W("Windows 11 Home / Pro",         "23H2",    "https://www.microsoft.com/software-download/windows11",                                    "Versão mais recente", 5200),
            W("Windows 11 Education/Enterprise","23H2",   "https://www.microsoft.com/en-us/evalcenter/evaluate-windows-11-enterprise",                "Avaliação 90 dias", 5200),
            W("Windows 11 Home / Pro",         "22H2",    "https://www.microsoft.com/software-download/windows11",                                    "Versão anterior estável", 5100),
            W("Windows 11",                    "ARM64 23H2","https://www.microsoft.com/software-download/windows11arm64",                             "Para Surface / Qualcomm", 5000, "ARM64"),
            W("Windows 10 Home / Pro",         "22H2",    "https://www.microsoft.com/software-download/windows10",                                    "Suporte até out/2025", 5800),
            W("Windows 10 LTSC",               "2021",    "https://www.microsoft.com/en-us/evalcenter/evaluate-windows-10-enterprise",                "Long Term Service — estável", 4500),
            W("Windows 10 LTSC",               "2019",    "https://www.microsoft.com/en-us/evalcenter/evaluate-windows-10-enterprise-2019",           "Versão LTSC anterior", 4200),
            W("Windows 8.1",                   "Update 3","https://www.microsoft.com/software-download/windows8ISO",                                  "Suporte encerrado jan/2023", 3900),
            W("Windows 7 SP1",                 "SP1 x64", "https://www.microsoft.com/software-download/windows7",                                     "Suporte encerrado jan/2020", 3200),
            W("Windows Server 2022",           "Eval 180d","https://www.microsoft.com/en-us/evalcenter/evaluate-windows-server-2022",                 "Avaliação gratuita 180 dias", 5300),
            W("Windows Server 2019",           "Eval 180d","https://www.microsoft.com/en-us/evalcenter/evaluate-windows-server-2019",                 "Avaliação gratuita 180 dias", 4900),
            W("Windows Server 2016",           "Eval 180d","https://www.microsoft.com/en-us/evalcenter/evaluate-windows-server-2016",                 "Avaliação gratuita 180 dias", 6600),
            W("Windows Server 2012 R2",        "Eval 180d","https://www.microsoft.com/en-us/evalcenter/evaluate-windows-server-2012-r2",              "Avaliação gratuita 180 dias", 4700),

            // ══ LINUX — Ubuntu Family ═════════════════════════════════════════
            L("Ubuntu Desktop",    "24.04.2 LTS","https://releases.ubuntu.com/24.04/ubuntu-24.04.2-desktop-amd64.iso",                               "LTS atual, suporte até 2029", 5720),
            L("Ubuntu Desktop",    "22.04.4 LTS","https://releases.ubuntu.com/22.04/ubuntu-22.04.4-desktop-amd64.iso",                               "LTS estável, suporte até 2027", 4700),
            L("Ubuntu Desktop",    "20.04.6 LTS","https://releases.ubuntu.com/20.04/ubuntu-20.04.6-desktop-amd64.iso",                               "LTS legado, suporte até 2025", 3300),
            L("Ubuntu Server",     "24.04.2 LTS","https://releases.ubuntu.com/24.04/ubuntu-24.04.2-live-server-amd64.iso",                           "Edição para servidores", 2600),
            L("Ubuntu Server",     "22.04.4 LTS","https://releases.ubuntu.com/22.04/ubuntu-22.04.4-live-server-amd64.iso",                           "Server LTS anterior", 2100),
            L("Kubuntu",           "24.04 LTS",  "https://cdimage.ubuntu.com/kubuntu/releases/24.04/release/kubuntu-24.04-desktop-amd64.iso",         "Ubuntu com KDE Plasma", 3200),
            L("Kubuntu",           "22.04 LTS",  "https://cdimage.ubuntu.com/kubuntu/releases/22.04/release/kubuntu-22.04.4-desktop-amd64.iso",       "KDE Plasma LTS anterior", 3100),
            L("Xubuntu",           "24.04 LTS",  "https://cdimage.ubuntu.com/xubuntu/releases/24.04/release/xubuntu-24.04-desktop-amd64.iso",         "Ubuntu com XFCE — leve", 3000),
            L("Lubuntu",           "24.04 LTS",  "https://cdimage.ubuntu.com/lubuntu/releases/24.04/release/lubuntu-24.04-desktop-amd64.iso",         "Ubuntu com LXQt — muito leve", 2900),
            L("Ubuntu MATE",       "24.04 LTS",  "https://cdimage.ubuntu.com/ubuntu-mate/releases/24.04/release/ubuntu-mate-24.04-desktop-amd64.iso", "Interface MATE clássica", 3100),
            L("Ubuntu Budgie",     "24.04 LTS",  "https://cdimage.ubuntu.com/ubuntu-budgie/releases/24.04/release/ubuntu-budgie-24.04-desktop-amd64.iso","Visual elegante", 3000),
            L("Ubuntu Studio",     "24.04 LTS",  "https://cdimage.ubuntu.com/ubuntustudio/releases/24.04/release/ubuntustudio-24.04-dvd-amd64.iso",   "Criação de conteúdo/áudio/vídeo", 5500),
            L("Ubuntu Kylin",      "24.04 LTS",  "https://cdimage.ubuntu.com/ubuntukylin/releases/24.04/release/ubuntukylin-24.04-desktop-amd64.iso", "Ubuntu oficial da China", 2800),
            L("Ubuntu Unity",      "24.04 LTS",  "https://cdimage.ubuntu.com/ubuntu-unity/releases/24.04/release/ubuntu-unity-24.04-desktop-amd64.iso","Interface Unity restaurada",2900),
            L("KDE neon",          "24.04",      "https://files.kde.org/neon/images/user/current/neon-user-current.iso",                              "KDE Plasma em estado da arte", 2800, true),

            // Linux Mint
            L("Linux Mint Cinnamon","22",        "https://mirrors.edge.kernel.org/linuxmint/stable/22/linuxmint-22-cinnamon-64bit.iso",               "Distro amigável p/ ex-Windows", 2800),
            L("Linux Mint XFCE",   "22",         "https://mirrors.edge.kernel.org/linuxmint/stable/22/linuxmint-22-xfce-64bit.iso",                   "Versão leve do Mint", 2700),
            L("Linux Mint MATE",   "22",         "https://mirrors.edge.kernel.org/linuxmint/stable/22/linuxmint-22-mate-64bit.iso",                   "Interface MATE clássica", 2700),
            L("Linux Mint Cinnamon","21.3",      "https://mirrors.edge.kernel.org/linuxmint/stable/21.3/linuxmint-21.3-cinnamon-64bit.iso",            "Versão anterior LTS", 2700),
            L("LMDE 6",            "Faye",       "https://mirrors.edge.kernel.org/linuxmint/debian/lmde6-cinnamon-64bit.iso",                         "Linux Mint baseado em Debian", 2300, false, "x64"),

            // Debian
            L("Debian Netinstall", "12.10",      "https://cdimage.debian.org/debian-cd/current/amd64/iso-cd/debian-12.10.0-amd64-netinst.iso",        "Instalação mínima via rede", 700),
            L("Debian DVD",        "12.10",      "https://cdimage.debian.org/debian-cd/current/amd64/iso-dvd/debian-12.10.0-amd64-DVD-1.iso",         "DVD completo offline", 3900),
            L("Debian Netinstall", "11.9",       "https://cdimage.debian.org/debian-cd/11.9.0/amd64/iso-cd/debian-11.9.0-amd64-netinst.iso",          "Debian Bullseye", 400),
            L("Debian Testing",    "Trixie",     "https://cdimage.debian.org/cdimage/weekly-builds/amd64/iso-cd/debian-testing-amd64-netinst.iso",     "Versão de testes rolling", 700),

            // Fedora
            L("Fedora Workstation","41",         "https://download.fedoraproject.org/pub/fedora/linux/releases/41/Workstation/x86_64/iso/Fedora-Workstation-Live-x86_64-41-1.4.iso","Tecnologia de ponta", 2300),
            L("Fedora KDE Spin",   "41",         "https://download.fedoraproject.org/pub/fedora/linux/releases/41/Spins/x86_64/iso/Fedora-KDE-Live-x86_64-41-1.4.iso","KDE Plasma", 2400),
            L("Fedora XFCE Spin",  "41",         "https://download.fedoraproject.org/pub/fedora/linux/releases/41/Spins/x86_64/iso/Fedora-Xfce-Live-x86_64-41-1.4.iso","Leve e estável", 1900),
            L("Fedora Workstation","40",         "https://download.fedoraproject.org/pub/fedora/linux/releases/40/Workstation/x86_64/iso/Fedora-Workstation-Live-x86_64-40-1.14.iso","Versão anterior", 2200),
            L("Fedora Server",     "41",         "https://download.fedoraproject.org/pub/fedora/linux/releases/41/Server/x86_64/iso/Fedora-Server-dvd-x86_64-41-1.4.iso","Edição para servidores", 2600),
            L("Fedora CoreOS",     "latest",     "https://fedoraproject.org/coreos/download/",                                                        "Para containers/Kubernetes", 900, false),

            // RHEL Family
            L("Rocky Linux DVD",   "9.4",        "https://download.rockylinux.org/pub/rocky/9/isos/x86_64/Rocky-9.4-x86_64-dvd.iso",                  "RHEL clone — produção", 10700),
            L("Rocky Linux",       "8.10",       "https://download.rockylinux.org/pub/rocky/8/isos/x86_64/Rocky-8.10-x86_64-dvd1.iso",                "Rocky 8 LTS", 10800),
            L("Rocky Linux Minimal","9.4",       "https://download.rockylinux.org/pub/rocky/9/isos/x86_64/Rocky-9.4-x86_64-minimal.iso",              "Instalação mínima", 2000),
            L("AlmaLinux DVD",     "9.4",        "https://repo.almalinux.org/almalinux/9/isos/x86_64/AlmaLinux-9.4-x86_64-dvd.iso",                   "RHEL clone alternativo", 9800),
            L("AlmaLinux",         "8.10",       "https://repo.almalinux.org/almalinux/8/isos/x86_64/AlmaLinux-8.10-x86_64-dvd.iso",                  "AlmaLinux 8", 9600),
            L("CentOS Stream",     "9",          "https://mirror.stream.centos.org/9-stream/BaseOS/x86_64/iso/CentOS-Stream-9-latest-x86_64-dvd1.iso", "Rolling RHEL upstream", 9500),
            L("Oracle Linux",      "9.4",        "https://yum.oracle.com/ISOS/OracleLinux/OL9/u4/x86_64/OracleLinux-R9-U4-x86_64-dvd.iso",           "RHEL compatível Oracle", 9200),
            L("Scientific Linux",  "7.9",        "https://ftp.scientificlinux.org/linux/scientific/7.9/x86_64/iso/SL-79-x86_64-2021-03-25-boot.iso",  "Usado em física/ciência", 600),

            // openSUSE
            L("openSUSE Leap",     "15.6",       "https://download.opensuse.org/distribution/leap/15.6/iso/openSUSE-Leap-15.6-DVD-x86_64-Media.iso",  "Enterprise-grade estável", 4800),
            L("openSUSE Tumbleweed","rolling",   "https://download.opensuse.org/tumbleweed/iso/openSUSE-Tumbleweed-DVD-x86_64-Current.iso",            "Rolling release atualizado", 4600),
            L("openSUSE MicroOS",  "rolling",    "https://download.opensuse.org/tumbleweed/appliances/openSUSE-MicroOS.x86_64-ContainerHost-kvm-and-xen.qcow2","Para containers", 1800, false),

            // Arch Family
            L("Arch Linux",        "latest",     "https://geo.mirror.pkgbuild.com/iso/latest/archlinux-x86_64.iso",                                   "Instale só o que precisar", 1000),
            L("Manjaro GNOME",     "24.1",       "https://manjaro.org/download/",                                                                      "Arch amigável — GNOME", 3100, false),
            L("Manjaro KDE",       "24.1",       "https://manjaro.org/download/",                                                                      "Arch amigável — KDE Plasma", 3200, false),
            L("Manjaro XFCE",      "24.1",       "https://manjaro.org/download/",                                                                      "Arch amigável — XFCE", 2900, false),
            L("EndeavourOS",       "Gemini",     "https://mirrors.alpix.eu/endeavouros/iso/EndeavourOS_Gemini-2024.04.20.iso",                         "Arch com instalador fácil", 2800),
            L("Garuda Dr460nized", "latest",     "https://iso.builds.garudalinux.org/iso/latest/garuda/dr460nized/latest.iso",                        "Visual deslumbrante, gaming", 2700, false),
            L("Artix Linux XFCE",  "latest",     "https://download.artixlinux.org/iso/artix-base-openrc-20241027-x86_64.iso",                         "Arch sem systemd", 2000),
            L("CachyOS",           "latest",     "https://cachyos.org/download/",                                                                      "Arch otimizado para desempenho", 2800, false),

            // Security / Kali Linux
            L("Kali Linux Installer",    "2024.4",  "https://cdimage.kali.org/kali-2024.4/kali-linux-2024.4-installer-amd64.iso",                     "Instalação completa no HD", 4100),
            L("Kali Linux Live",         "2024.4",  "https://cdimage.kali.org/kali-2024.4/kali-linux-2024.4-live-amd64.iso",                          "Boot direto sem instalar", 3900),
            L("Kali Linux Everything",   "2024.4",  "https://cdimage.kali.org/kali-2024.4/kali-linux-2024.4-installer-everything-amd64.iso",           "Todos os 600+ pacotes incluídos", 9000),
            L("Kali Linux Netinst",      "2024.4",  "https://cdimage.kali.org/kali-2024.4/kali-linux-2024.4-installer-netinst-amd64.iso",              "Instalação mínima via rede", 500),
            L("Kali Linux Purple",       "2023.1",  "https://cdimage.kali.org/kali-2023.1/kali-linux-2023.1-installer-purple-amd64.iso",               "Foco em segurança defensiva (Blue Team)", 4400),
            L("Kali Linux NetHunter",    "2024.4",  "https://www.kali.org/get-kali/#kali-arm",                                                         "Para dispositivos Android/ARM", 1500, false),
            L("Kali Linux VM (VMware)",  "2024.4",  "https://www.kali.org/get-kali/#kali-virtual-machines",                                            "Imagem pronta para VMware", 3600, false),
            L("Kali Linux VM (VBox)",    "2024.4",  "https://www.kali.org/get-kali/#kali-virtual-machines",                                            "Imagem pronta para VirtualBox", 3600, false),
            L("Kali Linux 2023.4",       "2023.4",  "https://cdimage.kali.org/kali-2023.4/kali-linux-2023.4-installer-amd64.iso",                      "Versão anterior estável", 4000),
            L("Kali Linux 2022.4",       "2022.4",  "https://cdimage.kali.org/kali-2022.4/kali-linux-2022.4-installer-amd64.iso",                      "Versão 2022 para hardware legado", 3700),

            // Outras distros de segurança
            L("Parrot Security",         "6.1",     "https://deb.parrot.sh/parrot/iso/6.1/Parrot-security-6.1_amd64.iso",                             "Segurança e privacidade Debian-based", 4400),
            L("Parrot Home",             "6.1",     "https://deb.parrot.sh/parrot/iso/6.1/Parrot-home-6.1_amd64.iso",                                 "Uso diário com privacidade", 2300),
            L("Parrot Architect",        "6.1",     "https://deb.parrot.sh/parrot/iso/6.1/Parrot-architect-6.1_amd64.iso",                            "Instalação mínima p/ customização", 600),
            L("BlackArch Linux",         "2024.01", "https://ftp.halifax.rwth-aachen.de/blackarch/iso/blackarch-linux-full-2024.01.01-x86_64.iso",     "2800+ ferramentas de segurança", 21500),
            L("BlackArch Slim",          "2024.01", "https://ftp.halifax.rwth-aachen.de/blackarch/iso/blackarch-linux-slim-2024.01.01-x86_64.iso",     "Versão enxuta do BlackArch", 1900),
            L("Whonix Gateway+WS",       "17.2",    "https://www.whonix.org/wiki/Download",                                                            "Anonimato via Tor (VM dupla)", 1600, false),
            L("Qubes OS",                "4.2.3",   "https://www.qubes-os.org/downloads/",                                                             "Segurança por compartimentalização", 5500, false),
            L("Tails OS",                "6.10",    "https://tails.net/install/download/",                                                             "Zero rastros — amnésia total", 1300, false),
            L("Whonix CLI",              "17.2",    "https://www.whonix.org/wiki/Download",                                                            "Versão linha de comando", 1200, false),
            L("REMnux",                  "7.0",     "https://remnux.org/",                                                                             "Análise de malware e forense", 5000, false),
            L("SIFT Workstation",        "3.0",     "https://www.sans.org/tools/sift-workstation/",                                                    "Forense digital SANS", 5000, false),
            L("DEFT Linux",              "8.2",     "https://www.deftlinux.net/",                                                                      "Digital Evidence Forensics Toolkit", 2000, false),

            // Outros Linux
            L("Pop!_OS Intel/AMD", "22.04 LTS",  "https://iso.pop-os.org/22.04/amd64/intel/42/pop-os_22.04_amd64_intel_42.iso",                       "Ubuntu System76 otimizado", 2700),
            L("Pop!_OS NVIDIA",    "22.04 LTS",  "https://iso.pop-os.org/22.04/amd64/nvidia/42/pop-os_22.04_amd64_nvidia_42.iso",                     "Versão com drivers NVIDIA", 2900),
            L("Zorin OS Core",     "17",         "https://zorin.com/os/download/",                                                                     "Visual próximo ao Windows", 3100, false),
            L("Zorin OS Lite",     "17",         "https://zorin.com/os/download/",                                                                     "Versão leve para PCs antigos", 2600, false),
            L("elementary OS",     "7.1",        "https://elementary.io/",                                                                             "Design inspirado no macOS", 2300, false),
            L("deepin",            "23",         "https://www.deepin.org/en/download/",                                                                "Interface mais bonita do Linux", 4000, false),
            L("MX Linux KDE",      "23.3",       "https://sourceforge.net/projects/mx-linux/files/Final/KDE/MX-23.3_KDE_x64.iso",                     "Debian estável e rápido", 2800),
            L("MX Linux XFCE",     "23.3",       "https://sourceforge.net/projects/mx-linux/files/Final/MX-23.3_x64.iso",                             "Muito popular, leve", 2600),
            L("antiX",             "23.1",       "https://sourceforge.net/projects/antix-linux/files/Final/antiX-23.1/antiX-23.1_x64-full.iso",       "Sem systemd, leve", 1600),
            L("Void Linux",        "glibc 2024", "https://repo-default.voidlinux.org/live/current/void-live-x86_64-20240314-xfce.iso",                "Rolling, independente", 1800),
            L("NixOS",             "24.05",      "https://channels.nixos.org/nixos-24.05/latest-nixos-gnome-x86_64-linux.iso",                        "Configuração declarativa", 2100),
            L("Solus",             "4.5",        "https://getsol.us/download/",                                                                        "Desktop de alto desempenho", 2300, false),
            L("Slackware",         "15.0",       "https://mirrors.slackware.com/slackware/slackware64-15.0/iso/slackware64-15.0-install-dvd.iso",      "A mais antiga ainda ativa", 3900),
            L("Gentoo Install",    "2024",       "https://bouncer.gentoo.org/fetch/root/all/releases/amd64/autobuilds/current-install-amd64-minimal/install-amd64-minimal-20241027T164557Z.iso","Compilação total do zero",500),
            L("SparkyLinux KDE",   "7.4",        "https://sourceforge.net/projects/sparkylinux/files/xfce/sparkylinux-7.4-x86_64-kde.iso",             "Debian rápido e leve", 2200),
            L("Peppermint OS",     "11",         "https://peppermintos.com/guide/downloading/",                                                        "Leve baseado em Debian", 1900, false),
            L("Q4OS Trinity",      "5.6",        "https://sourceforge.net/projects/q4os/files/stable/",                                               "Visual Windows XP/7", 1200, false),
            L("Bodhi Linux",       "7.0",        "https://sourceforge.net/projects/bodhilinux/files/latest/download",                                 "Baseado em Ubuntu, levíssimo", 950),
            L("Nobara Linux",      "40",         "https://nobaraproject.org/download-nobara/",                                                         "Fedora otimizado para games", 3500, false),
            L("Bazzite",           "40",         "https://bazzite.gg/",                                                                                "Linux gamers — Steam Deck", 3800, false),
            L("Puppy Linux",       "BionicPup",  "https://sourceforge.net/projects/puppy/files/",                                                      "Roda inteiro na RAM", 400, false),
            L("Tiny Core",         "14.0",       "http://tinycorelinux.net/14.x/x86/release/TinyCore-14.0.iso",                                       "O menor Linux funcional: 21MB", 21),
            L("Finnix",            "125",        "https://www.finnix.org/releases/125/finnix-125.iso",                                                 "Debian Live para sysadmins", 500),

            // Linux — Mais distros especializadas
            L("Alpine Linux",      "3.20.3",    "https://dl-cdn.alpinelinux.org/alpine/v3.20/releases/x86_64/alpine-standard-3.20.3-x86_64.iso",     "Levíssimo, musel libc — containers", 200),
            L("Alpine Linux Extended","3.20.3", "https://dl-cdn.alpinelinux.org/alpine/v3.20/releases/x86_64/alpine-extended-3.20.3-x86_64.iso",     "Alpine com mais pacotes", 700),
            L("Clear Linux",       "latest",    "https://clearlinux.org/downloads",                                                                   "Intel-otimizado, máx desempenho", 2000, false),
            L("Mageia",            "9",         "https://www.mageia.org/downloads/",                                                                  "Fork do Mandriva, fácil de usar", 3500, false),
            L("OpenMandriva",      "24.03",     "https://www.openmandriva.org/en/download",                                                           "Desktop polido baseado em Mandriva", 2800, false),
            L("PCLinuxOS",         "2024",      "https://www.pclinuxos.com/get-pclinuxos/",                                                           "Rolling release amigável", 2800, false),
            L("Devuan",            "5.0",       "https://www.devuan.org/get-devuan",                                                                  "Debian sem systemd", 900, false),
            L("PureOS",            "10.0",      "https://pureos.net/download/",                                                                       "100% software livre — Purism", 2200, false),
            L("Trisquel",          "11.0",      "https://trisquel.info/en/download",                                                                  "Ubuntu 100% software livre (FSF)", 3400, false),
            L("Guix System",       "1.4.0",     "https://guix.gnu.org/en/download/",                                                                  "Declarativo como NixOS, GNU puro", 1200, false),
            L("NixOS KDE",         "24.05",     "https://channels.nixos.org/nixos-24.05/latest-nixos-plasma5-x86_64-linux.iso",                       "NixOS com KDE Plasma", 2400),
            L("NixOS Minimal",     "24.05",     "https://channels.nixos.org/nixos-24.05/latest-nixos-minimal-x86_64-linux.iso",                       "NixOS sem interface gráfica", 1100),
            L("Calculate Linux",   "23",        "https://www.calculate-linux.org/download",                                                           "Gentoo user-friendly", 2200, false),
            L("Funtoo Linux",      "2024",      "https://www.funtoo.org/Install/Download",                                                            "Gentoo otimizado por drobbins", 800, false),
            L("Bedrock Linux",     "0.7.30",    "https://bedrocklinux.org/0.7/hijack.sh",                                                             "Use pacotes de qualquer distro", 50, false),
            L("Chimera Linux",     "latest",    "https://chimera-linux.org/downloads",                                                                "FreeBSD userland + Linux kernel", 600, false),
            L("Debian Edu",        "12.10",     "https://cdimage.debian.org/cdimage/unofficial/edu/current/amd64/iso-dvd/",                           "Debian para ambientes educacionais", 4000, false),
            L("Raspberry Pi OS",   "Bookworm",  "https://www.raspberrypi.com/software/operating-systems/",                                            "Para Raspberry Pi (ARM)", 1100, false, "ARM"),
            L("Armbian",           "24.08",     "https://www.armbian.com/download/",                                                                  "Debian/Ubuntu para placas ARM", 900, false, "ARM"),
            L("DietPi",            "9.5",       "https://dietpi.com/",                                                                                "Ultra-leve para SBC e VMs", 500, false),
            L("Proxmox VE",        "8.2",       "https://enterprise.proxmox.com/iso/proxmox-ve_8.2-1.iso",                                            "Hipervisor KVM + LXC gratuito", 1100),
            L("Proxmox Backup",    "3.2",       "https://enterprise.proxmox.com/iso/proxmox-backup-server_3.2-1.iso",                                 "Servidor de backup Proxmox", 1000),
            L("oVirt Node",        "4.5",       "https://resources.ovirt.org/pub/ovirt-4.5/iso/",                                                     "Virtualização enterprise Red Hat", 1200, false),
            L("Talos Linux",       "1.7.0",     "https://factory.talos.dev/",                                                                         "OS imutável para Kubernetes", 100, false),
            L("Flatcar Container", "latest",    "https://www.flatcar.org/releases",                                                                   "CoreOS successor, para containers", 400, false),
            L("Void Linux Musl",   "2024",      "https://repo-default.voidlinux.org/live/current/void-live-x86_64-musl-20240314-xfce.iso",            "Void com musl libc", 1700),
            L("GRML Full",         "2024.02",   "https://download.grml.org/grml96-full_2024.02.iso",                                                  "Debian Live sysadmin full", 900),
            L("4MLinux",           "44.0",      "https://4mlinux.com/",                                                                               "Mini Linux: 4 funções básicas", 350, false),
            L("Emmabuntüs",        "DE5",       "https://emmabuntus.org/distribution/download/",                                                      "Para computadores recondicionados", 3500, false),
            L("Lakka",             "5.x",       "https://www.lakka.tv/get/",                                                                          "RetroGaming — emulação consoles", 900, false),
            L("Batocera Linux",    "40",        "https://batocera.org/download",                                                                      "Console retro gaming all-in-one", 4000, false),
            L("LibreELEC",         "12.0",      "https://libreelec.tv/downloads/",                                                                    "Kodi media center OS", 300, false),
            L("OSMC",              "2024.01",   "https://osmc.tv/download/",                                                                          "Kodi para Raspberry Pi / Apple TV", 400, false),

            // ══ BSD ═══════════════════════════════════════════════════════════
            B("FreeBSD DVD",       "14.1",       "https://download.freebsd.org/releases/amd64/amd64/ISO-IMAGES/14.1/FreeBSD-14.1-RELEASE-amd64-dvd1.iso","BSD mais popular", 4700),
            B("FreeBSD",           "13.3",       "https://download.freebsd.org/releases/amd64/amd64/ISO-IMAGES/13.3/FreeBSD-13.3-RELEASE-amd64-dvd1.iso","Versão anterior estável", 4200),
            B("OpenBSD",           "7.5",        "https://cdn.openbsd.org/pub/OpenBSD/7.5/amd64/install75.iso",                                       "Segurança em primeiro lugar", 800),
            B("NetBSD",            "10.0",       "https://cdn.netbsd.org/pub/NetBSD/NetBSD-10.0/images/NetBSD-10.0-amd64.iso",                        "Portável para qualquer hardware", 600),
            B("GhostBSD",          "24.04.1",    "https://download.ghostbsd.org/releases/amd64/24.04.1/GhostBSD-24.04.1.iso",                         "FreeBSD com desktop MATE", 2600),
            B("TrueNAS SCALE",     "24.04",      "https://download.sys.truenas.net/TrueNAS-SCALE-DragonFish/24.04.0/TrueNAS-SCALE-24.04.0.iso",       "NAS baseado em Linux/FreeBSD", 1600),
            B("TrueNAS CORE",      "13.0-U6.1",  "https://download.freenas.org/13.0/STABLE/U6.1/x64/TrueNAS-13.0-U6.1.iso",                          "NAS baseado em FreeBSD", 1100),
            B("pfSense CE",        "2.7.2",      "https://www.pfsense.org/download/",                                                                  "Firewall/roteador BSD", 600, false),
            B("OPNsense",          "24.7",       "https://opnsense.org/download/",                                                                     "Firewall moderno baseado em BSD", 600, false),
            B("MidnightBSD",       "3.1",        "https://www.midnightbsd.org/download/",                                                              "BSD desktop user-friendly", 1200, false),
            B("DragonFly BSD",     "6.4.0",      "https://mirror-master.dragonflybsd.org/iso-images/dfly-x86_64-6.4.0_REL.iso",                       "HAMMER2 filesystem inovador", 1200),
            B("NomadBSD",          "131R-20240126","https://nomadbsd.org/download.html",                                                               "FreeBSD persistente em pen drive", 1400, false),
            B("HardenedBSD",       "14-STABLE",  "https://hardenedbsd.org/downloads",                                                                  "FreeBSD com hardening extra", 900, false),
            B("XigmaNAS",          "13.3",       "https://xigmanas.com/xnaswiki/doku.php?id=downloads",                                               "NAS BSD leve (antigo NAS4Free)", 500, false),
            B("ClonOS",            "latest",     "https://clonos.kotet.ru/download/",                                                                  "Painel web para FreeBSD/bhyve", 600, false),

            // ══ OUTROS ════════════════════════════════════════════════════════
            O("ChromeOS Flex",     "latest",     "https://chromeenterprise.google/os/chromeosflex/",                                                   "Google ChromeOS para qualquer PC"),
            O("Android-x86",       "9.0-r2",     "https://sourceforge.net/projects/android-x86/files/Release%209.0/android-x86_9.0-r2.iso",           "Android no PC x86", 900, true),
            O("Haiku",             "R1 Beta4",   "https://cdn.haiku-os.org/haiku-release/r1beta4/haiku-r1beta4-x86_64-anyboot.iso",                   "Sucessor espiritual do BeOS", 900, true),
            O("ReactOS",           "0.4.15",     "https://sourceforge.net/projects/reactos/files/ReactOS/0.4.15/ReactOS-0.4.15-release-x86-gcc-live.iso","Windows open source", 200, true),
            O("FreeDOS",           "1.3",        "https://www.ibiblio.org/pub/micro/pc-stuff/freedos/files/distributions/1.3/FD13-FullUSB.zip",        "DOS livre — compatível MS-DOS", 300, true),
            O("KolibriOS",         "latest",     "http://builds.kolibrios.org/en_US/latest-distr.7z",                                                  "OS escrito em Assembly, 1,44MB", 2),
            O("Redox OS",          "latest",     "https://www.redox-os.org/news/",                                                                     "OS escrito em Rust", 200, false),
            O("illumos/OpenIndiana","2023.10",   "https://www.openindiana.org/download/",                                                              "Descendente do Solaris/OpenSolaris", 1900, false),
            O("SerenityOS",        "nightly",    "https://serenityos.org/",                                                                            "OS moderno com API anos 90", 300, false),

            // ══ UTILITÁRIOS ═══════════════════════════════════════════════════
            U("GParted Live",      "1.6.0-3",    "https://downloads.sourceforge.net/gparted/gparted-live-1.6.0-3-amd64.iso",                          "Particionamento gráfico", 500),
            U("SystemRescue",      "11.02",      "https://fastly.cdn.sys.re/sys.re-11.02-amd64.iso",                                                  "Recuperação Linux/Windows", 920),
            U("Clonezilla Live",   "latest",     "https://clonezilla.org/downloads.php",                                                              "Clonagem e backup de discos", 400, false),
            U("Hiren's BootCD PE", "1.0.2",      "https://www.hirensbootcd.org/download/",                                                            "Kit completo de recuperação", 1000, false),
            U("Tails OS",          "6.10",       "https://tails.net/install/download/",                                                               "SO anônimo — zero rastros", 1300, false),
            U("Memtest86+",        "7.00",       "https://www.memtest.org/download/v7.00/mt86plus_7.00_64.iso",                                       "Diagnóstico de RAM", 15),
            U("Rescuezilla",       "2.5.1",      "https://rescuezilla.com/download",                                                                  "Backup/restore gráfico", 800, false),
            U("ShredOS",           "2023.07",    "https://github.com/PartialVolume/shredos.x86_64/releases/latest/download/shredos.img",              "Apagamento seguro de dados", 20),
            U("DBAN",              "2.3.0",      "https://sourceforge.net/projects/dban/files/dban/dban-2.3.0/dban-2.3.0_i586.iso",                   "Wipe de disco (legado)", 50),
            U("Finnix",            "125",        "https://www.finnix.org/releases/125/finnix-125.iso",                                                "Live CLI para sysadmins", 500),
            U("GRML",              "2024.02",    "https://download.grml.org/grml96-full_2024.02.iso",                                                  "Debian Live com ferramentas", 900),
            U("Knoppix",           "9.1",        "https://www.knopper.net/knoppix-mirrors/",                                                           "Pioneer dos Live CDs", 3900, false),
            U("Porteus",           "5.01 XFCE",  "https://porteus.org/",                                                                              "Módulos comprimidos, 300MB", 300, false),
            U("Trinity Rescue Kit","3.4",        "https://trinityhome.org/trinity_rescue_kit_download/",                                               "Recuperação Windows/Linux", 400, false),
            U("UBCD",              "5.3.9",      "https://www.ultimatebootcd.com/download.html",                                                       "Ultimate Boot CD — diagnósticos", 600, false),
            U("Ventoy Bootable",   "1.0.99",     "https://github.com/ventoy/Ventoy/releases",                                                         "Cria USB multiboot de ISOs", 20, false),
            U("WinPE Sergei",      "2024.01",    "https://sergeistrelec.ru/winpe_10_8/",                                                               "WinPE completo com ferramentas", 900, false),
            U("Kaspersky Rescue",  "18",         "https://www.kaspersky.com/downloads/free-rescue-disk",                                               "Antivírus bootável Kaspersky", 400, false),
            U("ESET SysRescue",    "latest",     "https://www.eset.com/us/support/sysrescue/",                                                         "Antivírus bootável ESET", 300, false),
            U("Avast Rescue",      "latest",     "https://www.avast.com/rescue-disk",                                                                  "Antivírus bootável Avast", 350, false),
            U("Dr.Web LiveDisk",   "9.0",        "https://free.drweb.com/livedisk/",                                                                   "Antivírus bootável Dr.Web", 800, false),
            U("Bitdefender Rescue","latest",     "https://www.bitdefender.com/support/how-to-create-a-bitdefender-rescue-cd-627.html",                 "Antivírus bootável Bitdefender", 600, false),
            U("Medicat USB",       "24.01",      "https://medicatusb.com/",                                                                            "WinPE completo para técnicos", 2000, false),
            U("Parted Magic",      "2024",       "https://partedmagic.com/",                                                                           "Particionamento + diagnóstico (pago)", 600, false),
            U("HDT (Plop)",        "latest",     "https://www.plop.at/en/bootmanager/download.html",                                                   "Boot manager + hardware detect", 20, false),
            U("MemTest86",         "10.4",       "https://www.memtest86.com/download.htm",                                                             "Diagnóstico RAM profissional", 35, false),
            U("CPU-Z Live",        "latest",     "https://www.cpuid.com/",                                                                             "Diagnóstico CPU/RAM bootável", 100, false),
            U("ShredOS x86",       "2023.07",    "https://github.com/PartialVolume/shredos.x86_64/releases",                                          "Wipe seguro NIST 800-88", 20, false),
            U("Eraser Live",       "latest",     "https://eraser.heidi.ie/",                                                                           "Apagamento seguro de dados", 150, false),
            U("Ultimate Boot CD",  "5.3.9",      "https://www.ultimatebootcd.com/",                                                                    "Diagnósticos CPU, HDD, memória", 600, false),
            U("Hiren's BootCD",    "15.2",       "https://www.hirensbootcd.org/old-versions/",                                                         "Versão clássica original (legado)", 700, false),
            U("AOMEI PE Builder",  "latest",     "https://www.aomeitech.com/pe-builder.html",                                                          "WinPE com backup/partição AOMEI", 500, false),
            U("Acronis Rescue",    "latest",     "https://www.acronis.com/en-us/support/",                                                             "Backup/restore Acronis bootável", 700, false),
            U("Macrium Rescue",    "latest",     "https://www.macrium.com/reflectfree.aspx",                                                           "Backup/restore Macrium bootável", 500, false),
            U("EaseUS WinPE",      "latest",     "https://www.easeus.com/todo-backup/",                                                                "Backup/clone EaseUS bootável", 500, false),
            };
        }
    }
}
