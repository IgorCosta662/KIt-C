using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using KitTecnicoPro.Models;

namespace KitTecnicoPro.Services
{
    /// <summary>Executa comandos e scripts PowerShell de forma assíncrona com validação por Regex gerada em compilação.</summary>
    public partial class PowerShellService
    {
        [GeneratedRegex(@"^[A-Za-z0-9._\-+]+$")]
        private static partial Regex SafeWingetIdRegex();

        [GeneratedRegex(@"\b(Format-Volume|Clear-Disk|Initialize-Disk|Remove-Partition)\b|\bRemove-Item\s+.*(-Recurse|-r).*c:\\windows|\brm\s+-rf\s+/|\bdel\s+/f\s+/s\s+/q\s+c:\\windows", RegexOptions.IgnoreCase)]
        private static partial Regex DangerousPatternsRegex();

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();

        /// <summary>Valida se um ID do winget contém apenas caracteres seguros.</summary>
        public static bool IsSafeWingetId(string? id)
            => !string.IsNullOrWhiteSpace(id) && SafeWingetIdRegex().IsMatch(id);

        /// <summary>Escapa aspas simples para strings literais do PowerShell.</summary>
        public static string EscapeSingleQuotes(string? s)
            => (s ?? string.Empty).Replace("'", "''");

        private static string EncodePowerShellCommand(string command)
        {
            string fullScript = $"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; {command}";
            return Convert.ToBase64String(Encoding.Unicode.GetBytes(fullScript));
        }

        /// <summary>Executa um comando PowerShell e retorna output linha a linha via callback.</summary>
        public async Task<bool> ExecuteAsync(
            string command,
            Action<string, LogLevel>? outputCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (!ValidateCommand(command))
            {
                outputCallback?.Invoke("⛔ Comando bloqueado por política de segurança.", LogLevel.Error);
                return false;
            }

            string encoded = EncodePowerShellCommand(command);

            var psi = new ProcessStartInfo
            {
                FileName               = "powershell.exe",
                Arguments              = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encoded}",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding  = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    outputCallback?.Invoke(e.Data, LogLevel.Info);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    outputCallback?.Invoke(e.Data, LogLevel.Warning);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(cancellationToken);
                return process.ExitCode == 0;
            }
            catch (OperationCanceledException)
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
                return false;
            }
        }

        /// <summary>Executa um comando PowerShell e captura todo o stdout como texto.</summary>
        public async Task<string> ExecuteAndCaptureAsync(
            string command,
            CancellationToken cancellationToken = default)
        {
            if (!ValidateCommand(command))
                return string.Empty;

            string encoded = EncodePowerShellCommand(command);

            var psi = new ProcessStartInfo
            {
                FileName               = "powershell.exe",
                Arguments              = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encoded}",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding  = Encoding.UTF8
            };

            var sb = new StringBuilder();
            var sync = new object();

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    lock (sync) sb.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
            }

            lock (sync) return sb.ToString();
        }

        /// <summary>Executa um arquivo de script .ps1 externo (plugin).</summary>
        public async Task<bool> ExecuteScriptFileAsync(
            string scriptPath,
            Action<string, LogLevel>? outputCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(scriptPath))
            {
                outputCallback?.Invoke($"Script não encontrado: {scriptPath}", LogLevel.Error);
                return false;
            }

            if (!ValidateScriptFile(scriptPath))
            {
                outputCallback?.Invoke($"⛔ Script bloqueado por conter comandos perigosos: {scriptPath}", LogLevel.Error);
                return false;
            }

            string command = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{scriptPath}\"";

            var psi = new ProcessStartInfo
            {
                FileName               = "powershell.exe",
                Arguments              = command,
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding  = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    outputCallback?.Invoke(e.Data, LogLevel.Info);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    outputCallback?.Invoke(e.Data, LogLevel.Warning);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(cancellationToken);
                return process.ExitCode == 0;
            }
            catch (OperationCanceledException)
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
                return false;
            }
        }

        private static bool ValidateCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return false;
            string normalized = WhitespaceRegex().Replace(command, " ");
            return !DangerousPatternsRegex().IsMatch(normalized);
        }

        private static bool ValidateScriptFile(string path)
        {
            if (!File.Exists(path)) return false;
            string content = File.ReadAllText(path);
            string normalized = WhitespaceRegex().Replace(content, " ");
            return !DangerousPatternsRegex().IsMatch(normalized);
        }
    }
}
