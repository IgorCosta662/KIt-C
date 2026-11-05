using System.Text.Json.Serialization;

namespace KitTecnicoPro.Models
{
    public class CredentialEntry
    {
        public Guid     Id        { get; set; } = Guid.NewGuid();
        public string   Titulo    { get; set; } = "";
        public string   Categoria { get; set; } = "📝 Nota";
        public string   Usuario   { get; set; } = "";
        public string   SenhaEnc  { get; set; } = "";   // AES-256 criptografado + Base64
        public string   Notas     { get; set; } = "";
        public string   SubTitulo { get; set; } = "";   // URL, IP, host
        public DateTime Criado    { get; set; } = DateTime.Now;

        [JsonIgnore]
        public string CatIcon => Categoria.Split(' ').FirstOrDefault() ?? "📝";
    }
}
