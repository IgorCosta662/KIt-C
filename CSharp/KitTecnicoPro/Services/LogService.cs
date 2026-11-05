using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using KitTecnicoPro.Models;
using KitTecnicoPro.Serialization;

namespace KitTecnicoPro.Services
{
    /// <summary>
    /// Serviço de logging estruturado assíncrono baseado em <see cref="Channel{T}"/> (.NET 8).
    /// Grava simultaneamente em texto legível (.log) e JSON Lines (.jsonl) com rotação automática,
    /// sem bloquear a thread de UI nem as tasks de instalação.
    /// </summary>
    public sealed class LogService : IAppLogger
    {
        private readonly record struct QueuedLog(LogEntry Entry, string Category, Exception? Exception);

        private readonly Channel<QueuedLog> _channel;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _writerTask;
        private int _disposed;

        public string LogFilePath { get; }
        public string JsonLogFilePath { get; }

        public event Action<LogEntry>? EntryLogged;

        public LogService(int retentionDays = 14)
        {
            string logDir = AppPaths.LogsDir;
            Directory.CreateDirectory(logDir);

            string dateStamp = DateTime.Now.ToString("yyyy-MM-dd");
            LogFilePath     = Path.Combine(logDir, $"ktp_{dateStamp}.log");
            JsonLogFilePath = Path.Combine(logDir, $"ktp_{dateStamp}.jsonl");

            _channel = Channel.CreateBounded<QueuedLog>(new BoundedChannelOptions(4096)
            {
                FullMode     = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

            PruneOldLogs(logDir, retentionDays);
            _writerTask = Task.Run(() => ProcessQueueAsync(_cts.Token));
        }

        public void Log(string message, LogLevel level = LogLevel.Info, string category = "App", Exception? exception = null)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level     = level,
                Category  = category,
                Message   = message
            };

            EnqueueInternal(entry, category, exception, raiseEvent: true);
        }

        /// <summary>Compatibilidade direta: grava no arquivo sem disparar evento duplicado quando a UI já adicionou o item.</summary>
        public void WriteToFile(LogEntry entry, string category = "App", Exception? exception = null)
            => EnqueueInternal(entry, string.IsNullOrWhiteSpace(entry.Category) ? category : entry.Category, exception, raiseEvent: false);

        private void EnqueueInternal(LogEntry entry, string category, Exception? exception, bool raiseEvent)
        {
            if (Volatile.Read(ref _disposed) != 0) return;
            _channel.Writer.TryWrite(new QueuedLog(entry, category, exception));

            if (raiseEvent)
            {
                try { EntryLogged?.Invoke(entry); } catch { }
            }
        }

        private async Task ProcessQueueAsync(CancellationToken ct)
        {
            var textBatch = new StringBuilder(4096);
            var jsonBatch = new StringBuilder(4096);
            var reader    = _channel.Reader;

            try
            {
                while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
                {
                    textBatch.Clear();
                    jsonBatch.Clear();

                    while (textBatch.Length < 64 * 1024 && reader.TryRead(out var item))
                    {
                        string lvlTag = item.Entry.Level switch
                        {
                            LogLevel.Success => "OK   ",
                            LogLevel.Warning => "WARN ",
                            LogLevel.Error   => "ERROR",
                            _                => "INFO "
                        };

                        textBatch.Append('[')
                                 .Append(item.Entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                                 .Append("] [")
                                 .Append(lvlTag)
                                 .Append("] [")
                                 .Append(item.Category)
                                 .Append("] ")
                                 .AppendLine(item.Entry.Message);

                        if (item.Exception != null)
                            textBatch.AppendLine(item.Exception.ToString());

                        var record = new StructuredLogRecord
                        {
                            Timestamp = new DateTimeOffset(item.Entry.Timestamp),
                            Level     = lvlTag.Trim(),
                            Category  = item.Category,
                            Message   = item.Entry.Message,
                            Exception = item.Exception?.ToString()
                        };

                        jsonBatch.AppendLine(JsonSerializer.Serialize(record, KitJsonCompactContext.Utf8.StructuredLogRecord));
                    }

                    if (textBatch.Length > 0)
                    {
                        try { await File.AppendAllTextAsync(LogFilePath, textBatch.ToString(), Encoding.UTF8, CancellationToken.None).ConfigureAwait(false); }
                        catch { }
                    }

                    if (jsonBatch.Length > 0)
                    {
                        try { await File.AppendAllTextAsync(JsonLogFilePath, jsonBatch.ToString(), Encoding.UTF8, CancellationToken.None).ConfigureAwait(false); }
                        catch { }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch { }
        }

        private static void PruneOldLogs(string logDir, int retentionDays)
        {
            if (retentionDays <= 0) return;
            try
            {
                DateTime cutoff = DateTime.Now.AddDays(-retentionDays);
                foreach (var file in Directory.EnumerateFiles(logDir, "ktp_*.*"))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        if (fi.LastWriteTime < cutoff) fi.Delete();
                    }
                    catch { }
                }
            }
            catch { }
        }

        public string GetLogFilePath() => LogFilePath;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _channel.Writer.TryComplete();
            try
            {
                await _writerTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }
            catch { }
            _cts.Cancel();
            _cts.Dispose();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _channel.Writer.TryComplete();
            try { _writerTask.Wait(TimeSpan.FromSeconds(2)); } catch { }
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
