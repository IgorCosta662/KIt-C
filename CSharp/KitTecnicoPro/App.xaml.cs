using System.IO;
using System.Windows;
using KitTecnicoPro.Infrastructure;
using KitTecnicoPro.Services;
using KitTecnicoPro.Views;

namespace KitTecnicoPro
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = AppHost.Initialize();
            var logger   = services.GetRequiredService<IAppLogger>();

            DispatcherUnhandledException += (_, args) =>
            {
                try
                {
                    logger.Error("Exceção não tratada na thread de UI (Dispatcher)", args.Exception, "Crash");
                    Directory.CreateDirectory(AppPaths.LogsDir);
                    string crashPath = Path.Combine(AppPaths.LogsDir, "crash.txt");
                    File.WriteAllText(crashPath, $"{args.Exception}\n\nINNER:\n{args.Exception.InnerException}");
                    MessageBox.Show($"Ocorreu um erro inesperado registrado em:\n{crashPath}\n\n{args.Exception.Message}",
                        "Kit Técnico Pro — Erro Capturado", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { }
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                try
                {
                    var ex = args.ExceptionObject as Exception;
                    logger.Error("Exceção fatal de AppDomain", ex, "Crash");
                    Directory.CreateDirectory(AppPaths.LogsDir);
                    string crashPath = Path.Combine(AppPaths.LogsDir, "crash_domain.txt");
                    File.WriteAllText(crashPath, args.ExceptionObject?.ToString() ?? "Unknown fatal exception");
                }
                catch { }
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                logger.Error("Task assíncrona com exceção não observada", args.Exception, "Async");
                args.SetObserved();
            };

            logger.Info("Host inicializado via Injeção de Dependência (.NET 8).", "Bootstrap");

            var mainWindow = services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                AppHost.Shutdown();
            }
            catch { }
            base.OnExit(e);
        }
    }
}
