using System.Management;
using System.Windows;
using System.Windows.Controls;

namespace KitTecnicoPro.Views
{
    public class ServiceEntry
    {
        public string Name        { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string State       { get; set; } = "";
        public string StartMode   { get; set; } = "";
        public string PathName    { get; set; } = "";

        public string StatusIcon   => State == "Running" ? "🟢" : State == "Stopped" ? "🔴" : "🟡";
        public string StatusBg     => State == "Running" ? "#1B5E20" : State == "Stopped" ? "#B71C1C" : "#E65100";
        public string StartModeColor => StartMode == "Auto"     ? "#4CAF50"
                                      : StartMode == "Disabled" ? "#EF5350" : "#94A3B8";
    }

    public partial class ServiceManagerWindow : Window
    {
        private List<ServiceEntry> _all  = new();
        private string _busca  = "";
        private string _filter = "Todos";

        public ServiceManagerWindow()
        {
            InitializeComponent();

            this.SourceInitialized += (s, e) =>
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch { }
            };
            _ = LoadServicesAsync();
        }

        private async Task LoadServicesAsync()
        {
            TxtResumo.Text = "Carregando...";
            try
            {
                var list = await Task.Run(() =>
                {
                    var items = new List<ServiceEntry>();
                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service");
                    using var results = searcher.Get();
                    foreach (ManagementObject obj in results)
                    {
                        using (obj)
                        {
                            items.Add(new ServiceEntry
                            {
                                Name        = obj["Name"]?.ToString()        ?? "",
                                DisplayName = obj["DisplayName"]?.ToString() ?? "",
                                State       = obj["State"]?.ToString()       ?? "",
                                StartMode   = obj["StartMode"]?.ToString()   ?? "",
                                PathName    = obj["PathName"]?.ToString()    ?? ""
                            });
                        }
                    }
                    return items.OrderBy(s => s.Name).ToList();
                });

                _all = list;
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Erro WMI: {ex.Message}";
            }

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (LstServicos == null) return;

            var view = _all.AsEnumerable();

            if (!string.IsNullOrEmpty(_busca))
                view = view.Where(s => s.Name.Contains(_busca, StringComparison.OrdinalIgnoreCase)
                                    || s.DisplayName.Contains(_busca, StringComparison.OrdinalIgnoreCase));

            view = _filter switch
            {
                "Em execução" => view.Where(s => s.State == "Running"),
                "Parado"      => view.Where(s => s.State == "Stopped"),
                "Automático"  => view.Where(s => s.StartMode == "Auto"),
                "Manual"      => view.Where(s => s.StartMode == "Manual"),
                "Desativado"  => view.Where(s => s.StartMode == "Disabled"),
                _             => view
            };

            var list = view.ToList();
            LstServicos.ItemsSource = list;
            int running = _all.Count(s => s.State == "Running");
            TxtResumo.Text = $"{_all.Count} serviços  •  {running} em execução  •  {_all.Count - running} parados";
        }

        private void OnBuscaChanged(object s, TextChangedEventArgs e)
        { _busca = TxtBusca.Text.Trim(); ApplyFilter(); }

        private void OnFilterChanged(object s, SelectionChangedEventArgs e)
        {
            _filter = (CmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
            ApplyFilter();
        }

        private async void OnRefreshClick(object s, RoutedEventArgs e) => await LoadServicesAsync();

        private ServiceEntry? GetSelected() => LstServicos.SelectedItem as ServiceEntry;

        private async Task InvokeServiceAsync(string method, string successMsg)
        {
            var entry = GetSelected();
            if (entry == null) { TxtStatus.Text = "⚠ Selecione um serviço primeiro."; return; }
            TxtStatus.Text = $"⏳ Executando {method} em '{entry.DisplayName}'...";
            try
            {
                uint returnCode = await Task.Run(() =>
                {
                    uint rc = 1;
                    string safeName = entry.Name.Replace("'", "\\'");
                    using var searcher = new ManagementObjectSearcher(
                        $"SELECT * FROM Win32_Service WHERE Name='{safeName}'");
                    using var results = searcher.Get();
                    foreach (ManagementObject obj in results)
                    {
                        using (obj)
                        {
                            var res = obj.InvokeMethod(method, null);
                            rc = res != null ? Convert.ToUInt32(res) : 0;
                        }
                    }
                    return rc;
                });

                if (returnCode == 0 || returnCode == 10) // 10 = already running/stopped
                    TxtStatus.Text = $"✅ {successMsg}: '{entry.DisplayName}'";
                else
                    TxtStatus.Text = $"⚠ Falha ao alterar '{entry.DisplayName}' (código WMI {returnCode} — execute como Admin).";

                await LoadServicesAsync();
            }
            catch (Exception ex) { TxtStatus.Text = $"❌ Erro: {ex.Message}"; }
        }

        private async void OnStartClick(object s, RoutedEventArgs e) => await InvokeServiceAsync("StartService", "Iniciado");
        private async void OnStopClick(object s, RoutedEventArgs e)  => await InvokeServiceAsync("StopService",  "Parado");

        private async void OnRestartClick(object sender, RoutedEventArgs e)
        {
            var entry = GetSelected();
            if (entry == null) { TxtStatus.Text = "⚠ Selecione um serviço."; return; }
            TxtStatus.Text = $"⏳ Reiniciando '{entry.DisplayName}'...";
            try
            {
                uint returnCode = await Task.Run(async () =>
                {
                    uint rc = 1;
                    string safeName = entry.Name.Replace("'", "\\'");
                    using var searcher = new ManagementObjectSearcher(
                        $"SELECT * FROM Win32_Service WHERE Name='{safeName}'");
                    using var results = searcher.Get();
                    foreach (ManagementObject obj in results)
                    {
                        using (obj)
                        {
                            if (entry.State == "Running") obj.InvokeMethod("StopService", null);
                            await Task.Delay(900);
                            var res = obj.InvokeMethod("StartService", null);
                            rc = res != null ? Convert.ToUInt32(res) : 0;
                        }
                    }
                    return rc;
                });

                if (returnCode == 0 || returnCode == 10)
                    TxtStatus.Text = $"✅ Reiniciado: '{entry.DisplayName}'";
                else
                    TxtStatus.Text = $"⚠ Falha ao reiniciar '{entry.DisplayName}' (código WMI {returnCode} — execute como Admin).";

                await LoadServicesAsync();
            }
            catch (Exception ex) { TxtStatus.Text = $"❌ Erro: {ex.Message}"; }
        }
    }
}
