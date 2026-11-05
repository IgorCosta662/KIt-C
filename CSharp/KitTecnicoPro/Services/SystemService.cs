using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Principal;
using KitTecnicoPro.Models;

namespace KitTecnicoPro.Services
{
    /// <summary>Coleta informações do sistema e verifica requisitos de ambiente.</summary>
    public class SystemService
    {
        /// <summary>Retorna true se o processo está rodando com privilégios de administrador.</summary>
        public static bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>Verifica conectividade com a internet via ping.</summary>
        public static bool HasInternetConnection()
        {
            try
            {
                using var ping = new Ping();
                var reply = ping.Send("8.8.8.8", 2000);
                return reply.Status == IPStatus.Success;
            }
            catch { return false; }
        }

        /// <summary>Versão assíncrona (não bloqueia a UI).</summary>
        public static async Task<bool> HasInternetConnectionAsync()
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync("8.8.8.8", 2000);
                return reply.Status == IPStatus.Success;
            }
            catch { return false; }
        }

        /// <summary>Coleta informações completas do hardware e sistema operacional.</summary>
        public static SystemInfo GetSystemInfo()
        {
            var info = new SystemInfo
            {
                MachineName = Environment.MachineName,
                UserName    = Environment.UserName,
                IsAdmin     = IsAdministrator(),
                HasInternet = HasInternetConnection()
            };

            // Sistema Operacional
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Caption, Version, TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                using (obj)
                {
                    info.OsName    = obj["Caption"]?.ToString() ?? "Windows";
                    info.OsVersion = obj["Version"]?.ToString() ?? "";

                    ulong totalRam = Convert.ToUInt64(obj["TotalVisibleMemorySize"]);
                    ulong freeRam  = Convert.ToUInt64(obj["FreePhysicalMemory"]);
                    info.RamTotalGB      = Math.Round(totalRam / 1024.0 / 1024.0, 1);
                    info.RamFreeGB       = Math.Round(freeRam  / 1024.0 / 1024.0, 1);
                    info.RamUsedGB       = Math.Round(info.RamTotalGB - info.RamFreeGB, 1);
                    info.RamUsagePercent = info.RamTotalGB > 0 ? Math.Round(info.RamUsedGB / info.RamTotalGB * 100, 1) : 0;
                }
            }
            catch { /* Ignora erros de WMI */ }

            // CPU
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfLogicalProcessors, LoadPercentage FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                using (obj)
                {
                    info.CpuName  = obj["Name"]?.ToString()?.Trim() ?? "N/A";
                    info.CpuCores = Convert.ToInt32(obj["NumberOfLogicalProcessors"]);
                    info.CpuUsagePercent = Convert.ToDouble(obj["LoadPercentage"]);
                }
            }
            catch { /* Ignora erros de WMI */ }

            // Discos
            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                    {
                        double total  = Math.Round(drive.TotalSize / 1024.0 / 1024.0 / 1024.0, 1);
                        double free   = Math.Round(drive.TotalFreeSpace / 1024.0 / 1024.0 / 1024.0, 1);
                        double used   = Math.Round(total - free, 1);
                        double pct    = total > 0 ? Math.Round(used / total * 100, 1) : 0;

                        info.Disks.Add(new DiskInfo
                        {
                            Drive       = drive.Name,
                            Label       = drive.VolumeLabel,
                            TotalGB     = total,
                            FreeGB      = free,
                            UsedPercent = pct
                        });
                    }
                }
            }
            catch { /* Ignora erros de disco */ }

            // GPU (prefere dedicada e lê 64-bit qwMemorySize quando >= 4 GB)
            try
            {
                ulong bestVram = 0;
                string bestGpu = "";
                using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                using (obj)
                {
                    string name = obj["Name"]?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(name)) continue;

                    ulong vram = Convert.ToUInt64(obj["AdapterRAM"] ?? 0UL);
                    bool isVirtual = name.Contains("Basic Display", StringComparison.OrdinalIgnoreCase)
                                  || name.Contains("Virtual", StringComparison.OrdinalIgnoreCase)
                                  || name.Contains("Remote", StringComparison.OrdinalIgnoreCase);

                    if (string.IsNullOrEmpty(bestGpu) || (!isVirtual && vram >= bestVram))
                    {
                        bestGpu  = name;
                        bestVram = vram;
                    }
                }

                try
                {
                    using var gpuClassKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                        @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
                    if (gpuClassKey != null)
                    {
                        foreach (var subName in gpuClassKey.GetSubKeyNames())
                        {
                            if (!subName.StartsWith("0")) continue;
                            using var sub = gpuClassKey.OpenSubKey(subName);
                            if (sub?.GetValue("HardwareInformation.qwMemorySize") is long qw && qw > (long)bestVram)
                                bestVram = (ulong)qw;
                            else if (sub?.GetValue("HardwareInformation.qwMemorySize") is byte[] bytes && bytes.Length >= 8)
                            {
                                ulong val = BitConverter.ToUInt64(bytes, 0);
                                if (val > bestVram) bestVram = val;
                            }
                        }
                    }
                }
                catch { /* Ignora falha de leitura no registro */ }

                info.GpuName   = bestGpu;
                info.GpuVramGB = Math.Round(bestVram / 1024.0 / 1024.0 / 1024.0, 1);
            }
            catch { /* Ignora erros de WMI */ }

            // Rede — adaptadores ativos
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    info.NetworkAdapters.Add(ni.Name);

                    if (string.IsNullOrEmpty(info.LocalIp))
                    {
                        foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                info.LocalIp = addr.Address.ToString();
                                break;
                            }
                        }
                    }
                }
            }
            catch { /* Ignora */ }

            return info;
        }

        /// <summary>Executa limpeza de arquivos temporários do sistema.</summary>
        public static async Task CleanSystemAsync(Action<string, LogLevel> log, CancellationToken ct)
        {
            log("🧹 Iniciando limpeza do sistema...", LogLevel.Info);

            string[] tempDirs =
            {
                Environment.GetEnvironmentVariable("TEMP") ?? "",
                Environment.GetEnvironmentVariable("TMP") ?? "",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
            };

            long totalFreed = 0;

            foreach (var dir in tempDirs.Where(d => !string.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!Directory.Exists(dir)) continue;

                log($"  Limpando: {dir}", LogLevel.Info);
                string current = dir;
                totalFreed += await Task.Run(() => DeleteTempFiles(current, log), ct);
            }

            // Esvazia a lixeira
            log("  🗑 Esvaziando Lixeira...", LogLevel.Info);
            try
            {
                await new PowerShellService().ExecuteAsync(
                    "Clear-RecycleBin -Force -ErrorAction SilentlyContinue",
                    null, ct);
                log("  ✅ Lixeira esvaziada", LogLevel.Success);
            }
            catch { /* Ignora */ }

            double mb = totalFreed / 1024.0 / 1024.0;
            log($"✅ Limpeza concluída. Espaço recuperado: {mb:F1} MB", LogLevel.Success);
        }

        private static long DeleteTempFiles(string dir, Action<string, LogLevel> log)
        {
            long freed = 0;
            try
            {
                foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        freed += fi.Length;
                        fi.Delete();
                    }
                    catch { /* Arquivo em uso, ignora */ }
                }
                foreach (var subDir in Directory.GetDirectories(dir))
                {
                    try { Directory.Delete(subDir, true); } catch { /* Ignora */ }
                }
            }
            catch (Exception ex)
            {
                log($"  ⚠️ Aviso ao limpar {dir}: {ex.Message}", LogLevel.Warning);
            }
            return freed;
        }
    }
}
