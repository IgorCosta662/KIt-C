using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using KitTecnicoPro.Models;
using KitTecnicoPro.Serialization;
using KitTecnicoPro.Services;
using KitTecnicoPro.ViewModels;
using KitTecnicoPro.Views;

namespace KitTecnicoPro.Infrastructure
{
    /// <summary>
    /// Contêiner leve e thread-safe de Injeção de Dependência (<see cref="IServiceProvider"/>)
    /// com suporte a ciclos de vida Singleton e Transient, resolução automática de construtores
    /// e descarte determinístico em ordem reversa (LIFO) no encerramento da aplicação.
    /// </summary>
    public sealed class ServiceContainer : IServiceProvider, IDisposable, IAsyncDisposable
    {
        private enum ServiceLifetime { Singleton, Transient }

        private sealed record Registration(
            ServiceLifetime Lifetime,
            Func<ServiceContainer, object> Factory);

        private readonly ConcurrentDictionary<Type, Registration> _registrations = new();
        private readonly ConcurrentDictionary<Type, Lazy<object>> _singletons    = new();
        private readonly Stack<object> _disposables = new();
        private readonly object _disposeLock = new();
        private bool _disposed;

        public ServiceContainer AddSingleton<TService>(TService instance) where TService : class
        {
            ArgumentNullException.ThrowIfNull(instance);
            TrackDisposable(instance);
            _singletons[typeof(TService)] = new Lazy<object>(() => instance);
            _registrations[typeof(TService)] = new Registration(ServiceLifetime.Singleton, _ => instance);
            return this;
        }

        public ServiceContainer AddSingleton<TService>(Func<ServiceContainer, TService> factory) where TService : class
        {
            _registrations[typeof(TService)] = new Registration(ServiceLifetime.Singleton, sp => factory(sp));
            return this;
        }

        public ServiceContainer AddSingleton<TInterface, TImplementation>(Func<ServiceContainer, TImplementation> factory)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            AddSingleton(factory);
            _registrations[typeof(TInterface)] = new Registration(
                ServiceLifetime.Singleton,
                sp => sp.GetRequiredService<TImplementation>());
            return this;
        }

