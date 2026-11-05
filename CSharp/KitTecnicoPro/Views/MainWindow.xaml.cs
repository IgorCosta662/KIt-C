using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using KitTecnicoPro.Infrastructure;
using KitTecnicoPro.Models;
using KitTecnicoPro.ViewModels;

namespace KitTecnicoPro.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;
        private readonly ServiceContainer? _services;

        public MainWindow() : this(
            AppHost.IsInitialized ? AppHost.Services.GetRequiredService<MainViewModel>() : new MainViewModel(),
            AppHost.IsInitialized ? AppHost.Services : null)
        {
        }

        public MainWindow(MainViewModel vm, ServiceContainer? services = null)
        {
            InitializeComponent();
            _vm       = vm;
            _services = services;
            DataContext = _vm;

            _vm.Logs.CollectionChanged += (_, _) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    LogScrollViewer.ScrollToBottom();
                });
            };

            this.SourceInitialized += (_, _) =>
            {
                KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this);
            };

            this.Closed += (_, _) =>
            {
                if (_services == null)
                    _vm.Dispose();
            };
        }

        private void OpenOrActivateWindow<T>(Func<T> factory) where T : Window
        {
            try
            {
                var existing = Application.Current.Windows.OfType<T>().FirstOrDefault();
                if (existing != null)
                {
                    if (existing.WindowState == WindowState.Minimized)
                        existing.WindowState = WindowState.Normal;
                    existing.Activate();
                    return;
                }

                var win = factory();
                win.Owner = this;
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir janela:\n{ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnInstallProfileClick(object sender, RoutedEventArgs e)
        {
            var win = new ProfileSelectorWindow(_vm.Profiles.ToList()) { Owner = this };
            if (win.ShowDialog() == true && win.SelectedProfile != null)
                _ = _vm.InstallProfileAsync(win.SelectedProfile);
        }

        private void OnAddAppClick(object sender, RoutedEventArgs e)
        {
            var win = new AddAppWindow { Owner = this };
            if (win.ShowDialog() == true && win.CreatedApp != null)
                _vm.AddApp(win.CreatedApp);
        }

        private void OnEditAppClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not AppItem app) return;
            var win = new AddAppWindow(app) { Owner = this };
            if (win.ShowDialog() == true)
            {
                _vm.SaveApps();
                _vm.ApplyFilter();
            }
        }

        private void OnRemoveAppClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not AppItem app) return;
            if (MessageBox.Show($"Remover '{app.Nome}'?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                _vm.RemoveApp(app);
        }

        private void OnManageProfilesClick(object sender, RoutedEventArgs e)
        {
            var win = new ProfileWindow(_vm.Profiles.ToList(), _vm.AllApps.ToList()) { Owner = this };
            win.ShowDialog();
            _vm.Profiles.Clear();
            foreach (var p in win.UpdatedProfiles)
                _vm.Profiles.Add(p);
            _vm.SaveProfiles();
        }

        private void OnSetLocalPathClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not AppItem app) return;

            string catFolder = AppPaths.SetupsCatDir(app.Categoria);
            Directory.CreateDirectory(catFolder);

            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title            = $"Selecionar setup para: {app.Nome}",
                Filter           = "Instaladores|*.exe;*.msi;*.msp|Todos os arquivos|*.*",
                InitialDirectory = catFolder,
                CheckFileExists  = true
            };

            if (dlg.ShowDialog() != true) return;

            app.LocalPath   = dlg.FileName;
            app.Tipo        = "local";
            app.SilentArgs ??= "/S";
            app.UpdateLocalPathState();
            _vm.SaveApps();

            MessageBox.Show(
                $"✅ Setup vinculado!\n\n{dlg.FileName}\n\nO tipo foi alterado para 'local'. Clique em ▶ para instalar.",
                app.Nome, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void OnQuickInstallClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not AppItem app) return;
            await _vm.QuickInstallAsync(app);
        }

        private async void OnDownloadOfflineClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not AppItem app) return;
            await _vm.DownloadOfflineAsync(app);
        }

        private async void OnOpenV1Click(object sender, RoutedEventArgs e)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] relLevels = { ".", "..", @"..\..", @"..\..\..", @"..\..\..\..", @"..\..\..\..\.." };
            string[] exeRelPaths =
            {
                @"1.0\bin\Release\net8.0-windows\win-x64\TechKitPro.exe",
                @"1.0\bin\Release\net8.0-windows\TechKitPro.exe",
                @"1.0\bin\Debug\net8.0-windows\win-x64\TechKitPro.exe",
                @"1.0\bin\Debug\net8.0-windows\TechKitPro.exe",
            };

            var roots = new List<string> { AppPaths.RootDir };
            foreach (var rel in relLevels)
            {
                try { roots.Add(Path.GetFullPath(Path.Combine(baseDir, rel))); } catch { }
            }

            string? exe = null;
            string? csproj = null;

            foreach (var root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                foreach (var relExe in exeRelPaths)
                {
                    string candidate = Path.Combine(root, relExe);
                    if (File.Exists(candidate))
                    {
                        exe = candidate;
                        break;
                    }
                }
                if (exe != null) break;

                string candidateProj = Path.Combine(root, @"1.0\TechKitPro.csproj");
                if (csproj == null && File.Exists(candidateProj))
                    csproj = candidateProj;
            }

            if (exe != null)
            {
                try { Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true }); }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
            else if (csproj != null)
            {
                if (MessageBox.Show(
                    $"Executável do TechKit Pro v1.0 ainda não compilado, mas o projeto foi encontrado em:\n{csproj}\n\nDeseja compilar e abrir agora?",
                    "TechKit Pro v1.0", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        _vm.AddLog("⏳ Compilando e iniciando TechKit Pro v1.0...", LogLevel.Info);
                        await Task.Run(() =>
                        {
                            Process.Start(new ProcessStartInfo("dotnet", $"run --project \"{csproj}\"")
                            {
                                UseShellExecute = false,
                                CreateNoWindow  = true
                            });
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Erro ao iniciar v1.0", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("TechKit Pro v1.0 não encontrado.\nVerifique se a pasta '1.0' está presente na raiz do projeto.",
                    "v1.0 não encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnIsoManagerClick(object sender, RoutedEventArgs e)
            => OpenOrActivateWindow(() => _services?.GetRequiredService<IsoManagerWindow>() ?? new IsoManagerWindow());

        private void OnServiceManagerClick(object sender, RoutedEventArgs e)
            => OpenOrActivateWindow(() => new ServiceManagerWindow());

        private void OnEventLogClick(object sender, RoutedEventArgs e)
            => OpenOrActivateWindow(() => new EventLogViewerWindow());

        private void OnFileHasherClick(object sender, RoutedEventArgs e)
            => OpenOrActivateWindow(() => new FileHasherWindow());

        private void OnCredentialVaultClick(object sender, RoutedEventArgs e)
            => OpenOrActivateWindow(() => new CredentialVaultWindow());

        private void OnProcessManagerClick(object sender, RoutedEventArgs e)
            => OpenOrActivateWindow(() => new ProcessManagerWindow());

        private void OnNetworkScannerClick(object sender, RoutedEventArgs e)
            => OpenOrActivateWindow(() => new NetworkScannerWindow());

        private void OnStartupManagerClick(object sender, RoutedEventArgs e)
            => OpenOrActivateWindow(() => new StartupManagerWindow());

        private void OnQuickToolsClick(object sender, RoutedEventArgs e)
            => OpenOrActivateWindow(() => new QuickToolsWindow());

        private void OnScriptsClick(object sender, RoutedEventArgs e)
            => OpenOrActivateWindow(() => new ScriptsWindow());

        private void OnToolsClick(object sender, RoutedEventArgs e)
            => OpenOrActivateWindow(() => new ToolsWindow());

        private void OnDashboardClick(object sender, RoutedEventArgs e)
            => OpenOrActivateWindow(() => _services?.GetRequiredService<DashboardWindow>() ?? new DashboardWindow());

        private void OnOptimizeClick(object sender, RoutedEventArgs e)
        {
            var win = _services?.GetRequiredService<OptimizationWindow>() ?? new OptimizationWindow(_vm.GetOptimizationService());
            win.Owner = this;
            win.ShowDialog();
        }

        private async void OnDriverBackupClick(object sender, RoutedEventArgs e)
        {
            if (_vm.IsBusy) return;

            var dlg = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Selecione a pasta onde deseja salvar o Backup dos Drivers (ex: Pen Drive)"
            };

            if (dlg.ShowDialog() == true)
            {
                string dest = dlg.FolderName;
                string safeDest = Services.PowerShellService.EscapeSingleQuotes(dest);
                _vm.AddLog($"💾 Iniciando Backup de Drivers para: {dest} ...", LogLevel.Info);

                var btn = sender as Button;
                if (btn != null) btn.IsEnabled = false;

                try
                {
                    string cmd = $"Export-WindowsDriver -Online -Destination '{safeDest}'";
                    bool ok = await _vm.GetPowerShellService().ExecuteAsync(cmd, _vm.AddLog);

                    if (ok)
                    {
                        _vm.AddLog($"✅ Backup de drivers concluído em: {dest}", LogLevel.Success);
                        MessageBox.Show("Backup de drivers concluído com sucesso!", "Drivers Exportados", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        _vm.AddLog("❌ Falha no backup de drivers. Verifique se o programa está rodando como Administrador.", LogLevel.Error);
                        MessageBox.Show("Houve um erro no backup dos drivers. Verifique os logs e se o programa tem privilégios de Admin.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    _vm.AddLog($"❌ Erro no backup de drivers: {ex.Message}", LogLevel.Error);
                    MessageBox.Show(ex.Message, "Erro no Backup", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (btn != null) btn.IsEnabled = true;
                }
            }
        }

        private void OnSystemInfoClick(object sender, RoutedEventArgs e)
            => OpenOrActivateWindow(() => new SystemInfoWindow());

        private void OnPluginsClick(object sender, RoutedEventArgs e)
        {
            var win = _services?.GetRequiredService<PluginsWindow>() ?? new PluginsWindow(_vm);
            win.Owner = this;
            win.ShowDialog();
        }

        private void OnImportExportClick(object sender, RoutedEventArgs e)
        {
            var win = _services?.GetRequiredService<ImportExportWindow>() ?? new ImportExportWindow(_vm);
            win.Owner = this;
            win.ShowDialog();
        }

        private void OnOpenLogFileClick(object sender, RoutedEventArgs e)
        {
            string logDir = AppPaths.LogsDir;
            Directory.CreateDirectory(logDir);
            Process.Start("explorer.exe", logDir);
        }

        private void OnClearLogClick(object sender, RoutedEventArgs e)
            => _vm.Logs.Clear();

        private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void OnMaximizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    }
}
