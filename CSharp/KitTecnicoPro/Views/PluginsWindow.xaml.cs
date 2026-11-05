using System.Diagnostics;
using System.IO;
using System.Windows;
using KitTecnicoPro.ViewModels;

namespace KitTecnicoPro.Views
{
    public partial class PluginsWindow : Window
    {
        private readonly MainViewModel _vm;
        private readonly string _pluginsDir;

        public PluginsWindow(MainViewModel vm)
        {
            InitializeComponent();

            this.SourceInitialized += (s, e) => 
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch {}
            };
            _vm         = vm;
            _pluginsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            RefreshList();
        }

        private void RefreshList()
        {
            Directory.CreateDirectory(_pluginsDir);
            var scripts = Directory.GetFiles(_pluginsDir, "*.ps1")
                                   .Select(Path.GetFileName)
                                   .ToList();
            PluginList.ItemsSource = scripts;
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshList();

        private void OnOpenFolderClick(object sender, RoutedEventArgs e)
            => Process.Start("explorer.exe", _pluginsDir);

        private void OnRunClick(object sender, RoutedEventArgs e)
        {
            if (PluginList.SelectedItem is not string fileName) return;
            string fullPath = Path.Combine(_pluginsDir, fileName);

            if (MessageBox.Show(
                $"Executar o script '{fileName}'?\n\nEle será validado antes da execução.",
                "Confirmar Plugin", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _ = _vm.RunPluginAsync(fullPath);
            Close();
        }
    }
}

