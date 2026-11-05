using System.Windows;
using KitTecnicoPro.Models;
using KitTecnicoPro.Services;

namespace KitTecnicoPro.Views
{
    public partial class OptimizationWindow : Window
    {
        private readonly OptimizationService _svc;
        private readonly List<WindowsTweak>  _tweaks;
        private CancellationTokenSource?     _cts;

        public OptimizationWindow(OptimizationService service)
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
            _svc    = service;
            _tweaks = OptimizationService.GetAvailableTweaks();
            TweaksList.ItemsSource = _tweaks;
        }

        private void OnSelectAllClick(object sender, RoutedEventArgs e)
        {
            foreach (var t in _tweaks) t.IsSelected = true;
            TweaksList.Items.Refresh();
        }

        private void OnDeselectAllClick(object sender, RoutedEventArgs e)
        {
            foreach (var t in _tweaks) t.IsSelected = false;
            TweaksList.Items.Refresh();
        }

        private async void OnRunClick(object sender, RoutedEventArgs e)
        {
            var selected = _tweaks.Where(t => t.IsSelected).ToList();
            if (!selected.Any())
            {
                MessageBox.Show("Selecione ao menos um tweak.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                $"Aplicar {selected.Count} tweaks selecionados?\nAlguns podem requerer reinicialização.",
                "Confirmar Otimização", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            BtnRun.IsEnabled = false;
            LogOutput.Clear();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            AppendLog("━━━ Iniciando Otimização do Windows ━━━");

            try
            {
                await _svc.RunSelectedTweaksAsync(
                    selected,
                    (msg, lvl) => Dispatcher.BeginInvoke(() => AppendLog(msg)),
                    _cts.Token);

                AppendLog("━━━ Otimização concluída! ━━━");
            }
            catch (OperationCanceledException)
            {
                AppendLog("⛔ Otimização cancelada.");
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Erro: {ex.Message}");
            }
            finally
            {
                BtnRun.IsEnabled = true;
            }
        }

        private void AppendLog(string msg)
        {
            LogOutput.AppendText(msg + Environment.NewLine);
            LogScroll.ScrollToBottom();
        }
    }
}
