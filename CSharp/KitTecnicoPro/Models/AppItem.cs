using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MaterialDesignThemes.Wpf;

namespace KitTecnicoPro.Models
{
    /// <summary>Textos padronizados de status exibidos nos cards.</summary>
    public static class AppStatus
    {
        public const string Installed  = "Instalado";
        public const string Failed     = "Falhou";
        public const string Installing = "Instalando...";
        public const string Waiting    = "Aguardando";
        public const string Downloaded = "Baixado";
    }

    /// <summary>Representa um aplicativo gerenciado pelo Kit Técnico Pro.</summary>
    public partial class AppItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isInstalled;
        private string _status = string.Empty;
        private string? _localPath;
        private bool _hasLocalPathCache;
        private string _nome = string.Empty;
        private string _id = string.Empty;
        private string _categoria = "Geral";
        private string _tipo = "winget";
        private string? _url;
        private string? _silentArgs;
        private string _descricao = string.Empty;
        private string? _iconUrl;

        [GeneratedRegex(@"^[^\w\p{L}]+\s*")]
        private static partial Regex LeadingSymbolsRegex();

        [JsonPropertyName("nome")]
        public string Nome
        {
            get => _nome;
            set
            {
                if (_nome == value) return;
                _nome = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AppIcon));
                OnPropertyChanged(nameof(ResolvedIconKind));
            }
        }

        [JsonPropertyName("id")]
        public string Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("categoria")]
        public string Categoria
        {
            get => _categoria;
            set
            {
                if (_categoria == value) return;
                _categoria = string.IsNullOrWhiteSpace(value) ? "Geral" : value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CategoriaIcon));
                OnPropertyChanged(nameof(AppIcon));
                OnPropertyChanged(nameof(ResolvedIconKind));
                OnPropertyChanged(nameof(CategoriaLimpa));
            }
        }

        /// <summary>Tipo de instalação: "winget", "custom" ou "local"</summary>
        [JsonPropertyName("tipo")]
        public string Tipo
        {
            get => _tipo;
            set
            {
                if (_tipo == value) return;
                _tipo = string.IsNullOrWhiteSpace(value) ? "winget" : value;
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("url")]
        public string? Url
        {
            get => _url;
            set
            {
                if (_url == value) return;
                _url = value;
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("silentArgs")]
        public string? SilentArgs
        {
            get => _silentArgs;
            set
            {
                if (_silentArgs == value) return;
                _silentArgs = value;
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("iconUrl")]
        public string? IconUrl
        {
            get => _iconUrl;
            set
            {
                if (_iconUrl == value) return;
                _iconUrl = value;
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("localPath")]
        public string? LocalPath
        {
            get => _localPath;
            set
            {
                _localPath = value;
                UpdateLocalPathState();
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("descricao")]
        public string Descricao
        {
            get => _descricao;
            set
            {
                if (_descricao == value) return;
                _descricao = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public PackIconKind CategoriaIcon => GetCategoryIcon(Categoria);

        [JsonIgnore]
        public PackIconKind AppIcon => GetAppIcon(Nome) ?? CategoriaIcon;

        [JsonIgnore]
        public PackIconKind ResolvedIconKind => AppIcon;

        [JsonIgnore]
        public string CategoriaLimpa
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_categoria)) return "Geral";
                string cleaned = LeadingSymbolsRegex().Replace(_categoria, "").Trim();
                return string.IsNullOrEmpty(cleaned) ? _categoria.Trim() : cleaned;
            }
        }

        private static readonly ConcurrentDictionary<string, PackIconKind?> _appIconCache = new(StringComparer.OrdinalIgnoreCase);

        private static readonly (string Key, PackIconKind Icon)[] _appIconMap =
        {
            ("chrome", PackIconKind.GoogleChrome),
            ("firefox", PackIconKind.Firefox),
            ("edge", PackIconKind.MicrosoftEdge),
            ("opera", PackIconKind.Opera),
            ("spotify", PackIconKind.Spotify),
            ("discord", PackIconKind.Chat),
            ("whatsapp", PackIconKind.Whatsapp),
            ("telegram", PackIconKind.MessageText),
            ("skype", PackIconKind.Skype),
            ("slack", PackIconKind.Slack),
            ("zoom", PackIconKind.Video),
            ("vlc", PackIconKind.Vlc),
            ("steam", PackIconKind.Steam),
            ("epic", PackIconKind.GamepadVariant),
            ("github", PackIconKind.Github),
            ("git", PackIconKind.Git),
            ("vscode", PackIconKind.MicrosoftVisualStudioCode),
            ("visual studio", PackIconKind.MicrosoftVisualStudio),
            ("node", PackIconKind.Nodejs),
            ("java", PackIconKind.LanguageJava),
            ("python", PackIconKind.LanguagePython),
            ("docker", PackIconKind.Docker),
            ("obs ", PackIconKind.Video),
            ("winrar", PackIconKind.ZipBox),
            ("7-zip", PackIconKind.ZipBox),
            ("adobe", PackIconKind.Image),
            ("office", PackIconKind.MicrosoftOffice),
            ("word", PackIconKind.FileWord),
            ("excel", PackIconKind.FileExcel),
            ("powerpoint", PackIconKind.FilePowerpoint),
            ("dropbox", PackIconKind.Dropbox),
            ("onedrive", PackIconKind.MicrosoftOnedrive),
            ("google drive", PackIconKind.GoogleDrive),
            ("anydesk", PackIconKind.RemoteDesktop),
            ("teamviewer", PackIconKind.RemoteDesktop),
            ("brave", PackIconKind.ShieldCheck),
            ("tor", PackIconKind.ShieldCheck)
        };

        private static PackIconKind? GetAppIcon(string? nome)
        {
            if (string.IsNullOrWhiteSpace(nome)) return null;
            return _appIconCache.GetOrAdd(nome, n =>
            {
                string lower = n.ToLowerInvariant();
                foreach (var (key, icon) in _appIconMap)
                    if (lower.Contains(key)) return icon;
                return null;
            });
        }

        private static readonly ConcurrentDictionary<string, PackIconKind> _iconCache = new(StringComparer.OrdinalIgnoreCase);

        private static readonly (string Key, PackIconKind Icon)[] _iconMap =
        {
            ("naveg", PackIconKind.Web),             ("segur", PackIconKind.ShieldCheck),
            ("antiv", PackIconKind.ShieldBug),       ("vpn", PackIconKind.Vpn),
            ("remot", PackIconKind.RemoteDesktop),   ("comunic", PackIconKind.MessageText),
            ("multim", PackIconKind.PlayCircle),     ("áudio", PackIconKind.Music),
            ("audio", PackIconKind.Music),           ("músic", PackIconKind.Music),
            ("vídeo", PackIconKind.Video),           ("video", PackIconKind.Video),
            ("imag", PackIconKind.Image),            ("design", PackIconKind.Palette),
            ("gráf", PackIconKind.Palette),          ("3d", PackIconKind.CubeOutline),
            ("desenv", PackIconKind.CodeBraces),     ("program", PackIconKind.CodeBraces),
            ("banco", PackIconKind.Database),        ("dados", PackIconKind.Database),
            ("game", PackIconKind.GamepadVariant),   ("jogo", PackIconKind.GamepadVariant),
            ("office", PackIconKind.FileDocument),   ("escrit", PackIconKind.FileDocument),
            ("produt", PackIconKind.ClipboardCheck), ("pdf", PackIconKind.FilePdfBox),
            ("compact", PackIconKind.ZipBox),        ("arquiv", PackIconKind.Archive),
            ("driver", PackIconKind.Chip),           ("hardware", PackIconKind.Memory),
            ("diagn", PackIconKind.Stethoscope),     ("monitor", PackIconKind.Gauge),
            ("limp", PackIconKind.Broom),            ("otimi", PackIconKind.Speedometer),
            ("backup", PackIconKind.BackupRestore),  ("recupe", PackIconKind.Restore),
            ("disco", PackIconKind.Harddisk),        ("armazen", PackIconKind.Harddisk),
            ("nuvem", PackIconKind.Cloud),           ("cloud", PackIconKind.Cloud),
            ("rede", PackIconKind.Lan),              ("internet", PackIconKind.Earth),
            ("download", PackIconKind.Download),     ("torrent", PackIconKind.Download),
            ("runtime", PackIconKind.Cogs),          ("framework", PackIconKind.Cogs),
            ("sistema", PackIconKind.MicrosoftWindows), ("util", PackIconKind.Tools),
            ("ferram", PackIconKind.Tools),          ("educa", PackIconKind.School),
            ("finan", PackIconKind.Finance),         ("virtual", PackIconKind.Monitor),
            ("leitor", PackIconKind.BookOpenVariant), ("email", PackIconKind.Email),
        };

        private static PackIconKind GetCategoryIcon(string? categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria)) return PackIconKind.FolderOutline;
            return _iconCache.GetOrAdd(categoria, cat =>
            {
                string lower = cat.ToLowerInvariant();
                foreach (var (key, icon) in _iconMap)
                    if (lower.Contains(key)) return icon;
                return PackIconKind.Apps;
            });
        }

        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected == value) return; _isSelected = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public bool IsInstalled
        {
            get => _isInstalled;
            set
            {
                if (_isInstalled == value) return;
                _isInstalled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusColor));
                if (value && string.IsNullOrEmpty(_status)) Status = AppStatus.Installed;
            }
        }

        [JsonIgnore]
        public string Status
        {
            get => _status;
            set
            {
                if (_status == value) return;
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(HasStatus));
            }
        }

        [JsonIgnore]
        public bool HasStatus => !string.IsNullOrEmpty(_status);

        [JsonIgnore]
        public string StatusColor
        {
            get
            {
                if (!string.IsNullOrEmpty(_status))
                {
                    if (_status == AppStatus.Failed ||
                        _status.Contains("Falhou", StringComparison.OrdinalIgnoreCase) ||
                        _status.Contains("Erro", StringComparison.OrdinalIgnoreCase))
                        return "#B91C1C";

                    if (_status == AppStatus.Installing ||
                        _status == AppStatus.Waiting ||
                        _status.Contains("Instalando", StringComparison.OrdinalIgnoreCase) ||
                        _status.Contains("Aguardando", StringComparison.OrdinalIgnoreCase))
                        return "#B45309";

                    if (_status == AppStatus.Downloaded ||
                        _status.Contains("Baixado", StringComparison.OrdinalIgnoreCase) ||
                        _status.Contains("Download", StringComparison.OrdinalIgnoreCase))
                        return "#7C3AED";

                    if (_status == AppStatus.Installed ||
                        _status.Contains("Instalado", StringComparison.OrdinalIgnoreCase))
                        return "#15803D";
                }

                return IsInstalled ? "#15803D" : "#475569";
            }
        }

        [JsonIgnore]
        public bool HasLocalPath => _hasLocalPathCache;

        public void UpdateLocalPathState()
        {
            _hasLocalPathCache = !string.IsNullOrWhiteSpace(_localPath) && File.Exists(_localPath);
            OnPropertyChanged(nameof(HasLocalPath));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
