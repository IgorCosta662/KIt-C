using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using KitTecnicoPro.Models;
using KitTecnicoPro.Services;

namespace KitTecnicoPro.ViewModels
{
    /// <summary>ViewModel do Dashboard — atualiza métricas de hardware em tempo real usando PeriodicTimer (.NET 8).</summary>
    public class DashboardViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private double _cpuUsage;
        private double _ramUsedGB;
        private double _ramTotalGB;
        private double _ramUsagePercent;
        private string _cpuName          = string.Empty;
        private string _osName           = string.Empty;
        private string _suggestedProfile = string.Empty;
        private string _suggestedReason  = string.Empty;
        private bool   _isAdmin;
        private bool   _hasInternet;
        private int    _isRefreshing;

        public ObservableCollection<DiskInfo> Disks { get; } = new();

        public double CpuUsage
        {
            get => _cpuUsage;
            set { _cpuUsage = value; OnPropertyChanged(); OnPropertyChanged(nameof(CpuColor)); }
        }

        public double RamUsedGB
        {
            get => _ramUsedGB;
            set { _ramUsedGB = value; OnPropertyChanged(); }
        }

        public double RamTotalGB
        {
            get => _ramTotalGB;
            set { _ramTotalGB = value; OnPropertyChanged(); }
        }

        public double RamUsagePercent
        {
            get => _ramUsagePercent;
            set { _ramUsagePercent = value; OnPropertyChanged(); OnPropertyChanged(nameof(RamColor)); }
        }

        public string CpuName
        {
            get => _cpuName;
            set { _cpuName = value; OnPropertyChanged(); }
        }

        public string OsName
        {
            get => _osName;
            set { _osName = value; OnPropertyChanged(); }
        }

        public string SuggestedProfile
        {
            get => _suggestedProfile;
            set { _suggestedProfile = value; OnPropertyChanged(); }
        }

        public string SuggestedReason
        {
            get => _suggestedReason;
            set { _suggestedReason = value; OnPropertyChanged(); }
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set { _isAdmin = value; OnPropertyChanged(); }
        }

        public bool HasInternet
        {
            get => _hasInternet;
            set { _hasInternet = value; OnPropertyChanged(); }
        }

        public string CpuColor => GetUsageColor(CpuUsage);
        public string RamColor => GetUsageColor(RamUsagePercent);

        public DashboardViewModel()
        {
            _ = RunTelemetryLoopAsync(_cts.Token);
        }

        private async Task RunTelemetryLoopAsync(CancellationToken ct)
        {
            await RefreshAsync();

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    await RefreshAsync();
                }
            }
            catch (OperationCanceledException) { }
        }

        public async Task RefreshAsync()
        {
            if (Interlocked.CompareExchange(ref _isRefreshing, 1, 0) != 0) return;
            try
            {
                var info = await Task.Run(SystemService.GetSystemInfo, _cts.Token);
                if (_cts.IsCancellationRequested) return;

                CpuName         = info.CpuName;
                CpuUsage        = info.CpuUsagePercent;
                RamUsedGB       = info.RamUsedGB;
                RamTotalGB      = info.RamTotalGB;
                RamUsagePercent = info.RamUsagePercent;
                OsName          = $"{info.OsName} (v{info.OsVersion})";
                IsAdmin         = info.IsAdmin;
                HasInternet     = info.HasInternet;

                Disks.Clear();
                foreach (var d in info.Disks) Disks.Add(d);

                SuggestProfile(info);
            }
            catch { }
            finally
            {
                Volatile.Write(ref _isRefreshing, 0);
            }
        }

        private void SuggestProfile(SystemInfo info)
        {
            if (info.RamTotalGB >= 16)
            {
                SuggestedProfile = "💻 Programador";
                SuggestedReason  = $"RAM: {info.RamTotalGB}GB — sistema de alta capacidade";
            }
            else if (info.RamTotalGB >= 8)
            {
                bool likelyGamer = info.CpuName.Contains("i7", StringComparison.OrdinalIgnoreCase) ||
                                   info.CpuName.Contains("i9", StringComparison.OrdinalIgnoreCase) ||
                                   info.CpuName.Contains("Ryzen 7", StringComparison.OrdinalIgnoreCase) ||
                                   info.CpuName.Contains("Ryzen 9", StringComparison.OrdinalIgnoreCase);
                if (likelyGamer)
                {
                    SuggestedProfile = "🎮 Gamer";
                    SuggestedReason  = $"CPU: {string.Join(' ', info.CpuName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(3))} — perfil de alto desempenho";
                }
                else
                {
                    SuggestedProfile = "📄 Escritório";
                    SuggestedReason  = $"RAM: {info.RamTotalGB}GB — uso intermediário";
                }
            }
            else
            {
                SuggestedProfile = "🖥️ Básico";
                SuggestedReason  = $"RAM: {info.RamTotalGB}GB — recomendado setup essencial";
            }
        }

        private static string GetUsageColor(double pct) => pct switch
        {
            > 85 => "#EF9A9A",
            > 60 => "#FFE082",
            _    => "#A5D6A7"
        };

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            GC.SuppressFinalize(this);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
