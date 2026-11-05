namespace KitTecnicoPro.Models
{
    /// <summary>Resultado de uma operação de instalação individual.</summary>
    public class InstallationResult
    {
        public string AppName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
    }
}
