using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace KitTecnicoPro.Models
{
    public class ScriptItem : INotifyPropertyChanged
    {
        public string Nome       { get; set; } = string.Empty;
        public string Descricao  { get; set; } = string.Empty;
        public string Categoria  { get; set; } = "Personalizado";
        public string Caminho    { get; set; } = string.Empty;
        public bool   RunAsAdmin { get; set; }

        [JsonIgnore]
        public bool Existe => !string.IsNullOrWhiteSpace(Caminho) && File.Exists(Caminho);

        [JsonIgnore]
        public string CategoriaIcon => Categoria switch
        {
            "Sistema"    => "🖥️",
            "Rede"       => "🌐",
            "Segurança"  => "🔒",
            "Manutenção" => "🔧",
            _            => "📜"
        };

        [JsonIgnore]
        public string ExisteColor => Existe ? "#4CAF50" : "#EF5350";

        [JsonIgnore]
        public string ExisteTexto => Existe ? "✅" : "❌ Arquivo não encontrado";

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Refresh()
        {
            OnPropertyChanged(nameof(Existe));
            OnPropertyChanged(nameof(ExisteColor));
            OnPropertyChanged(nameof(ExisteTexto));
        }
    }
}
