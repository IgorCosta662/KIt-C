using System.Text.Json.Serialization;

namespace KitTecnicoPro.Models
{
    /// <summary>Perfil de instalação que agrupa um conjunto de aplicativos.</summary>
    public class Profile
    {
        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("descricao")]
        public string Descricao { get; set; } = string.Empty;

        [JsonPropertyName("icone")]
        public string Icone { get; set; } = "👤";

        /// <summary>Lista de IDs (winget) dos programas incluídos neste perfil.</summary>
        [JsonPropertyName("apps")]
        public List<string> Apps { get; set; } = new();

        public override string ToString() => $"{Icone} {Nome}";
    }
}
