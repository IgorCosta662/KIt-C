using System.Windows;
using KitTecnicoPro.Services;
using KitTecnicoPro.ViewModels;
using Microsoft.Win32;

namespace KitTecnicoPro.Views
{
    public partial class ImportExportWindow : Window
    {
        private readonly MainViewModel _vm;
        private readonly AppDataService _svc;

        public ImportExportWindow(MainViewModel vm, AppDataService? svc = null)
        {
            InitializeComponent();

            this.SourceInitialized += (_, _) =>
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch { }
            };
            _vm  = vm;
            _svc = svc ?? new AppDataService();
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                FileName   = "apps.json",
                DefaultExt = ".json",
                Filter     = "JSON files (*.json)|*.json"
            };
            if (dlg.ShowDialog() == true)
            {
                _svc.ExportApps(_vm.AllApps.ToList(), dlg.FileName);
                MessageBox.Show($"Exportado com sucesso!\n{dlg.FileName}", "Sucesso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnImportClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                DefaultExt = ".json",
                Filter     = "JSON files (*.json)|*.json"
            };
            if (dlg.ShowDialog() != true) return;

            if (MessageBox.Show(
                "Importar vai substituir a lista atual. Confirmar?",
                "Confirmar Importação", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            var apps = _svc.ImportApps(dlg.FileName);
            _svc.SaveApps(apps);
            _vm.LoadApps();
            MessageBox.Show($"{apps.Count} programas importados!", "Importação concluída",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
