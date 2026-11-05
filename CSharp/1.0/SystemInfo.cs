using System;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;

namespace TechKitPro
{
    public static class SystemInfo
    {
        public static string GetCpuName()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                    return obj["Name"]?.ToString()?.Trim() ?? "Desconhecido";
            }
            catch { }
            return "N/A";
        }

        public static string GetRamInfo()
        {
            try
            {
                ulong total = 0;
                using var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory");
                foreach (ManagementObject obj in searcher.Get())
                    total += Convert.ToUInt64(obj["Capacity"]);
                double gb = total / (1024.0 * 1024 * 1024);
                return $"{gb:F1} GB";
            }
            catch { }
            return "N/A";
        }

        public static string GetOsInfo()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                    return $"{obj["Caption"]} (Build {obj["Version"]})";
            }
            catch { }
            return Environment.OSVersion.ToString();
        }

        public static string GetDiskInfo()
        {
            var sb = new StringBuilder();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Model, Size, MediaType FROM Win32_DiskDrive");
                foreach (ManagementObject obj in searcher.Get())
                {
                    double gb = Convert.ToDouble(obj["Size"]) / (1024.0 * 1024 * 1024);
                    sb.AppendLine($"  • {obj["Model"]} — {gb:F0} GB ({obj["MediaType"]})");
                }
            }
            catch { }
            return sb.Length > 0 ? sb.ToString().TrimEnd() : "N/A";
        }

        public static string GetGpuInfo()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                    return obj["Name"]?.ToString() ?? "N/A";
            }
            catch { }
            return "N/A";
        }

        public static string GetMotherboardInfo()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Manufacturer, Product FROM Win32_BaseBoard");
                foreach (ManagementObject obj in searcher.Get())
                    return $"{obj["Manufacturer"]} {obj["Product"]}";
            }
            catch { }
            return "N/A";
        }

        public static string GetUptime()
        {
            TimeSpan up = TimeSpan.FromMilliseconds(Environment.TickCount64);
            return $"{(int)up.TotalHours}h {up.Minutes}m";
        }

        public static bool IsOnline()
        {
            try
            {
                using var ping = new Ping();
                var reply = ping.Send("8.8.8.8", 1500);
                return reply.Status == IPStatus.Success;
            }
            catch { return false; }
        }

        public static string GetNetworkAdapterInfo()
        {
            var sb = new StringBuilder();
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    sb.AppendLine($"  • {ni.Name} ({ni.NetworkInterfaceType}) — {ni.Speed / 1_000_000} Mbps");
                }
            }
            return sb.Length > 0 ? sb.ToString().TrimEnd() : "Sem adaptadores ativos";
        }
    }
}
