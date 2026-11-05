using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace KitTecnicoPro.Models
{
    public class IsoItem : INotifyPropertyChanged
    {
        private static readonly SolidColorBrush BrushDownloading = CreateFrozenBrush(0xFF, 0xA7, 0x26);
        private static readonly SolidColorBrush BrushDownloaded  = CreateFrozenBrush(0x4C, 0xAF, 0x50);
        private static readonly SolidColorBrush BrushIdle        = CreateFrozenBrush(0x37, 0x47, 0x4F);

        private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        public string Nome         { get; set; } = string.Empty;
        public string Versao       { get; set; } = string.Empty;
        public string Arq          { get; set; } = "x64";
        public string Categoria    { get; set; } = string.Empty;
        public string Url          { get; set; } = string.Empty;
        public bool   IsDirectLink { get; set; } = true;
        public long   TamanhoMB    { get; set; }
        public string Descricao    { get; set; } = string.Empty;

        private string? _localPath;
        public string? LocalPath
        {
            get => _localPath;
            set { _localPath = value; OnPropertyChanged(); Refresh(); }
        }

        private int _progresso;
        [JsonIgnore]
        public int Progresso
        {
            get => _progresso;
            set { if (_progresso == value) return; _progresso = value; OnPropertyChanged(); Refresh(); }
        }

        private bool _isDownloading;
        [JsonIgnore]
        public bool IsDownloading
        {
            get => _isDownloading;
            set { if (_isDownloading == value) return; _isDownloading = value; OnPropertyChanged(); Refresh(); }
        }

        [JsonIgnore]
        public bool IsDownloaded => !string.IsNullOrEmpty(LocalPath) && File.Exists(LocalPath);

        [JsonIgnore]
        public string TamanhoTexto => TamanhoMB >= 1024 ? $"{TamanhoMB / 1024.0:F1} GB" : $"{TamanhoMB} MB";

        [JsonIgnore]
        public string CategoriaIcon => Categoria switch
        {
            "Windows"     => "🪟",
            "Linux"       => "🐧",
            "BSD"         => "🐡",
            "Outros"      => "🖥️",
            "Utilitários" => "🔧",
            _             => "💿"
        };

        [JsonIgnore]
        public string StatusText => IsDownloading ? $"⬇ {Progresso}%" : IsDownloaded ? "✅ Disponível" : "—";

        [JsonIgnore]
        public SolidColorBrush StatusBrush =>
            IsDownloading ? BrushDownloading :
            IsDownloaded  ? BrushDownloaded  : BrushIdle;

        private void Refresh()
        {
            OnPropertyChanged(nameof(IsDownloaded));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusBrush));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
