namespace KitTecnicoPro.Models
{
    /// <summary>Informações do sistema coletadas para exibição na tela de diagnóstico.</summary>
    public class SystemInfo
    {
        public string CpuName { get; set; } = string.Empty;
        public int CpuCores { get; set; }
        public double CpuUsagePercent { get; set; }
        public double RamTotalGB { get; set; }
        public double RamUsedGB { get; set; }
        public double RamFreeGB { get; set; }
        public double RamUsagePercent { get; set; }
        public string OsName { get; set; } = string.Empty;
        public string OsVersion { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool HasInternet { get; set; }
        public List<DiskInfo> Disks { get; set; } = new();

        public string GpuName { get; set; } = string.Empty;
        public double GpuVramGB { get; set; }
        public List<string> NetworkAdapters { get; set; } = new();
        public string LocalIp { get; set; } = string.Empty;
    }

    public class DiskInfo
    {
        public string Drive { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public double TotalGB { get; set; }
        public double FreeGB { get; set; }
        public double UsedPercent { get; set; }
    }
}
