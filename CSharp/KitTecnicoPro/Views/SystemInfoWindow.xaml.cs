using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KitTecnicoPro.Models;
using KitTecnicoPro.Services;

namespace KitTecnicoPro.Views
{
    public partial class SystemInfoWindow : Window
    {
        private SystemInfo? _lastInfo;

        public SystemInfoWindow() {
            InitializeComponent();
            this.SourceInitialized += (s, e) => 
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch {}
            };

        
        }private void Window_Loaded(object sender, RoutedEventArgs e) => Load();

        private void OnRefreshClick(object sender, RoutedEventArgs e) => Load();

        private void Load()
        {
            _lastInfo = SystemService.GetSystemInfo();
            BuildUI(_lastInfo);
            TxtAtualizado.Text = $"Atualizado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
        }

        private void OnCopyClick(object sender, RoutedEventArgs e)
        {
            if (_lastInfo == null) return;
            var sb = new StringBuilder();
            sb.AppendLine("=== Kit Técnico Pro — Informações do Sistema ===");
            sb.AppendLine($"Data: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine($"OS:          {_lastInfo.OsName} {_lastInfo.OsVersion}");
            sb.AppendLine($"Máquina:     {_lastInfo.MachineName}");
            sb.AppendLine($"Usuário:     {_lastInfo.UserName}");
            sb.AppendLine($"Admin:       {(_lastInfo.IsAdmin ? "Sim" : "Não")}");
            sb.AppendLine($"Internet:    {(_lastInfo.HasInternet ? "Conectado" : "Sem conexão")}");
            sb.AppendLine($"IP Local:    {_lastInfo.LocalIp}");
            sb.AppendLine();
            sb.AppendLine($"CPU:         {_lastInfo.CpuName}");
            sb.AppendLine($"Núcleos:     {_lastInfo.CpuCores}");
            sb.AppendLine($"Uso CPU:     {_lastInfo.CpuUsagePercent}%");
            sb.AppendLine();
            sb.AppendLine($"RAM Total:   {_lastInfo.RamTotalGB} GB");
            sb.AppendLine($"RAM Usada:   {_lastInfo.RamUsedGB} GB ({_lastInfo.RamUsagePercent}%)");
            sb.AppendLine($"RAM Livre:   {_lastInfo.RamFreeGB} GB");
            sb.AppendLine();
            sb.AppendLine($"GPU:         {_lastInfo.GpuName}");
            if (_lastInfo.GpuVramGB > 0) sb.AppendLine($"VRAM:        {_lastInfo.GpuVramGB} GB");
            sb.AppendLine();
            foreach (var d in _lastInfo.Disks)
                sb.AppendLine($"Disco {d.Drive}   {d.TotalGB} GB total | {d.FreeGB} GB livre ({d.UsedPercent}%) {d.Label}");
            sb.AppendLine();
            if (_lastInfo.NetworkAdapters.Count > 0)
                sb.AppendLine($"Rede:        {string.Join(", ", _lastInfo.NetworkAdapters)}");

            Clipboard.SetText(sb.ToString());
            MessageBox.Show("✅ Informações copiadas para a área de transferência!",
                "Copiar", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BuildUI(SystemInfo info)
        {
            InfoPanel.Children.Clear();

            AddSection("💻 Sistema Operacional");
            AddRow("OS",        info.OsName);
            AddRow("Versão",    info.OsVersion);
            AddRow("Máquina",   info.MachineName);
            AddRow("Usuário",   info.UserName);
            AddRow("Admin",     info.IsAdmin ? "✅ Sim" : "❌ Não");
            AddRow("Internet",  info.HasInternet ? "✅ Conectado" : "❌ Sem conexão");
            AddRow("IP Local",  string.IsNullOrEmpty(info.LocalIp) ? "N/A" : info.LocalIp);

            AddSection("🔧 Processador");
            AddRow("CPU",       info.CpuName);
            AddRow("Núcleos",   info.CpuCores.ToString());
            AddRow("Uso atual", $"{info.CpuUsagePercent}%");
            AddProgressBar("Uso CPU", info.CpuUsagePercent);

            AddSection("🧠 Memória RAM");
            AddRow("Total",     $"{info.RamTotalGB} GB");
            AddRow("Usada",     $"{info.RamUsedGB} GB");
            AddRow("Livre",     $"{info.RamFreeGB} GB");
            AddProgressBar("Uso RAM", info.RamUsagePercent);

            AddSection("🎮 Placa de Vídeo");
            AddRow("GPU",   string.IsNullOrEmpty(info.GpuName) ? "N/A" : info.GpuName);
            if (info.GpuVramGB > 0)
                AddRow("VRAM", $"{info.GpuVramGB} GB");

            AddSection("🌐 Rede");
            AddRow("IP Local", string.IsNullOrEmpty(info.LocalIp) ? "N/A" : info.LocalIp);
            foreach (var adapter in info.NetworkAdapters)
                AddRow("Adaptador", adapter);

            AddSection("💾 Discos");
            foreach (var disk in info.Disks)
            {
                AddRow($"Drive {disk.Drive}", $"{disk.TotalGB} GB total  |  {disk.FreeGB} GB livre  |  {disk.Label}");
                AddProgressBar($"Uso {disk.Drive}", disk.UsedPercent);
            }
        }

        private void AddSection(string title)
        {
            InfoPanel.Children.Add(new TextBlock
            {
                Text       = title,
                FontSize   = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x90, 0xCA, 0xF9)),
                Margin     = new Thickness(0, 14, 0, 6)
            });
        }

        private void AddRow(string label, string value)
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock
            {
                Text       = label,
                Foreground = new SolidColorBrush(Color.FromRgb(0x78, 0x90, 0x9C)),
                FontSize   = 12
            };
            var val = new TextBlock
            {
                Text       = value,
                Foreground = new SolidColorBrush(Color.FromRgb(0xEC, 0xEF, 0xF1)),
                FontSize   = 12,
                FontWeight = FontWeights.SemiBold
            };

            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(val, 1);
            grid.Children.Add(lbl);
            grid.Children.Add(val);
            InfoPanel.Children.Add(grid);
        }

        private void AddProgressBar(string label, double percent)
        {
            Color barColor = percent > 85
                ? Color.FromRgb(0xEF, 0x9A, 0x9A)
                : percent > 60
                    ? Color.FromRgb(0xFF, 0xE0, 0x82)
                    : Color.FromRgb(0xA5, 0xD6, 0xA7);

            var bar = new ProgressBar
            {
                Value      = percent,
                Maximum    = 100,
                Height     = 8,
                Margin     = new Thickness(0, 2, 0, 6),
                Foreground = new SolidColorBrush(barColor),
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x2A, 0x3A))
            };
            InfoPanel.Children.Add(bar);
        }
    }
}


