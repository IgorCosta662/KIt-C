using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace KitTecnicoPro.Models
{
    public class ToolItem : INotifyPropertyChanged
    {
        private string _nome = string.Empty;
        private string _descricao = string.Empty;
        private string _categoria = "Ferramentas";
        private string _caminho = string.Empty;
        private string _args = string.Empty;
        private bool _runAsAdmin;

        public string Nome
        {
            get => _nome;
            set { if (_nome == value) return; _nome = value ?? string.Empty; OnPropertyChanged(); }
        }

        public string Descricao
        {
            get => _descricao;
            set { if (_descricao == value) return; _descricao = value ?? string.Empty; OnPropertyChanged(); }
        }

        public string Categoria
        {
            get => _categoria;
            set
            {
                if (_categoria == value) return;
                _categoria = value ?? "Ferramentas";
                OnPropertyChanged();
                OnPropertyChanged(nameof(CategoriaIcon));
            }
        }

        public string Caminho
        {
            get => _caminho;
            set
            {
                if (_caminho == value) return;
                _caminho = value ?? string.Empty;
                OnPropertyChanged();
                Refresh();
            }
        }

        public string Args
        {
            get => _args;
            set { if (_args == value) return; _args = value ?? string.Empty; OnPropertyChanged(); }
        }

        public bool RunAsAdmin
        {
            get => _runAsAdmin;
            set { if (_runAsAdmin == value) return; _runAsAdmin = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public bool Existe => !string.IsNullOrWhiteSpace(Caminho) && File.Exists(Caminho);

        [JsonIgnore]
        public string CategoriaIcon => Categoria switch
        {
            "Ferramentas" => "🔧",
            "Plugins"     => "🧩",
            "Cracks"      => "🔓",
            _             => "📁"
        };

        [JsonIgnore]
        public string ExisteColor => Existe ? "#4CAF50" : "#EF5350";

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Refresh()
        {
            OnPropertyChanged(nameof(Existe));
            OnPropertyChanged(nameof(ExisteColor));
        }
    }
}
