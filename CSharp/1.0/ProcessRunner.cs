using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace TechKitPro
{
    public static class ProcessRunner
    {
        /// <summary>Executa um comando PowerShell e retorna o output.</summary>
        public static async Task<string> RunPowerShellAsync(
            string command,
            Action<string>? progressCallback = null)
        {
            return await Task.Run(() =>
            {
                var sb = new StringBuilder();
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -Command \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                };

                using var proc = new Process { StartInfo = psi };
                proc.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        sb.AppendLine(e.Data);
                        progressCallback?.Invoke(e.Data);
                    }
                };
                proc.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        progressCallback?.Invoke("[ERRO] " + e.Data);
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();

                return sb.ToString();
            });
        }

        /// <summary>Executa um comando CMD simples.</summary>
        public static async Task<string> RunCmdAsync(string command)
        {
            return await Task.Run(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding(850),
                };
                using var proc = Process.Start(psi)!;
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                return output;
            });
        }

        /// <summary>Abre um executável elevado (como Admin).</summary>
        public static void RunAsAdmin(string exe, string args = "")
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                Verb = "runas",
                UseShellExecute = true,
            });
        }

        /// <summary>Instala um aplicativo via winget.</summary>
        public static async Task<string> WingetInstallAsync(
            string packageId,
            Action<string>? progressCallback = null)
        {
            return await RunPowerShellAsync(
                $"winget install --id {packageId} --silent --accept-package-agreements --accept-source-agreements",
                progressCallback);
        }
    }
}
