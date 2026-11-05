using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace KitTecnicoPro.Views
{
    public class ProcessEntry
    {
        public string Name    { get; set; } = "";
        public int    Pid     { get; set; }
        public double RamMB   { get; set; }
        public double CpuPct  { get; set; }
        public int    Threads { get; set; }
        public string Path    { get; set; } = "";

        public string RamMbText  => $"{RamMB:F1} MB";
        public double RamBarPct  => Math.Min(RamMB / 5, 100);
        public string RamColor   => RamMB > 500 ? "#EF5350" : RamMB > 200 ? "#FFA726" : "#4CAF50";
        public string CpuText    => $"{CpuPct:F1}%";
        public string CpuColor   => CpuPct > 50 ? "#EF5350" : CpuPct > 20 ? "#FFA726" : "#4CAF50";
    }

    public partial class ProcessManagerWindow : Window
    {
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(3) };
        private List<ProcessEntry> _all = new();
        private string _busca = "";
        private int _sortIdx = 2;
        private bool _isLoading;

        public ProcessManagerWindow()
        {
            InitializeComponent();

            this.SourceInitialized += (s, e) =>
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch { }
            };
            this.Closed += (_, _) => _timer.Stop();
            _timer.Tick += async (_, _) => await LoadProcessesAsync();
            _timer.Start();
            _ = LoadProcessesAsync();
        }

        private async Task LoadProcessesAsync()
        {
            if (_isLoading) return;
            _isLoading = true;
            try
            {
                var list = await Task.Run(() =>
                {
                    var cpuMap = new Dictionary<int, double>();
                    try
                    {
                        using var s = new ManagementObjectSearcher(
                            "SELECT IDProcess, PercentProcessorTime FROM Win32_PerfFormattedData_PerfProc_Process");
                        using var results = s.Get();
                        foreach (ManagementObject o in results)
                        {
                            using (o)
                            {
                                int pid  = Convert.ToInt32(o["IDProcess"]);
                                double c = Convert.ToDouble(o["PercentProcessorTime"]);
                                cpuMap[pid] = c;
                            }
                        }
                    }
                    catch { /* WMI pode falhar sem admin */ }

                    var procs = Process.GetProcesses();
                    var entries = new List<ProcessEntry>(procs.Length);
                    foreach (var p in procs)
                    {
                        using (p)
                        {
                            double ram = 0, cpu = 0;
                            string path = "";
                            int threads = 0;
                            try { ram     = p.WorkingSet64 / 1024.0 / 1024.0; } catch { }
                            try { threads = p.Threads.Count; } catch { }
                            try { path    = p.MainModule?.FileName ?? ""; } catch { }
                            cpuMap.TryGetValue(p.Id, out cpu);
                            entries.Add(new ProcessEntry
                            {
                                Name    = p.ProcessName,
                                Pid     = p.Id,
                                RamMB   = Math.Round(ram, 1),
                                CpuPct  = Math.Round(cpu, 1),
                                Threads = threads,
                                Path    = path
                            });
                        }
                    }
                    return entries;
                });

                _all = list;
                ApplyFilter();
            }
            catch { }
            finally
            {
                _isLoading = false;
            }
        }

        private void ApplyFilter()
        {
            if (LstProcessos == null) return;

            var filtered = string.IsNullOrEmpty(_busca)
                ? _all
                : _all.Where(p => p.Name.Contains(_busca, StringComparison.OrdinalIgnoreCase)).ToList();

            var sorted = _sortIdx switch
            {
                0 => filtered.OrderBy(p => p.Name).ToList(),
                1 => filtered.OrderBy(p => p.Pid).ToList(),
                2 => filtered.OrderByDescending(p => p.RamMB).ToList(),
                3 => filtered.OrderByDescending(p => p.CpuPct).ToList(),
                _ => filtered
            };

            LstProcessos.ItemsSource = sorted;

            double totalRam = _all.Sum(p => p.RamMB) / 1024.0;
            TxtResumo.Text = $"{_all.Count} processos  •  RAM total: {totalRam:F1} GB  •  " +
                             $"Atualizado: {DateTime.Now:HH:mm:ss}";
        }

        private async void OnRefreshClick(object s, RoutedEventArgs e) => await LoadProcessesAsync();

        private void OnAutoToggle(object s, RoutedEventArgs e)
        {
            if (TglAuto.IsChecked == true) _timer.Start();
            else                           _timer.Stop();
        }

        private void OnBuscaChanged(object s, TextChangedEventArgs e)
        {
            _busca = TxtBusca.Text.Trim();
            ApplyFilter();
        }

        private void OnSortChanged(object s, SelectionChangedEventArgs e)
        {
            _sortIdx = CmbSort.SelectedIndex;
            ApplyFilter();
        }

        private async void OnKillClick(object sender, RoutedEventArgs e)
        {
            if (LstProcessos.SelectedItem is not ProcessEntry entry) return;

            if (MessageBox.Show($"Encerrar o processo '{entry.Name}' (PID {entry.Pid})?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                using var proc = Process.GetProcessById(entry.Pid);
                proc.Kill(entireProcessTree: true);
                TxtStatus.Text = $"✅ '{entry.Name}' encerrado.";
                await LoadProcessesAsync();
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Erro: {ex.Message}";
            }
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            _timer.Stop();
        }
    }
}
