using System.Text.Json.Serialization;

namespace KitTecnicoPro.Models
{
    /// <summary>Configurações fortemente tipadas da aplicação (Data/settings.json).</summary>
    public sealed class AppSettings
    {
        [JsonPropertyName("onlineAppsJsonUrl")]
        public string OnlineAppsJsonUrl { get; set; } = string.Empty;

        [JsonPropertyName("maxParallelInstalls")]
        public int MaxParallelInstalls { get; set; } = 3;

        [JsonPropertyName("parallelModeDefault")]
        public bool ParallelModeDefault { get; set; }

        [JsonPropertyName("installTimeoutMinutes")]
        public int InstallTimeoutMinutes { get; set; } = 15;

        [JsonPropertyName("autoCheckInstalled")]
        public bool AutoCheckInstalled { get; set; }

        [JsonPropertyName("createRestorePoint")]
        public bool CreateRestorePoint { get; set; } = true;

        [JsonPropertyName("logRetentionDays")]
        public int LogRetentionDays { get; set; } = 14;

        [JsonPropertyName("httpTimeoutMinutes")]
        public int HttpTimeoutMinutes { get; set; } = 240;
    }

    /// <summary>Registro estruturado gravado em JSON Lines (.jsonl) para auditoria e telemetria.</summary>
    public sealed class StructuredLogRecord
    {
        [JsonPropertyName("ts")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonPropertyName("lvl")]
        public string Level { get; set; } = "INFO";

        [JsonPropertyName("cat")]
        public string Category { get; set; } = "App";

        [JsonPropertyName("msg")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("ex")]
        public string? Exception { get; set; }
    }
}
