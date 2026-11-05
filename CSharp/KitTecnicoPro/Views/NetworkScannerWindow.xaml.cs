using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace KitTecnicoPro.Views
{
    public class HostEntry
    {
        public string Ip         { get; set; } = "";
        public string Hostname   { get; set; } = "";
        public long   PingMs     { get; set; }
        public string OpenPorts  { get; set; } = "";
        public bool   Online     { get; set; }

        public string StatusIcon => Online ? "🟢" : "🔴";
        public string PingColor  => PingMs < 10 ? "#4CAF50" : PingMs < 50 ? "#FFA726" : "#EF5350";
    }

    public partial class NetworkScannerWindow : Window
    {
        private readonly ObservableCollection<HostEntry> _results = new();
        private CancellationTokenSource? _cts;
        private static readonly int[] PortsToCheck = { 80, 443, 3389, 22, 445, 8080 };

        public NetworkScannerWindow()
        {
            InitializeComponent();

            this.SourceInitialized += (s, e) =>
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch { }
            };
            this.Closed += (_, _) =>
            {
                try { _cts?.Cancel(); } catch { }
                _cts?.Dispose();
            };
            LstHosts.ItemsSource = _results;
            DetectSubnet();
        }

        private void DetectSubnet()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        string ip = addr.Address.ToString();
                        var parts = ip.Split('.');
                        if (parts.Length == 4)
                        {
                            TxtSubnet.Text = $"{parts[0]}.{parts[1]}.{parts[2]}";
                            return;
                        }
                    }
                }
            }
            catch { }
        }

        private async void OnScanClick(object sender, RoutedEventArgs e)
        {
            string subnet = TxtSubnet.Text.Trim().TrimEnd('.');
            if (string.IsNullOrEmpty(subnet) || subnet.Split('.').Length != 3)
            {
                TxtStatus.Text = "⚠ Informe uma sub-rede válida (ex: 192.168.1)";
                return;
            }

            int timeout = int.TryParse((CmbTimeout.SelectedItem as ComboBoxItem)?.Content?.ToString(), out int tOut) ? tOut : 500;
            bool checkPorts = ChkPorts.IsChecked == true;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            _results.Clear();
            PrgScan.Value = 0;
            BtnScan.IsEnabled = false;
            TxtStatus.Text = $"🔍 Escaneando {subnet}.1 — {subnet}.254 ...";

            int online = 0, scanned = 0;
            using var sem = new SemaphoreSlim(40);

            var tasks = Enumerable.Range(1, 254).Select(async i =>
            {
                try
                {
                    await sem.WaitAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                try
                {
                    if (ct.IsCancellationRequested) return;
                    string ip = $"{subnet}.{i}";
                    using var ping = new Ping();
                    PingReply? reply = null;
                    try { reply = await ping.SendPingAsync(ip, timeout); } catch { }

                    bool alive = reply?.Status == IPStatus.Success;
                    int s = Interlocked.Increment(ref scanned);

                    if (alive && !ct.IsCancellationRequested)
                    {
                        Interlocked.Increment(ref online);
                        string host = ip;
                        try { host = (await Dns.GetHostEntryAsync(ip)).HostName; } catch { }

                        string ports = "—";
                        if (checkPorts && !ct.IsCancellationRequested)
                        {
                            var openList = new List<string>();
                            foreach (int port in PortsToCheck)
                            {
                                if (ct.IsCancellationRequested) break;
                                try
                                {
                                    using var tcp = new TcpClient();
                                    using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                                    connectCts.CancelAfter(300);
                                    await tcp.ConnectAsync(ip, port, connectCts.Token);
                                    if (tcp.Connected)
                                        openList.Add(port.ToString());
                                }
                                catch { }
                            }
                            ports = openList.Count > 0 ? string.Join(", ", openList) : "—";
                        }

                        var entry = new HostEntry
                        {
                            Ip        = ip,
                            Hostname  = host,
                            PingMs    = reply!.RoundtripTime,
                            OpenPorts = ports,
                            Online    = true
                        };

                        _ = Dispatcher.BeginInvoke(() => _results.Add(entry));
                    }

                    _ = Dispatcher.BeginInvoke(() =>
                    {
                        PrgScan.Value = s;
                        TxtPct.Text   = $"{s}/254";
                    });
                }
                finally
                {
                    sem.Release();
                }
            });

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) { }

            BtnScan.IsEnabled = true;
            TxtStatus.Text = ct.IsCancellationRequested
                ? "⛔ Scan interrompido."
                : $"✅ Scan concluído — {online} dispositivo(s) online de 254 endereços";
        }

        private void OnStopClick(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            BtnScan.IsEnabled = true;
            TxtStatus.Text = "⛔ Scan interrompido pelo usuário.";
        }

        private void OnCopyClick(object sender, RoutedEventArgs e)
        {
            if (_results.Count == 0) { TxtStatus.Text = "⚠ Nenhum resultado para copiar."; return; }
            var sb = new StringBuilder();
            sb.AppendLine($"=== Scan de Rede — {DateTime.Now:dd/MM/yyyy HH:mm:ss} ===");
            sb.AppendLine($"Sub-rede: {TxtSubnet.Text}.0/24");
            sb.AppendLine();
            sb.AppendLine($"{"IP",-16} {"Hostname",-30} {"Ping(ms)",-10} Portas");
            sb.AppendLine(new string('-', 80));
            foreach (var h in _results.OrderBy(x => x.Ip))
                sb.AppendLine($"{h.Ip,-16} {h.Hostname,-30} {h.PingMs,-10} {h.OpenPorts}");
            Clipboard.SetText(sb.ToString());
            TxtStatus.Text = "✅ Resultados copiados para a área de transferência.";
        }
    }
}
