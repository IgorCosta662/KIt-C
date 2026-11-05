using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace KitTecnicoPro.Views
{
    public class StartupEntry
    {
        public string Name        { get; set; } = "";
        public string Command     { get; set; } = "";
        public string Source      { get; set; } = "";   // HKLM / HKCU / Startup
        public bool   Enabled     { get; set; } = true;
        public string OriginalKey { get; set; } = "";   // nome original no registro ou caminho do arquivo

        public string StatusIcon  => Enabled ? "🟢" : "🔴";
        public string SourceColor => Source == "HKLM" ? "#1565C0" : Source == "HKCU" ? "#4A235A" : "#1B5E20";
    }

    public partial class StartupManagerWindow : Window
    {
        private const string DisabledPrefix = "DISABLED_KTP_";
        private List<StartupEntry> _all  = new();
        private string             _busca = "";

        private static readonly string RunHklm = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private static readonly string RunHkcu = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public StartupManagerWindow()
        {
            InitializeComponent();

            this.SourceInitialized += (s, e) =>
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch { }
            };
            Load();
        }

        private void Load()
        {
            _all.Clear();
            ReadRegistryKey(Registry.LocalMachine, RunHklm, "HKLM");
            ReadRegistryKey(Registry.CurrentUser,  RunHkcu, "HKCU");
            ReadStartupFolder();
            ApplyFilter();
        }

        private void ReadRegistryKey(RegistryKey hive, string path, string source)
        {
            try
            {
                using var key = hive.OpenSubKey(path, writable: false);
                if (key == null) return;
                foreach (string name in key.GetValueNames())
                {
                    bool disabled = name.StartsWith(DisabledPrefix, StringComparison.OrdinalIgnoreCase);
                    string display = disabled ? name[DisabledPrefix.Length..] : name;
                    _all.Add(new StartupEntry
                    {
                        Name        = display,
                        Command     = key.GetValue(name)?.ToString() ?? "",
                        Source      = source,
                        Enabled     = !disabled,
                        OriginalKey = name
                    });
                }
            }
            catch { }
        }

        private void ReadStartupFolder()
        {
            try
            {
                string[] folders =
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
                };
                foreach (string folder in folders)
                {
                    if (!Directory.Exists(folder)) continue;
                    foreach (string file in Directory.GetFiles(folder, "*.lnk"))
                    {
                        _all.Add(new StartupEntry
                        {
                            Name        = Path.GetFileNameWithoutExtension(file),
                            Command     = file,
                            Source      = "Startup",
                            Enabled     = true,
                            OriginalKey = file
                        });
                    }
                    foreach (string file in Directory.GetFiles(folder, "*.lnk.disabled"))
                    {
                        string baseName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file));
                        _all.Add(new StartupEntry
                        {
                            Name        = baseName,
                            Command     = file,
                            Source      = "Startup",
                            Enabled     = false,
                            OriginalKey = file
                        });
                    }
                }
            }
            catch { }
        }

        private void ApplyFilter()
        {
            if (LstStartup == null) return;
            var view = string.IsNullOrEmpty(_busca)
                ? _all
                : _all.Where(e => e.Name.Contains(_busca, StringComparison.OrdinalIgnoreCase)
                               || e.Command.Contains(_busca, StringComparison.OrdinalIgnoreCase)).ToList();
            LstStartup.ItemsSource = view;
            TxtStatus.Text = $"{_all.Count} entradas de inicialização ({_all.Count(x => x.Enabled)} habilitadas)";
        }

        private void OnBuscaChanged(object s, TextChangedEventArgs e)
        {
            _busca = TxtBusca.Text.Trim();
            ApplyFilter();
        }

        private void OnRefreshClick(object s, RoutedEventArgs e) => Load();

        private void OnEnableClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not StartupEntry entry) return;
            if (entry.Enabled) { TxtStatus.Text = $"✅ '{entry.Name}' já está habilitado."; return; }

            try
            {
                if (entry.Source == "Startup")
                {
                    string target = entry.OriginalKey.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)
                        ? entry.OriginalKey[..^9]
                        : entry.OriginalKey;
                    if (File.Exists(entry.OriginalKey))
                        File.Move(entry.OriginalKey, target, overwrite: true);
                    TxtStatus.Text = $"✅ '{entry.Name}' habilitado.";
                    Load();
                    return;
                }

                var hive = entry.Source == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser;
                using var key = hive.OpenSubKey(RunHklm, writable: true);
                if (key == null) { TxtStatus.Text = "❌ Sem permissão para editar HKLM. Execute como Admin."; return; }
                string val = key.GetValue(entry.OriginalKey)?.ToString() ?? "";
                key.DeleteValue(entry.OriginalKey);
                key.SetValue(entry.Name, val);
                TxtStatus.Text = $"✅ '{entry.Name}' habilitado.";
                Load();
            }
            catch (Exception ex) { TxtStatus.Text = $"❌ {ex.Message}"; }
        }

        private void OnDisableClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not StartupEntry entry) return;
            if (!entry.Enabled) { TxtStatus.Text = $"🔴 '{entry.Name}' já está desabilitado."; return; }

            if (MessageBox.Show($"Desabilitar '{entry.Name}' na inicialização?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                if (entry.Source == "Startup")
                {
                    if (File.Exists(entry.OriginalKey))
                        File.Move(entry.OriginalKey, entry.OriginalKey + ".disabled", overwrite: true);
                    TxtStatus.Text = $"🔴 '{entry.Name}' desabilitado.";
                    Load();
                    return;
                }

                var hive = entry.Source == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser;
                using var key = hive.OpenSubKey(RunHklm, writable: true);
                if (key == null) { TxtStatus.Text = "❌ Sem permissão para editar HKLM. Execute como Admin."; return; }
                string val = key.GetValue(entry.OriginalKey)?.ToString() ?? "";
                key.DeleteValue(entry.OriginalKey);
                key.SetValue(DisabledPrefix + entry.Name, val);
                TxtStatus.Text = $"🔴 '{entry.Name}' desabilitado.";
                Load();
            }
            catch (Exception ex) { TxtStatus.Text = $"❌ {ex.Message}"; }
        }
    }
}
