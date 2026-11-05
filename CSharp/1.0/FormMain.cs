using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TechKitPro
{
    public class FormMain : Form
    {
        // ── Cores do tema ────────────────────────────────────────────────────
        static readonly Color Azul      = Color.FromArgb(24, 95, 165);
        static readonly Color AzulLight = Color.FromArgb(230, 241, 251);
        static readonly Color Verde     = Color.FromArgb(15, 110, 86);
        static readonly Color VerdeLight= Color.FromArgb(225, 245, 238);
        static readonly Color BgDark    = Color.FromArgb(44, 44, 42);
        static readonly Color BgPanel   = Color.FromArgb(248, 248, 246);
        static readonly Color Borda     = Color.FromArgb(210, 210, 200);
        static readonly Color Texto     = Color.FromArgb(30, 30, 28);
        static readonly Color TextoSec  = Color.FromArgb(100, 100, 95);

        // ── Controles principais ─────────────────────────────────────────────
        private TabControl _tabs = null!;
        private RichTextBox _kitLog = null!, _posLog = null!, _hwLog = null!;
        private ProgressBar _kitProgress = null!;
        private Label _kitProgressLabel = null!;
        private Panel _kitToolPanel = null!, _posToolPanel = null!;
        private ProgressBar _posProgress = null!;
        private Label _posProgressLabel = null!;
        private TextBox _txtCliente = null!, _txtComputador = null!, _txtTecnico = null!;
        private Label _lblStatusBar = null!;
        private System.Windows.Forms.Timer _clock = null!;

        // Botões de ferramentas
        private readonly Dictionary<string, Button> _kitBtns = new();
        private readonly Dictionary<string, Button> _posBtns = new();

        // ── Configuração das ferramentas ──────────────────────────────────────
        record ToolDef(string Key, string Icon, string Label, string Command, bool RequiresAdmin = false, string Category = "");

        private readonly ToolDef[] _kitTools =
        {
            new("chkdsk",  "🖴", "Teste de Disco",   "chkdsk C: /scan /perf"),
            new("memtest", "🧠", "Scan de Memória",  "Get-WmiObject Win32_PhysicalMemory | Select-Object -Property BankLabel,Capacity,Speed | Format-Table -AutoSize"),
            new("temp",    "🌡", "Temp. CPU",        "Get-WmiObject MSAcpi_ThermalZoneTemperature -Namespace root/wmi | ForEach-Object { ($_.CurrentTemperature - 2732)/10 }"),
            new("ping",    "📡", "Diagnóstico Rede", "Test-Connection 8.8.8.8 -Count 4 | Select-Object -Property Address,ResponseTime,StatusCode | Format-Table"),
            new("defender","🛡", "Scan Malware",     "Start-MpScan -ScanType QuickScan"),
            new("sfc",     "🔧", "Integridade SFC",  "sfc /scannow",                          true),
            new("dism",    "🩹", "Reparo DISM",      "DISM /Online /Cleanup-Image /RestoreHealth", true),
            new("startup", "⚡", "Startup",          "Get-CimInstance Win32_StartupCommand | Select-Object -Property Name,Command,Location | Format-Table -AutoSize"),
        };

        private readonly ToolDef[] _posTools =
        {
            // ── Sistema
            new("winupdate",  "🪟", "Windows Update",     "Install-Module PSWindowsUpdate -Force; Get-WindowsUpdate -Install -AcceptAll -AutoReboot:$false",        false, "Sistema"),
            new("vcredist",   "⚙",  "VC++ Redist x64",    "winget install Microsoft.VCRedist.2015+.x64 --silent --accept-package-agreements --accept-source-agreements", false, "Sistema"),
            new("vcredist86", "⚙",  "VC++ Redist x86",    "winget install Microsoft.VCRedist.2015+.x86 --silent --accept-package-agreements --accept-source-agreements", false, "Sistema"),
            new("directx",    "🎯", "DirectX",            "winget install Microsoft.DirectX --silent --accept-package-agreements --accept-source-agreements",           false, "Sistema"),
            new("dotnet8",    "🟣", ".NET Runtime 8",     "winget install Microsoft.DotNet.DesktopRuntime.8 --silent --accept-package-agreements --accept-source-agreements", false, "Sistema"),
            new("dotnet9",    "🟣", ".NET Runtime 9",     "winget install Microsoft.DotNet.DesktopRuntime.9 --silent --accept-package-agreements --accept-source-agreements", false, "Sistema"),
            new("ps7",        "💠", "PowerShell 7",       "winget install Microsoft.PowerShell --silent --accept-package-agreements --accept-source-agreements",          false, "Sistema"),
            new("winterminal","🖥", "Windows Terminal",   "winget install Microsoft.WindowsTerminal --silent --accept-package-agreements --accept-source-agreements",     false, "Sistema"),
            new("powertoys",  "🔧", "PowerToys",          "winget install Microsoft.PowerToys --silent --accept-package-agreements --accept-source-agreements",           false, "Sistema"),
            new("cleanmgr",   "🧹", "Limpeza de Disco",   "cleanmgr /sagerun:1",                                                                                        false, "Sistema"),
            new("pagefile",   "💾", "Mem. Virtual Auto",  "wmic computersystem set AutomaticManagedPagefile=True",                                                       false, "Sistema"),
            new("restore",    "♻",  "Ponto de Restauro",  "Checkpoint-Computer -Description 'TechKitPro - Pos-Formatacao' -RestorePointType 'MODIFY_SETTINGS'",        true,  "Sistema"),
            // ── Navegadores
            new("chrome",     "🌐", "Google Chrome",      "winget install Google.Chrome --silent --accept-package-agreements --accept-source-agreements",                false, "Navegadores"),
            new("firefox",    "🦊", "Firefox",            "winget install Mozilla.Firefox --silent --accept-package-agreements --accept-source-agreements",              false, "Navegadores"),
            new("brave",      "🦁", "Brave",              "winget install Brave.Brave --silent --accept-package-agreements --accept-source-agreements",                  false, "Navegadores"),
            new("opera",      "🅾",  "Opera",              "winget install Opera.Opera --silent --accept-package-agreements --accept-source-agreements",                  false, "Navegadores"),
            new("vivaldi",    "🎻", "Vivaldi",            "winget install Vivaldi.Vivaldi --silent --accept-package-agreements --accept-source-agreements",              false, "Navegadores"),
            new("tor",        "🧅", "Tor Browser",        "winget install TorProject.TorBrowser --silent --accept-package-agreements --accept-source-agreements",       false, "Navegadores"),
            // ── Comunicação
            new("whatsapp",   "💬", "WhatsApp",           "winget install 9NKSQGP7F2NH --silent --accept-package-agreements --accept-source-agreements",                false, "Comunicação"),
            new("discord",    "🎮", "Discord",            "winget install Discord.Discord --silent --accept-package-agreements --accept-source-agreements",              false, "Comunicação"),
            new("telegram",   "✈",  "Telegram",           "winget install Telegram.TelegramDesktop --silent --accept-package-agreements --accept-source-agreements",   false, "Comunicação"),
            new("zoom",       "📹", "Zoom",               "winget install Zoom.Zoom --silent --accept-package-agreements --accept-source-agreements",                   false, "Comunicação"),
            new("teams",      "💼", "Microsoft Teams",   "winget install Microsoft.Teams --silent --accept-package-agreements --accept-source-agreements",              false, "Comunicação"),
            new("skype",      "🔵", "Skype",              "winget install Microsoft.Skype --silent --accept-package-agreements --accept-source-agreements",              false, "Comunicação"),
            new("slack",      "🟨", "Slack",              "winget install SlackTechnologies.Slack --silent --accept-package-agreements --accept-source-agreements",     false, "Comunicação"),
            new("signal",     "🔒", "Signal",             "winget install OpenWhisperSystems.Signal --silent --accept-package-agreements --accept-source-agreements",   false, "Comunicação"),
            // ── Mídia
            new("vlc",        "▶",  "VLC Media Player",   "winget install VideoLAN.VLC --silent --accept-package-agreements --accept-source-agreements",                false, "Mídia"),
            new("spotify",    "🎵", "Spotify",            "winget install Spotify.Spotify --silent --accept-package-agreements --accept-source-agreements",             false, "Mídia"),
            new("mpchc",      "🎬", "MPC-HC",             "winget install clsid2.mpc-hc --silent --accept-package-agreements --accept-source-agreements",               false, "Mídia"),
            new("foobar",     "🎶", "foobar2000",         "winget install PeterPavlishak.foobar2000 --silent --accept-package-agreements --accept-source-agreements",   false, "Mídia"),
            new("itunes",     "🎸", "iTunes",             "winget install Apple.iTunes --silent --accept-package-agreements --accept-source-agreements",                false, "Mídia"),
            new("audacity",   "🎙", "Audacity",           "winget install Audacity.Audacity --silent --accept-package-agreements --accept-source-agreements",           false, "Mídia"),
            new("handbrake",  "📼", "HandBrake",          "winget install HandBrake.HandBrake --silent --accept-package-agreements --accept-source-agreements",         false, "Mídia"),
            new("obs",        "📡", "OBS Studio",         "winget install OBSProject.OBSStudio --silent --accept-package-agreements --accept-source-agreements",        false, "Mídia"),
            new("kodi",       "📺", "Kodi",               "winget install XBMCFoundation.Kodi --silent --accept-package-agreements --accept-source-agreements",         false, "Mídia"),
            new("plex",       "🟡", "Plex",               "winget install Plex.Plexamp --silent --accept-package-agreements --accept-source-agreements",                false, "Mídia"),
            // ── Documentos
            new("acrobat",    "📄", "Adobe Acrobat",      "winget install Adobe.Acrobat.Reader.64-bit --silent --accept-package-agreements --accept-source-agreements", false, "Documentos"),
            new("sumatra",    "📖", "SumatraPDF",         "winget install SumatraPDF.SumatraPDF --silent --accept-package-agreements --accept-source-agreements",       false, "Documentos"),
            new("foxit",      "🦊", "Foxit PDF Reader",  "winget install Foxit.FoxitReader --silent --accept-package-agreements --accept-source-agreements",            false, "Documentos"),
            new("libreoffice","📊", "LibreOffice",        "winget install TheDocumentFoundation.LibreOffice --silent --accept-package-agreements --accept-source-agreements", false, "Documentos"),
            new("onlyoffice", "📋", "ONLYOFFICE",         "winget install ONLYOFFICE.DesktopEditors --silent --accept-package-agreements --accept-source-agreements",  false, "Documentos"),
            new("notepadpp",  "📝", "Notepad++",          "winget install Notepad++.Notepad++ --silent --accept-package-agreements --accept-source-agreements",         false, "Documentos"),
            new("obsidian",   "🪨", "Obsidian",           "winget install Obsidian.Obsidian --silent --accept-package-agreements --accept-source-agreements",           false, "Documentos"),
            new("notion",     "📓", "Notion",             "winget install Notion.Notion --silent --accept-package-agreements --accept-source-agreements",               false, "Documentos"),
            // ── Compactadores
            new("7zip",       "📦", "7-Zip",              "winget install 7zip.7zip --silent --accept-package-agreements --accept-source-agreements",                   false, "Compactadores"),
            new("winrar",     "🗜", "WinRAR",             "winget install RARLab.WinRAR --silent --accept-package-agreements --accept-source-agreements",               false, "Compactadores"),
            new("peazip",     "🤐", "PeaZip",             "winget install Giorgiotani.Peazip --silent --accept-package-agreements --accept-source-agreements",          false, "Compactadores"),
            // ── Utilitários
            new("everything", "🔍", "Everything",         "winget install voidtools.Everything --silent --accept-package-agreements --accept-source-agreements",        false, "Utilitários"),
            new("ccleaner",   "🧹", "CCleaner",           "winget install Piriform.CCleaner --silent --accept-package-agreements --accept-source-agreements",           false, "Utilitários"),
            new("revo",       "🗑",  "Revo Uninstaller",  "winget install RevoUninstaller.RevoUninstaller --silent --accept-package-agreements --accept-source-agreements", false, "Utilitários"),
            new("windirstat", "📊", "WinDirStat",         "winget install WinDirStat.WinDirStat --silent --accept-package-agreements --accept-source-agreements",       false, "Utilitários"),
            new("treesize",   "🌳", "TreeSize Free",      "winget install JAMSoftware.TreeSize.Free --silent --accept-package-agreements --accept-source-agreements",   false, "Utilitários"),
            new("sharex",     "📸", "ShareX",             "winget install ShareX.ShareX --silent --accept-package-agreements --accept-source-agreements",               false, "Utilitários"),
            new("greenshot",  "📷", "Greenshot",          "winget install Greenshot.Greenshot --silent --accept-package-agreements --accept-source-agreements",         false, "Utilitários"),
            new("autoruns",   "⚙",  "Autoruns",           "winget install Microsoft.Sysinternals.Autoruns --silent --accept-package-agreements --accept-source-agreements", false, "Utilitários"),
            // ── Diagnóstico
            new("hwinfo",     "🖥", "HWiNFO",             "winget install REALiX.HWiNFO --silent --accept-package-agreements --accept-source-agreements",               false, "Diagnóstico"),
            new("cpuz",       "🔬", "CPU-Z",              "winget install CPUID.CPU-Z --silent --accept-package-agreements --accept-source-agreements",                 false, "Diagnóstico"),
            new("gpuz",       "🎮", "GPU-Z",              "winget install TechPowerUp.GPU-Z --silent --accept-package-agreements --accept-source-agreements",           false, "Diagnóstico"),
            new("crystaldisk","💿", "CrystalDiskInfo",   "winget install CrystalDewWorld.CrystalDiskInfo --silent --accept-package-agreements --accept-source-agreements", false, "Diagnóstico"),
            new("speccy",     "🔭", "Speccy",             "winget install Piriform.Speccy --silent --accept-package-agreements --accept-source-agreements",             false, "Diagnóstico"),
            new("aida64",     "📡", "AIDA64 Extreme",    "winget install FinalWire.AIDA64.Extreme --silent --accept-package-agreements --accept-source-agreements",     false, "Diagnóstico"),
            // ── Desenvolvimento
            new("vscode",     "💻", "VS Code",            "winget install Microsoft.VisualStudioCode --silent --accept-package-agreements --accept-source-agreements",  false, "Desenvolvimento"),
            new("git",        "🌿", "Git",                "winget install Git.Git --silent --accept-package-agreements --accept-source-agreements",                     false, "Desenvolvimento"),
            new("python",     "🐍", "Python 3.12",        "winget install Python.Python.3.12 --silent --accept-package-agreements --accept-source-agreements",          false, "Desenvolvimento"),
            new("nodejs",     "🟢", "Node.js LTS",        "winget install OpenJS.NodeJS.LTS --silent --accept-package-agreements --accept-source-agreements",           false, "Desenvolvimento"),
            new("javajdk",    "☕", "Java JDK 21",        "winget install Oracle.JDK.21 --silent --accept-package-agreements --accept-source-agreements",               false, "Desenvolvimento"),
            new("putty",      "🔌", "PuTTY",              "winget install PuTTY.PuTTY --silent --accept-package-agreements --accept-source-agreements",                 false, "Desenvolvimento"),
            new("winscp",     "📂", "WinSCP",             "winget install WinSCP.WinSCP --silent --accept-package-agreements --accept-source-agreements",               false, "Desenvolvimento"),
            new("postman",    "📮", "Postman",            "winget install Postman.Postman --silent --accept-package-agreements --accept-source-agreements",             false, "Desenvolvimento"),
            new("dbeaver",    "🗄",  "DBeaver",            "winget install dbeaver.dbeaver --silent --accept-package-agreements --accept-source-agreements",             false, "Desenvolvimento"),
            new("heidisql",   "🐬", "HeidiSQL",           "winget install HeidiSQL.HeidiSQL --silent --accept-package-agreements --accept-source-agreements",           false, "Desenvolvimento"),
            // ── Gráficos
            new("gimp",       "🎨", "GIMP",               "winget install GIMP.GIMP --silent --accept-package-agreements --accept-source-agreements",                   false, "Gráficos"),
            new("inkscape",   "✒",  "Inkscape",           "winget install Inkscape.Inkscape --silent --accept-package-agreements --accept-source-agreements",           false, "Gráficos"),
            new("krita",      "🖌",  "Krita",              "winget install KDE.Krita --silent --accept-package-agreements --accept-source-agreements",                   false, "Gráficos"),
            new("paintdotnet","🖼", "Paint.NET",          "winget install dotPDN.PaintDotNet --silent --accept-package-agreements --accept-source-agreements",          false, "Gráficos"),
            new("irfanview",  "👁",  "IrfanView",          "winget install IrfanSkiljan.IrfanView --silent --accept-package-agreements --accept-source-agreements",     false, "Gráficos"),
            new("blender",    "🔶", "Blender",            "winget install BlenderFoundation.Blender --silent --accept-package-agreements --accept-source-agreements",   false, "Gráficos"),
            // ── Segurança
            new("malwarebytes","🛡","Malwarebytes",       "winget install Malwarebytes.Malwarebytes --silent --accept-package-agreements --accept-source-agreements",   false, "Segurança"),
            new("bitwarden",  "🔐", "Bitwarden",          "winget install Bitwarden.Bitwarden --silent --accept-package-agreements --accept-source-agreements",         false, "Segurança"),
            new("keepass",    "🔑", "KeePass",            "winget install KeePass.KeePass --silent --accept-package-agreements --accept-source-agreements",             false, "Segurança"),
            new("protonvpn",  "🔒", "ProtonVPN",          "winget install ProtonTechnologies.ProtonVPN --silent --accept-package-agreements --accept-source-agreements", false, "Segurança"),
            new("wireshark",  "🦈", "Wireshark",          "winget install WiresharkFoundation.Wireshark --silent --accept-package-agreements --accept-source-agreements", false, "Segurança"),
            // ── Rede / FTP
            new("filezilla",  "📁", "FileZilla",          "winget install TimKosse.FileZilla.Client --silent --accept-package-agreements --accept-source-agreements",  false, "Rede / FTP"),
            new("qbittorrent","⬇", "qBittorrent",        "winget install qBittorrent.qBittorrent --silent --accept-package-agreements --accept-source-agreements",     false, "Rede / FTP"),
            new("nmap",       "🗺",  "Nmap",               "winget install Insecure.Nmap --silent --accept-package-agreements --accept-source-agreements",               false, "Rede / FTP"),
            new("mobaxterm",  "🔷", "MobaXterm",          "winget install Mobatek.MobaXterm --silent --accept-package-agreements --accept-source-agreements",           false, "Rede / FTP"),
            // ── Games
            new("steam",      "🎮", "Steam",              "winget install Valve.Steam --silent --accept-package-agreements --accept-source-agreements",                 false, "Games"),
            new("epicgames",  "🎯", "Epic Games",         "winget install EpicGames.EpicGamesLauncher --silent --accept-package-agreements --accept-source-agreements", false, "Games"),
            new("gog",        "🎲", "GOG Galaxy",         "winget install GOG.Galaxy --silent --accept-package-agreements --accept-source-agreements",                  false, "Games"),
            new("battlenet",  "⚔",  "Battle.net",         "winget install Blizzard.BattleNet --silent --accept-package-agreements --accept-source-agreements",         false, "Games"),
            new("eadesktop",  "🟠", "EA App",             "winget install ElectronicArts.EADesktop --silent --accept-package-agreements --accept-source-agreements",   false, "Games"),
            new("ubisoft",    "🔵", "Ubisoft Connect",    "winget install Ubisoft.Connect --silent --accept-package-agreements --accept-source-agreements",             false, "Games"),
            // ── Acesso Remoto
            new("teamviewer", "🖥", "TeamViewer",         "winget install TeamViewer.TeamViewer --silent --accept-package-agreements --accept-source-agreements",       false, "Acesso Remoto"),
            new("anydesk",    "🖱",  "AnyDesk",            "winget install AnyDeskSoftwareGmbH.AnyDesk --silent --accept-package-agreements --accept-source-agreements", false, "Acesso Remoto"),
            new("rustdesk",   "🦀", "RustDesk",           "winget install RustDesk.RustDesk --silent --accept-package-agreements --accept-source-agreements",           false, "Acesso Remoto"),
            new("vncviewer",  "👁",  "VNC Viewer",         "winget install RealVNC.VNCViewer --silent --accept-package-agreements --accept-source-agreements",          false, "Acesso Remoto"),
            new("mremoteng",  "🌐", "mRemoteNG",          "winget install mRemoteNG.mRemoteNG --silent --accept-package-agreements --accept-source-agreements",         false, "Acesso Remoto"),
        };

        // ── Construtor ────────────────────────────────────────────────────────
        public FormMain()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            DoubleBuffered = true;
            Text = "TechKit Pro — Kit Técnico + Pós-Formatação";
            Size = new Size(900, 680);
            MinimumSize = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgPanel;
            Font = new Font("Segoe UI", 9.5f);

            // ── Header ──────────────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Azul,
                Padding = new Padding(16, 0, 16, 0),
            };
            var lblTitulo = new Label
            {
                Text = "⚙  TechKit Pro",
                Font = new Font("Segoe UI Semibold", 14f),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, 14),
            };
            var lblSub = new Label
            {
                Text = "Kit Técnico + Pós-Formatação Avançado",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(190, 220, 250),
                AutoSize = true,
                Location = new Point(18, 36),
            };
            header.Controls.AddRange(new Control[] { lblTitulo, lblSub });

            // ── TabControl ──────────────────────────────────────────────────
            _tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
                Padding = new Point(16, 6),
            };

            _tabs.TabPages.Add(BuildKitTab());
            _tabs.TabPages.Add(BuildPosTab());
            _tabs.TabPages.Add(BuildChecklistTab());
            _tabs.TabPages.Add(BuildHardwareTab());

            // ── Status bar ──────────────────────────────────────────────
            _lblStatusBar = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 22,
                BackColor = Color.FromArgb(30, 30, 28),
                ForeColor = Color.FromArgb(160, 160, 155),
                Font = new Font("Segoe UI", 8.5f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Text = "Pronto.",
            };

            _clock = new System.Windows.Forms.Timer { Interval = 1000 };
            _clock.Tick += (_, __) =>
                _lblStatusBar.Text = $"TechKit Pro v1.0  •  {DateTime.Now:dd/MM/yyyy  HH:mm:ss}  •  {SystemInfo.GetUptime()} ligado";
            _clock.Start();

            Controls.Add(_tabs);
            Controls.Add(header);
            Controls.Add(_lblStatusBar);

            ResumeLayout(false);
        }

        // ── KIT TÉCNICO ───────────────────────────────────────────────────────
        private TabPage BuildKitTab()
        {
            var page = new TabPage("  🔧  Kit Técnico  ");
            page.Padding = new Padding(10);
            page.BackColor = BgPanel;

            // Grade de botões de ferramentas
            _kitToolPanel = BuildToolPanel(_kitTools, _kitBtns,
                async key => await RunKitToolAsync(key));

            // Log
            _kitLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                ForeColor = Color.FromArgb(211, 209, 199),
                Font = new Font("Cascadia Code", 9f),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
            };
            AppendLog(_kitLog, "info", "[TechKit Pro] Inicializado. Selecione uma ferramenta.");

            // Progress bar
            _kitProgressLabel = new Label
            {
                Dock = DockStyle.Top,
                Text = "Aguardando...",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TextoSec,
                Height = 18,
                Padding = new Padding(2, 0, 0, 0),
            };
            _kitProgress = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 10,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 0,
            };

            // Painel de botões de ação
            var btnRunAll = MakeButton("▶  Executar Tudo", Azul, Color.White);
            btnRunAll.Click += async (_, __) => await RunAllKitToolsAsync();

            var btnClear = MakeButton("✕  Limpar Log", BgPanel, Texto);
            btnClear.FlatAppearance.BorderColor = Borda;
            btnClear.Click += (_, __) =>
            {
                _kitLog.Clear();
                AppendLog(_kitLog, "info", "[TechKit Pro] Log limpo.");
                ResetBtns(_kitBtns, _kitTools);
            };

            var btnExportKit = MakeButton("💾  Exportar Log", BgPanel, Texto);
            btnExportKit.FlatAppearance.BorderColor = Borda;
            btnExportKit.Click += (_, __) => ExportLog(_kitLog, "Log_KitTecnico");

            var actionRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 6, 0, 0),
                BackColor = BgPanel,
            };
            actionRow.Controls.Add(btnRunAll);
            actionRow.Controls.Add(btnClear);
            actionRow.Controls.Add(btnExportKit);

            // Montagem
            var logPanel = new Panel { Dock = DockStyle.Fill };
            logPanel.Controls.Add(_kitLog);
            logPanel.Controls.Add(_kitProgress);
            logPanel.Controls.Add(_kitProgressLabel);

            var splitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 220,
                Panel1MinSize = 180,
                Panel2MinSize = 120,
                BackColor = Borda,
            };
            splitter.Panel1.Controls.Add(_kitToolPanel);
            splitter.Panel2.Controls.Add(logPanel);

            page.Controls.Add(splitter);
            page.Controls.Add(actionRow);
            return page;
        }

        // ── PÓS-FORMATAÇÃO ────────────────────────────────────────────────────
        private TabPage BuildPosTab()
        {
            var page = new TabPage("  💻  Pós-Formatação  ");
            page.Padding = new Padding(10);
            page.BackColor = BgPanel;

            _posToolPanel = BuildToolPanel(_posTools, _posBtns,
                async key => await RunPosToolAsync(key));

            _posProgressLabel = new Label
            {
                Dock = DockStyle.Top,
                Text = "Aguardando...",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TextoSec,
                Height = 18,
                Padding = new Padding(2, 0, 0, 0),
            };
            _posProgress = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 10,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 0,
            };

            _posLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                ForeColor = Color.FromArgb(211, 209, 199),
                Font = new Font("Cascadia Code", 9f),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
            };
            AppendLog(_posLog, "info", "[Pós-Formatação] Módulos carregados. Pronto.");

            var btnRunAll = MakeButton("▶  Pós-Formatação Completa", Verde, Color.White);
            btnRunAll.Click += async (_, __) => await RunAllPosToolsAsync();

            var btnClear = MakeButton("✕  Limpar Log", BgPanel, Texto);
            btnClear.FlatAppearance.BorderColor = Borda;
            btnClear.Click += (_, __) =>
            {
                _posLog.Clear();
                AppendLog(_posLog, "info", "[Pós-Formatação] Log limpo.");
                ResetBtns(_posBtns, _posTools);
            };

            var btnExport = MakeButton("💾  Exportar Log", BgPanel, Texto);
            btnExport.FlatAppearance.BorderColor = Borda;
            btnExport.Click += (_, __) => ExportLog(_posLog, "Log_PosFormatacao");

            var actionRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 6, 0, 0),
                BackColor = BgPanel,
            };
            actionRow.Controls.Add(btnRunAll);
            actionRow.Controls.Add(btnClear);
            actionRow.Controls.Add(btnExport);

            var logPanel = new Panel { Dock = DockStyle.Fill };
            logPanel.Controls.Add(_posLog);
            logPanel.Controls.Add(_posProgress);
            logPanel.Controls.Add(_posProgressLabel);

            var splitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 310,
                Panel1MinSize = 220,
                Panel2MinSize = 120,
                BackColor = Borda,
            };
            splitter.Panel1.Controls.Add(_posToolPanel);
            splitter.Panel2.Controls.Add(logPanel);

            page.Controls.Add(splitter);
            page.Controls.Add(actionRow);
            return page;
        }

        // ── CHECKLIST ─────────────────────────────────────────────────────────
        private static readonly (string Label, string Categoria)[] ChecklistItems =
        {
            ("Backup dos dados realizado antes da formatação",     "Pré"),
            ("Windows instalado e licença ativada",                "Sistema"),
            ("Drivers de chipset/placa-mãe instalados",           "Drivers"),
            ("Drivers de áudio instalados",                        "Drivers"),
            ("Drivers de vídeo (GPU) instalados",                  "Drivers"),
            ("Drivers de rede (Wi-Fi / Ethernet) instalados",      "Drivers"),
            ("Windows Update executado completamente",             "Windows"),
            ("Antivírus instalado e banco de dados atualizado",    "Segurança"),
            ("Firewall do Windows habilitado",                     "Segurança"),
            ("Navegador instalado e testado",                      "Apps"),
            ("Aplicativos solicitados pelo cliente instalados",    "Apps"),
            ("Teste de disco sem erros (chkdsk)",                  "Diagnóstico"),
            ("Verificação de arquivos do sistema (sfc /scannow)",  "Diagnóstico"),
            ("Temperatura da CPU dentro do normal",                "Diagnóstico"),
            ("Rede testada — internet funcionando",                "Rede"),
            ("Dados do cliente restaurados e verificados",         "Finalização"),
            ("Ponto de restauro criado",                           "Finalização"),
            ("Serviços de inicialização otimizados",               "Finalização"),
            ("Técnico explicou ao cliente o que foi feito",        "Finalização"),
            ("Cliente assinou/aprovou o serviço",                  "Finalização"),
        };

        private readonly List<CheckBox> _checkBoxes = new();

        private TabPage BuildChecklistTab()
        {
            var page = new TabPage("  ✅  Checklist  ");
            page.BackColor = BgPanel;
            page.Padding = new Padding(12);

            // Cabeçalho com dados do atendimento
            var infoPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 70,
                ColumnCount = 3,
                BackColor = Color.FromArgb(240, 240, 238),
                Padding = new Padding(8, 6, 8, 6),
            };
            infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));

            Label MkLbl(string t) => new() { Text = t, AutoSize = true,
                Font = new Font("Segoe UI", 8f), ForeColor = TextoSec, Dock = DockStyle.Top };

            _txtCliente    = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f), PlaceholderText = "Nome do cliente" };
            _txtComputador = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f), PlaceholderText = "Computador / modelo" };
            _txtTecnico    = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f), PlaceholderText = "Técnico responsável" };

            void AddField(string lbl, TextBox txt, int col)
            {
                var wrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };
                wrap.Controls.Add(txt);
                wrap.Controls.Add(MkLbl(lbl));
                infoPanel.Controls.Add(wrap, col, 0);
            }
            AddField("Cliente", _txtCliente, 0);
            AddField("Computador", _txtComputador, 1);
            AddField("Técnico", _txtTecnico, 2);

            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
            };

            page.Controls.Add(infoPanel);  // adicionado antes do scroll

            string lastCat = "";
            int y = 8;
            foreach (var (label, cat) in ChecklistItems)
            {
                if (cat != lastCat)
                {
                    var catLabel = new Label
                    {
                        Text = cat.ToUpper(),
                        Font = new Font("Segoe UI Semibold", 8f),
                        ForeColor = TextoSec,
                        Location = new Point(8, y + 6),
                        AutoSize = true,
                    };
                    scroll.Controls.Add(catLabel);
                    y += 26;
                    lastCat = cat;
                }

                var chk = new CheckBox
                {
                    Text = "  " + label,
                    Font = new Font("Segoe UI", 9.5f),
                    ForeColor = Texto,
                    Location = new Point(8, y),
                    AutoSize = true,
                };
                _checkBoxes.Add(chk);
                scroll.Controls.Add(chk);
                y += 26;
            }

            var btnRelatorio = MakeButton("📋  Gerar Relatório TXT", Azul, Color.White);
            btnRelatorio.Click += BtnRelatorio_Click;

            var btnReset = MakeButton("↺  Resetar", BgPanel, Texto);
            btnReset.FlatAppearance.BorderColor = Borda;
            btnReset.Click += (_, __) => _checkBoxes.ForEach(c => c.Checked = false);

            var actionRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 6, 0, 0),
                BackColor = BgPanel,
            };
            actionRow.Controls.Add(btnRelatorio);
            actionRow.Controls.Add(btnReset);

            page.Controls.Add(scroll);
            page.Controls.Add(actionRow);
            return page;
        }

        private void BtnRelatorio_Click(object? sender, EventArgs e)
        {
            int total  = _checkBoxes.Count;
            int feitos = _checkBoxes.Count(c => c.Checked);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("================================================");
            sb.AppendLine("       RELATÓRIO TÉCNICO — TechKit Pro v1.0");
            sb.AppendLine("================================================");
            sb.AppendLine($"Data/Hora : {DateTime.Now:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"Cliente   : {(_txtCliente?.Text.Trim() is { Length: > 0 } c ? c : "(não informado)")}");
            sb.AppendLine($"Computador: {(_txtComputador?.Text.Trim() is { Length: > 0 } m ? m : "(não informado)")}");
            sb.AppendLine($"Técnico   : {(_txtTecnico?.Text.Trim() is { Length: > 0 } t ? t : "(não informado)")}");
            sb.AppendLine("------------------------------------------------");
            sb.AppendLine($"Sistema   : {SystemInfo.GetOsInfo()}");
            sb.AppendLine($"CPU       : {SystemInfo.GetCpuName()}");
            sb.AppendLine($"RAM       : {SystemInfo.GetRamInfo()}");
            sb.AppendLine($"GPU       : {SystemInfo.GetGpuInfo()}");
            sb.AppendLine($"Disco     : {SystemInfo.GetDiskInfo()}");
            sb.AppendLine("------------------------------------------------");
            sb.AppendLine($"CHECKLIST ({feitos}/{total} itens concluídos):");
            sb.AppendLine();

            string lastCat = "";
            int i = 0;
            foreach (var (label, cat) in ChecklistItems)
            {
                if (cat != lastCat)
                {
                    sb.AppendLine($"  [{cat.ToUpper()}]");
                    lastCat = cat;
                }
                bool ok = _checkBoxes[i++].Checked;
                sb.AppendLine($"    [{(ok ? "X" : " ")}]  {label}");
            }

            sb.AppendLine();
            sb.AppendLine("------------------------------------------------");
            sb.AppendLine($"Resultado: {feitos}/{total} itens  ({feitos * 100 / total}% concluído)");
            sb.AppendLine();
            sb.AppendLine("Assinatura do Técnico: ________________________________");
            sb.AppendLine("Assinatura do Cliente: ________________________________");
            sb.AppendLine("================================================");

            string path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"Relatorio_Tecnico_{DateTime.Now:yyyyMMdd_HHmm}.txt");
            System.IO.File.WriteAllText(path, sb.ToString());
            MessageBox.Show($"Relatório salvo em:\n{path}\n\n{feitos}/{total} itens checados.",
                "Relatório Gerado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── HARDWARE ─────────────────────────────────────────────────────────
        private TabPage BuildHardwareTab()
        {
            var page = new TabPage("  🖥  Hardware  ");
            page.BackColor = BgPanel;
            page.Padding = new Padding(12);

            var grid = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 4,
                Dock = DockStyle.Top,
                Height = 240,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int r = 0; r < 4; r++)
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

            (string Lbl, Func<string> Val)[] fields =
            {
                ("Processador",        SystemInfo.GetCpuName),
                ("Memória RAM",        SystemInfo.GetRamInfo),
                ("Sistema Operacional",SystemInfo.GetOsInfo),
                ("GPU / Vídeo",        SystemInfo.GetGpuInfo),
                ("Placa-Mãe",          SystemInfo.GetMotherboardInfo),
                ("Uptime",             SystemInfo.GetUptime),
                ("Internet",           () => SystemInfo.IsOnline() ? "✔ Online" : "✖ Offline"),
                ("Disco(s)",           SystemInfo.GetDiskInfo),
            };

            foreach (var (lbl, val) in fields)
            {
                var card = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Margin = new Padding(4),
                    Padding = new Padding(10),
                };
                card.Paint += (s, e) =>
                {
                    using var pen = new Pen(Borda);
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                };

                var titleLbl = new Label
                {
                    Text = lbl,
                    Font = new Font("Segoe UI", 8f),
                    ForeColor = TextoSec,
                    Dock = DockStyle.Top,
                    Height = 18,
                };
                var valLbl = new Label
                {
                    Text = "Carregando...",
                    Font = new Font("Segoe UI Semibold", 9.5f),
                    ForeColor = Texto,
                    Dock = DockStyle.Fill,
                    AutoSize = false,
                };
                card.Controls.Add(valLbl);
                card.Controls.Add(titleLbl);
                grid.Controls.Add(card);

                // Carrega de forma assíncrona para não travar a UI
                Task.Run(() =>
                {
                    string result;
                    try { result = val(); }
                    catch { result = "Erro ao ler"; }
                    BeginInvoke(() => valLbl.Text = result);
                });
            }

            _hwLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                ForeColor = Color.FromArgb(211, 209, 199),
                Font = new Font("Cascadia Code", 9f),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
            };
            AppendLog(_hwLog, "info", "[Hardware] Informações do sistema carregadas.");

            var btnCopiar = MakeButton("📋  Copiar Info", Azul, Color.White);
            btnCopiar.Click += (_, __) =>
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"CPU: {SystemInfo.GetCpuName()}");
                sb.AppendLine($"RAM: {SystemInfo.GetRamInfo()}");
                sb.AppendLine($"OS: {SystemInfo.GetOsInfo()}");
                sb.AppendLine($"GPU: {SystemInfo.GetGpuInfo()}");
                sb.AppendLine($"Placa-mãe: {SystemInfo.GetMotherboardInfo()}");
                sb.AppendLine($"Disco: {SystemInfo.GetDiskInfo()}");
                Clipboard.SetText(sb.ToString());
                AppendLog(_hwLog, "ok", "[Hardware] Informações copiadas para a área de transferência.");
            };

            var actionRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 6, 0, 0),
                BackColor = BgPanel,
            };
            actionRow.Controls.Add(btnCopiar);

            page.Controls.Add(_hwLog);
            page.Controls.Add(grid);
            page.Controls.Add(actionRow);
            return page;
        }

        // ── EXECUÇÃO DAS FERRAMENTAS ──────────────────────────────────────────
        private bool _kitBusy, _posBusy;

        private async Task RunKitToolAsync(string key)
        {
            if (_kitBusy) { AppendLog(_kitLog, "warn", "Aguarde a operação atual terminar."); return; }
            _kitBusy = true;
            var tool = Array.Find(_kitTools, t => t.Key == key)!;
            var btn = _kitBtns[key];

            SetBtnState(btn, $"⏳  {tool.Label}", AzulLight, Azul);
            SetProgress(true, $"Executando: {tool.Label}...");
            AppendLog(_kitLog, "info", $"[{tool.Label}] Iniciando...");

            try
            {
                await ProcessRunner.RunPowerShellAsync(
                    tool.Command,
                    line => BeginInvoke(() => AppendLog(_kitLog, "default", "  " + line)));
                AppendLog(_kitLog, "ok", $"[{tool.Label}] ✓ Concluído.");
                SetBtnState(btn, $"✓  {tool.Label}", VerdeLight, Verde);
            }
            catch (Exception ex)
            {
                AppendLog(_kitLog, "warn", $"[{tool.Label}] ⚠ {ex.Message}");
                SetBtnState(btn, $"⚠  {tool.Label}", Color.FromArgb(255, 243, 205), Color.FromArgb(180, 100, 0));
            }

            SetProgress(false, "Concluído.");
            _kitBusy = false;
        }

        private async Task RunAllKitToolsAsync()
        {
            foreach (var t in _kitTools)
                await RunKitToolAsync(t.Key);
        }

        private async Task RunPosToolAsync(string key)
        {
            if (_posBusy) { AppendLog(_posLog, "warn", "Aguarde a operação atual terminar."); return; }
            _posBusy = true;
            var tool = Array.Find(_posTools, t => t.Key == key)!;
            var btn = _posBtns[key];

            SetBtnState(btn, $"⏳  {tool.Label}", AzulLight, Azul);
            SetPosProgress(true, $"Instalando: {tool.Label}...");
            AppendLog(_posLog, "info", $"[{tool.Label}] Iniciando instalação...");

            try
            {
                await ProcessRunner.RunPowerShellAsync(
                    tool.Command,
                    line => BeginInvoke(() => AppendLog(_posLog, "default", "  " + line)));
                AppendLog(_posLog, "ok", $"[{tool.Label}] ✓ Concluído.");
                SetBtnState(btn, $"✓  {tool.Label}", VerdeLight, Verde);
            }
            catch (Exception ex)
            {
                AppendLog(_posLog, "warn", $"[{tool.Label}] ⚠ {ex.Message}");
                SetBtnState(btn, $"⚠  {tool.Label}", Color.FromArgb(255, 243, 205), Color.FromArgb(180, 100, 0));
            }

            SetPosProgress(false, "Concluído.");
            _posBusy = false;
        }

        private async Task RunAllPosToolsAsync()
        {
            foreach (var t in _posTools)
                await RunPosToolAsync(t.Key);
        }

        // ── HELPERS DE UI ─────────────────────────────────────────────────────
        private static Panel BuildToolPanel(
            ToolDef[] tools,
            Dictionary<string, Button> btns,
            Func<string, Task> onClick)
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true,
                Padding = new Padding(8),
                BackColor = BgPanel,
            };

            string lastCat = "";
            foreach (var tool in tools)
            {
                if (tool.Category.Length > 0 && tool.Category != lastCat)
                {
                    var catLabel = new Label
                    {
                        Text = "  " + tool.Category.ToUpper(),
                        Width = 3000,
                        Height = 24,
                        Font = new Font("Segoe UI Semibold", 7.5f),
                        ForeColor = TextoSec,
                        BackColor = Color.FromArgb(236, 236, 232),
                        TextAlign = ContentAlignment.MiddleLeft,
                        Margin = new Padding(0, lastCat.Length == 0 ? 0 : 8, 0, 2),
                    };
                    panel.Controls.Add(catLabel);
                    lastCat = tool.Category;
                }

                var btn = new Button
                {
                    Text = $"{tool.Icon}  {tool.Label}",
                    Width = 172,
                    Height = 52,
                    Margin = new Padding(4),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White,
                    ForeColor = Texto,
                    Font = new Font("Segoe UI", 9f),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(8, 0, 0, 0),
                    Cursor = Cursors.Hand,
                };
                btn.FlatAppearance.BorderColor = Borda;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.MouseOverBackColor = AzulLight;
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 225, 245);
                string key = tool.Key;
                btn.Click += async (_, __) => await onClick(key);
                btns[key] = btn;
                panel.Controls.Add(btn);
            }
            return panel;
        }

        private static Button MakeButton(string text, Color bg, Color fg)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = fg,
                Font = new Font("Segoe UI Semibold", 9.5f),
                Padding = new Padding(12, 4, 12, 4),
                Cursor = Cursors.Hand,
                Height = 34,
                Margin = new Padding(0, 0, 8, 0),
            };
            btn.FlatAppearance.BorderColor = bg == BgPanel ? Borda : bg;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = bg == BgPanel
                ? Color.FromArgb(235, 235, 232)
                : ControlPaint.Light(bg, 0.3f);
            btn.FlatAppearance.MouseDownBackColor = bg == BgPanel
                ? Color.FromArgb(220, 220, 215)
                : ControlPaint.Dark(bg, 0.05f);
            return btn;
        }

        private void SetProgress(bool active, string label)
        {
            BeginInvoke(() =>
            {
                _kitProgressLabel.Text = label;
                _kitProgress.MarqueeAnimationSpeed = active ? 30 : 0;
                _kitProgress.Style = active ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
                if (!active) _kitProgress.Value = 0;
            });
        }

        private void SetBtnState(Button btn, string text, Color bg, Color fg)
        {
            if (btn.InvokeRequired) { btn.BeginInvoke(() => SetBtnState(btn, text, bg, fg)); return; }
            btn.Text = text;
            btn.BackColor = bg;
            btn.ForeColor = fg;
            btn.FlatAppearance.BorderColor = fg;
        }

        private void SetPosProgress(bool active, string label)
        {
            BeginInvoke(() =>
            {
                _posProgressLabel.Text = label;
                _posProgress.MarqueeAnimationSpeed = active ? 30 : 0;
                _posProgress.Style = active ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
                if (!active) _posProgress.Value = 0;
            });
        }

        private static void ExportLog(RichTextBox rtb, string prefix)
        {
            string path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"{prefix}_{DateTime.Now:yyyyMMdd_HHmm}.txt");
            System.IO.File.WriteAllText(path, rtb.Text);
            MessageBox.Show($"Log salvo em:\n{path}", "Exportar Log",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ResetBtns(Dictionary<string, Button> btns, ToolDef[] tools)
        {
            foreach (var t in tools)
            {
                var btn = btns[t.Key];
                btn.Text = $"{t.Icon}  {t.Label}";
                btn.BackColor = Color.White;
                btn.ForeColor = Texto;
                btn.FlatAppearance.BorderColor = Borda;
            }
        }

        private static readonly Dictionary<string, Color> LogColors = new()
        {
            { "info",    Color.FromArgb(133, 183, 235) },  // azul
            { "ok",      Color.FromArgb(93, 202, 165) },   // verde
            { "warn",    Color.FromArgb(239, 159, 39) },   // âmbar
            { "default", Color.FromArgb(211, 209, 199) },  // cinza claro
        };

        private static void AppendLog(RichTextBox rtb, string level, string text)
        {
            if (rtb.InvokeRequired) { rtb.BeginInvoke(() => AppendLog(rtb, level, text)); return; }
            string ts = DateTime.Now.ToString("HH:mm:ss");
            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;
            rtb.SelectionColor = Color.FromArgb(80, 80, 75);
            rtb.AppendText($"[{ts}] ");
            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;
            rtb.SelectionColor = LogColors.GetValueOrDefault(level, LogColors["default"]);
            rtb.AppendText(text + "\n");
            rtb.ScrollToCaret();
        }
    }
}
