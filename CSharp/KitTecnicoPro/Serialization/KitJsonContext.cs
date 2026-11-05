using System.Collections.ObjectModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using KitTecnicoPro.Models;

namespace KitTecnicoPro.Serialization
{
    /// <summary>
    /// Source Generator do System.Text.Json (.NET 8) — elimina reflexão em tempo de execução,
    /// acelera a inicialização e reduz alocações de memória ao ler/gravar JSON.
    /// </summary>
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip)]
    [JsonSerializable(typeof(AppItem))]
    [JsonSerializable(typeof(List<AppItem>))]
    [JsonSerializable(typeof(Profile))]
    [JsonSerializable(typeof(List<Profile>))]
    [JsonSerializable(typeof(IsoItem))]
    [JsonSerializable(typeof(List<IsoItem>))]
    [JsonSerializable(typeof(ObservableCollection<IsoItem>))]
    [JsonSerializable(typeof(ScriptItem))]
    [JsonSerializable(typeof(List<ScriptItem>))]
    [JsonSerializable(typeof(ObservableCollection<ScriptItem>))]
    [JsonSerializable(typeof(ToolItem))]
    [JsonSerializable(typeof(List<ToolItem>))]
    [JsonSerializable(typeof(ObservableCollection<ToolItem>))]
    [JsonSerializable(typeof(CredentialEntry))]
    [JsonSerializable(typeof(List<CredentialEntry>))]
    [JsonSerializable(typeof(AppSettings))]
    public partial class KitJsonContext : JsonSerializerContext
    {
        /// <summary>Instância pré-configurada que preserva acentuação UTF-8 legível sem escapes \uXXXX.</summary>
        public static KitJsonContext IndentedUtf8 { get; } = new(new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    /// <summary>Contexto compacto (uma linha) para logs estruturados .jsonl e cofre criptografado.</summary>
    [JsonSourceGenerationOptions(
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(StructuredLogRecord))]
    [JsonSerializable(typeof(List<CredentialEntry>))]
    public partial class KitJsonCompactContext : JsonSerializerContext
    {
        public static KitJsonCompactContext Utf8 { get; } = new(new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }
}
