using KitTecnicoPro.Models;

namespace KitTecnicoPro.Services
{
    /// <summary>Contrato de logging estruturado e assíncrono para todos os serviços e ViewModels.</summary>
    public interface IAppLogger : IDisposable, IAsyncDisposable
    {
        /// <summary>Disparado sempre que uma nova entrada de log é registrada.</summary>
        event Action<LogEntry>? EntryLogged;

        void Log(string message, LogLevel level = LogLevel.Info, string category = "App", Exception? exception = null);

        void Info(string message, string category = "App") => Log(message, LogLevel.Info, category);
        void Success(string message, string category = "App") => Log(message, LogLevel.Success, category);
        void Warn(string message, string category = "App") => Log(message, LogLevel.Warning, category);
        void Error(string message, Exception? ex = null, string category = "App") => Log(message, LogLevel.Error, category, ex);

        string LogFilePath { get; }
        string JsonLogFilePath { get; }
    }
}
