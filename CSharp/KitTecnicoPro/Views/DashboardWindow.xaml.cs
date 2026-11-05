using System.Windows;
using KitTecnicoPro.ViewModels;

namespace KitTecnicoPro.Views
{
    public partial class DashboardWindow : Window
    {
        private readonly DashboardViewModel _vm;

        public DashboardWindow() : this(new DashboardViewModel()) { }

        public DashboardWindow(DashboardViewModel vm)
        {
            InitializeComponent();

            this.SourceInitialized += (_, _) =>
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch { }
            };
            _vm = vm;
            DataContext = _vm;
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
            => _ = _vm.RefreshAsync();

        private void Window_Closed(object sender, EventArgs e)
            => _vm.Dispose();
    }
}