        public ServiceContainer AddTransient<TService>(Func<ServiceContainer, TService> factory) where TService : class
        {
            _registrations[typeof(TService)] = new Registration(ServiceLifetime.Transient, sp => factory(sp));
            return this;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceProvider) || serviceType == typeof(ServiceContainer))
                return this;

            if (!_registrations.TryGetValue(serviceType, out var reg))
                return null;

            if (reg.Lifetime == ServiceLifetime.Singleton)
            {
                var lazy = _singletons.GetOrAdd(serviceType, _ => new Lazy<object>(() =>
                {
                    object created = reg.Factory(this);
                    TrackDisposable(created);
                    return created;
                }, LazyThreadSafetyMode.ExecutionAndPublication));
                return lazy.Value;
            }

            return reg.Factory(this);
        }

        public T GetRequiredService<T>() where T : class
            => (T)(GetService(typeof(T)) ?? throw new InvalidOperationException($"Serviço não registrado no contêiner DI: {typeof(T).FullName}"));

        private void TrackDisposable(object instance)
        {
            if (instance is IDisposable or IAsyncDisposable && !ReferenceEquals(instance, this))
            {
                lock (_disposeLock)
                {
                    if (!_disposables.Contains(instance))
                        _disposables.Push(instance);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            List<object> toDispose;
            lock (_disposeLock)
            {
                if (_disposed) return;
                _disposed = true;
                toDispose = _disposables.ToList();
                _disposables.Clear();
            }

            foreach (var item in toDispose)
            {
                try
                {
                    if (item is IAsyncDisposable asyncDisp)
                        await asyncDisp.DisposeAsync().ConfigureAwait(false);
                    else if (item is IDisposable disp)
                        disp.Dispose();
                }
                catch { }
            }
        }

        public void Dispose()
        {
            List<object> toDispose;
            lock (_disposeLock)
            {
                if (_disposed) return;
                _disposed = true;
                toDispose = _disposables.ToList();
                _disposables.Clear();
            }

            foreach (var item in toDispose)
            {
                try { (item as IDisposable)?.Dispose(); } catch { }
            }
        }
    }

    /// <summary>
    /// Host central da aplicação: carrega configurações tipadas, configura o grafo de Injeção de Dependência
    /// e gerencia o ciclo de vida completo do Kit Técnico Pro.
    /// </summary>
    public static class AppHost
    {
        private static ServiceContainer? _current;

        public static ServiceContainer Services
            => _current ?? throw new InvalidOperationException("AppHost ainda não foi inicializado.");

        public static bool IsInitialized => _current != null;

        public static ServiceContainer Initialize()
        {
            if (_current != null) return _current;

            var settings  = LoadSettings();
            var container = new ServiceContainer();

            // 1. Configurações & Infraestrutura base
            container.AddSingleton(settings);
            container.AddSingleton<IAppLogger, LogService>(_ => new LogService(settings.LogRetentionDays));
            container.AddSingleton(sp => new ResilientHttpService(
                sp.GetRequiredService<IAppLogger>(),
                TimeSpan.FromMinutes(Math.Clamp(settings.HttpTimeoutMinutes, 5, 240))));

            // 2. Serviços de domínio
            container.AddSingleton(_ => new PowerShellService());
            container.AddSingleton(_ => new CacheService());
            container.AddSingleton(_ => new AppDataService());
            container.AddSingleton(_ => new ProfileService());
            container.AddSingleton(_ => new ReportService());
            container.AddSingleton(sp => new OnlineUpdateService(sp.GetRequiredService<ResilientHttpService>()));
            container.AddSingleton(sp => new OptimizationService(sp.GetRequiredService<PowerShellService>()));
            container.AddSingleton(sp => new InstallationService(
                sp.GetRequiredService<PowerShellService>(),
                sp.GetRequiredService<CacheService>(),
                sp.GetRequiredService<ResilientHttpService>(),
                sp.GetRequiredService<IAppLogger>()));

            // 3. ViewModels
            container.AddSingleton(sp => new MainViewModel(
                sp.GetRequiredService<AppDataService>(),
                sp.GetRequiredService<ProfileService>(),
                sp.GetRequiredService<InstallationService>(),
                sp.GetRequiredService<PowerShellService>(),
                sp.GetRequiredService<LogService>(),
                sp.GetRequiredService<CacheService>(),
                sp.GetRequiredService<OnlineUpdateService>(),
                sp.GetRequiredService<OptimizationService>(),
                sp.GetRequiredService<ReportService>(),
                sp.GetRequiredService<AppSettings>()));

            container.AddTransient(_ => new DashboardViewModel());

            // 4. Views principais e janelas filhas injetadas
            container.AddSingleton(sp => new MainWindow(sp.GetRequiredService<MainViewModel>(), sp));
            container.AddTransient(sp => new DashboardWindow(sp.GetRequiredService<DashboardViewModel>()));
            container.AddTransient(sp => new IsoManagerWindow(sp.GetRequiredService<ResilientHttpService>(), sp.GetRequiredService<IAppLogger>()));
            container.AddTransient(sp => new OptimizationWindow(sp.GetRequiredService<OptimizationService>()));
            container.AddTransient(sp => new ImportExportWindow(sp.GetRequiredService<MainViewModel>(), sp.GetRequiredService<AppDataService>()));
            container.AddTransient(sp => new PluginsWindow(sp.GetRequiredService<MainViewModel>()));

            _current = container;
            return container;
        }

        public static AppSettings LoadSettings()
        {
            try
            {
                string settingsPath = AppPaths.GetDataFile("settings.json");
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    var parsed = JsonSerializer.Deserialize(json, KitJsonContext.IndentedUtf8.AppSettings);
                    if (parsed != null) return parsed;
                }
            }
            catch { }
            return new AppSettings();
        }

        public static void Shutdown()
        {
            var current = Interlocked.Exchange(ref _current, null);
            current?.Dispose();
        }
    }
}
