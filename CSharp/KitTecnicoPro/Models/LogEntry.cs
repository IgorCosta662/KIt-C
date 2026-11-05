namespace KitTecnicoPro.Models
{
    public enum LogLevel { Info, Success, Warning, Error }

    /// <summary>Entrada individual do painel de logs e auditoria estruturada.</summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public LogLevel Level { get; set; } = LogLevel.Info;
        public string Category { get; set; } = "App";
        public string Message { get; set; } = string.Empty;

        public string LevelIcon => Level switch
        {
            LogLevel.Info    => "[INFO] ",
            LogLevel.Success => "[OK]   ",
            LogLevel.Warning => "[AVISO]",
            LogLevel.Error   => "[ERRO] ",
            _                => "[LOG]  "
        };

        public string LevelColor => Level switch
        {
            LogLevel.Info    => "#90CAF9",
            LogLevel.Success => "#A5D6A7",
            LogLevel.Warning => "#FFE082",
            LogLevel.Error   => "#EF9A9A",
            _                => "#FFFFFF"
        };

        public string FormattedTime => Timestamp.ToString("HH:mm:ss");
    }
}
