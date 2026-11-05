using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using KitTecnicoPro.Helpers;
using KitTecnicoPro.Infrastructure;
using KitTecnicoPro.Models;
using KitTecnicoPro.Services;

namespace KitTecnicoPro.ViewModels
{
    /// <summary>ViewModel principal — orquestra a UI com Injeção de Dependência, AsyncRelayCommand e Logging Estruturado.</summary>
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private const string AllCategories = "Todas";
        private const int    MaxLogEntries = 1000;

        // ── Serviços injetados via DI ───────────────────────────────────
        private readonly AppDataService       _appData;
        private readonly ProfileService       _profileSvc;
        private readonly InstallationService  _installer;
        private readonly PowerShellService    _ps;
        private readonly LogService           _logSvc;
        private readonly CacheService         _cache;
        private readonly OnlineUpdateService  _onlineUpdate;
        private readonly OptimizationService  _optimization;
        private readonly ReportService        _report;

        // ── Estado de instalação (para relatório) ─────────────────────────
        private List<InstallationResult> _lastResults = new();
        private DateTime _batchStartTime;

        // ── Coleções expostas à UI ──────────────────────────────────────
        public ObservableCollection<AppItem>      AllApps       { get; } = new();
        public BulkObservableCollection<AppItem>  FilteredApps  { get; } = new();
        public ObservableCollection<LogEntry>     Logs          { get; } = new();
        public ObservableCollection<Profile>      Profiles      { get; } = new();
        public ObservableCollection<string>       Categories    { get; } = new();

        // ── Estado ─────────────────────────────────────────────────────
        private string _searchText        = string.Empty;
        private string _selectedCategory  = AllCategories;
        private bool   _isBusy;
        private int    _progress;
        private string _statusText        = "Pronto";
        private bool   _hasInternet;
        private bool   _isAdmin;
        private bool   _isMenuExpanded    = true;
        private int    _selectedCount;

        private CancellationTokenSource? _cts;
        private bool   _parallelMode;
        private int    _maxParallel  = 3;
        private string _onlineAppsUrl = string.Empty;
        private double _cacheSizeMB;

        private readonly DispatcherTimer _searchDebounce;

        // ── Propriedades ────────────────────────────────────────────────
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value ?? string.Empty;
                OnPropertyChanged();
                _searchDebounce.Stop();
                if (_searchText.Length == 0) ApplyFilter();
                else _searchDebounce.Start();
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (value is null || _selectedCategory == value) return;
                _selectedCategory = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public bool IsMenuExpanded
        {
            get => _isMenuExpanded;
            set { _isMenuExpanded = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsIdle));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsIdle => !_isBusy;

        public int Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public bool HasInternet
        {
            get => _hasInternet;
            set { _hasInternet = value; OnPropertyChanged(); }
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set { _isAdmin = value; OnPropertyChanged(); }
        }

        public bool ParallelMode
        {
            get => _parallelMode;
            set { _parallelMode = value; OnPropertyChanged(); }
        }

        public int MaxParallel
        {
            get => _maxParallel;
            set { _maxParallel = Math.Clamp(value, 1, 6); OnPropertyChanged(); }
        }

        public string OnlineAppsUrl
        {
            get => _onlineAppsUrl;
            set { _onlineAppsUrl = value; OnPropertyChanged(); }
        }

        public double CacheSizeMB
        {
            get => _cacheSizeMB;
            set { _cacheSizeMB = value; OnPropertyChanged(); }
        }

        public int SelectedCount => _selectedCount;

        // ── Comandos MVVM (AsyncRelayCommand + RelayCommand) ─────────────
        public ICommand InstallSelectedCommand    { get; }
        public ICommand InstallAllCommand         { get; }
        public ICommand UpdateAllCommand          { get; }
        public ICommand CleanSystemCommand        { get; }
        public ICommand CancelCommand             { get; }
        public ICommand SelectAllCommand          { get; }
        public ICommand DeselectAllCommand        { get; }
        public ICommand CheckInstalledCommand     { get; }
        public ICommand UpdateListOnlineCommand   { get; }
        public ICommand ClearCacheCommand         { get; }
        public ICommand GenerateReportTxtCommand  { get; }
        public ICommand GenerateReportHtmlCommand { get; }
        public ICommand ClearSearchCommand        { get; }
        public ICommand ToggleMenuCommand         { get; }

        /// <summary>Construtor para design-time XAML ou inicialização standalone.</summary>
        public MainViewModel()
            : this(
                new AppDataService(),
                new ProfileService(),
                new InstallationService(new PowerShellService(), new CacheService()),
                new PowerShellService(),
                new LogService(),
                new CacheService(),
                new OnlineUpdateService(),
                new OptimizationService(new PowerShellService()),
                new ReportService(),
                AppHost.LoadSettings())
        {
        }

        /// <summary>Construtor principal resolvido pelo contêiner de Injeção de Dependência (<see cref="AppHost"/>).</summary>
        public MainViewModel(
            AppDataService appData,
            ProfileService profileSvc,
            InstallationService installer,
            PowerShellService ps,
            LogService logSvc,
            CacheService cache,
            OnlineUpdateService onlineUpdate,
            OptimizationService optimization,
            ReportService report,
            AppSettings settings)
        {
            _appData      = appData;
            _profileSvc   = profileSvc;
            _installer    = installer;
            _ps           = ps;
            _logSvc       = logSvc;
            _cache        = cache;
            _onlineUpdate = onlineUpdate;
            _optimization = optimization;
            _report       = report;

            OnlineAppsUrl = settings.OnlineAppsJsonUrl;
            MaxParallel   = settings.MaxParallelInstalls;
            ParallelMode  = settings.ParallelModeDefault;

            _logSvc.EntryLogged += AppendEntryToUi;

            _searchDebounce = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(220) };
            _searchDebounce.Tick += (_, _) => { _searchDebounce.Stop(); ApplyFilter(); };

            void HandleCommandError(Exception ex) => AddLog($"❌ Erro inesperado: {ex.Message}", LogLevel.Error);

            InstallSelectedCommand    = new AsyncRelayCommand(InstallSelectedAsync,    () => IsIdle && SelectedCount > 0, HandleCommandError);
            InstallAllCommand         = new AsyncRelayCommand(InstallAllAsync,         () => IsIdle, HandleCommandError);
            UpdateAllCommand          = new AsyncRelayCommand(UpdateAllAsync,          () => IsIdle, HandleCommandError);
            CleanSystemCommand        = new AsyncRelayCommand(CleanSystemAsync,        () => IsIdle, HandleCommandError);
            CancelCommand             = new RelayCommand(Cancel,                       () => IsBusy);
            SelectAllCommand          = new RelayCommand(SelectAll);
            DeselectAllCommand        = new RelayCommand(DeselectAll);
            CheckInstalledCommand     = new AsyncRelayCommand(CheckInstalledAsync,     () => IsIdle, HandleCommandError);
            UpdateListOnlineCommand   = new AsyncRelayCommand(UpdateListOnlineAsync,   () => IsIdle, HandleCommandError);
            ClearCacheCommand         = new RelayCommand(ClearCache,                   () => IsIdle);
            GenerateReportTxtCommand  = new RelayCommand(() => GenerateReport("txt"),  () => _lastResults.Count > 0);
            GenerateReportHtmlCommand = new RelayCommand(() => GenerateReport("html"), () => _lastResults.Count > 0);
            ClearSearchCommand        = new RelayCommand(() => { SearchText = string.Empty; SelectedCategory = AllCategories; });
            ToggleMenuCommand         = new RelayCommand(() => IsMenuExpanded = !IsMenuExpanded);

            Initialize();
        }

        public PowerShellService GetPowerShellService() => _ps;

        private void Initialize()
        {
            IsAdmin = SystemService.IsAdministrator();
            LoadApps();
            LoadProfiles();

            AddLog($"Kit Técnico Pro v2.0 iniciado — {AllApps.Count} programas em {Math.Max(0, Categories.Count - 1)} categorias.", LogLevel.Success);
            _ = InitializeBackgroundAsync();
        }

        private async Task InitializeBackgroundAsync()
        {
            try
            {
                var internetTask = SystemService.HasInternetConnectionAsync();
                var cacheTask    = Task.Run(() => _cache.GetCacheSizeMB());
                await Task.WhenAll(internetTask, cacheTask);

                HasInternet = internetTask.Result;
                CacheSizeMB = cacheTask.Result;

                AddLog($"Admin: {(IsAdmin ? "Sim" : "Não")}  |  Internet: {(HasInternet ? "Conectado" : "Sem conexão")}  |  Cache: {CacheSizeMB:F1} MB", LogLevel.Info);
                if (!IsAdmin)
                    AddLog("Execute como Administrador para instalar programas sem interrupções.", LogLevel.Warning);
            }
            catch (Exception ex)
            {
                AddLog($"Falha nas verificações iniciais: {ex.Message}", LogLevel.Warning);
            }
        }

        // ── Carregamento de dados ────────────────────────────────────────
        public void LoadApps()
        {
            foreach (var oldApp in AllApps)
                UnhookApp(oldApp);

            var apps = _appData.LoadApps();
            AllApps.Clear();
            _selectedCount = 0;
            foreach (var app in apps)
            {
                HookApp(app);
                AllApps.Add(app);
            }
            OnPropertyChanged(nameof(SelectedCount));
            BuildCategoryList();
            ApplyFilter();
        }

        private void HookApp(AppItem app)
        {
            app.PropertyChanged -= OnAppPropertyChanged;
            if (app.IsSelected) _selectedCount++;
            app.PropertyChanged += OnAppPropertyChanged;
        }

        private void UnhookApp(AppItem app)
        {
            app.PropertyChanged -= OnAppPropertyChanged;
        }

        private void OnAppPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(AppItem.IsSelected) || sender is not AppItem app) return;
            _selectedCount = Math.Max(0, _selectedCount + (app.IsSelected ? 1 : -1));
            OnPropertyChanged(nameof(SelectedCount));
            CommandManager.InvalidateRequerySuggested();
        }

        public void LoadProfiles()
        {
            Profiles.Clear();
            foreach (var p in _profileSvc.LoadProfiles())
                Profiles.Add(p);
        }

        private void BuildCategoryList()
        {
            string keep = _selectedCategory;
            var cats = AllApps.Select(a => a.Categoria)
                              .Where(c => !string.IsNullOrWhiteSpace(c))
                              .Distinct(StringComparer.OrdinalIgnoreCase)
                              .OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase)
                              .ToList();

            Categories.Clear();
            Categories.Add(AllCategories);
            foreach (var cat in cats) Categories.Add(cat);

            _selectedCategory = Categories.Contains(keep) ? keep : AllCategories;
            OnPropertyChanged(nameof(SelectedCategory));
        }

        // ── Filtro ───────────────────────────────────────────────────────
        public void ApplyFilter()
        {
            string term    = SearchText.Trim();
            bool   allCats = string.IsNullOrEmpty(_selectedCategory) || _selectedCategory == AllCategories;

            IEnumerable<AppItem> filtered = AllApps;

            if (!allCats)
                filtered = filtered.Where(a => string.Equals(a.Categoria, _selectedCategory, StringComparison.OrdinalIgnoreCase));

            if (term.Length > 0)
                filtered = filtered.Where(a =>
                    (a.Nome?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.Descricao?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.Categoria?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.Id?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));

            FilteredApps.ReplaceAll(filtered.ToList());
        }

        // ── Ações de instalação ──────────────────────────────────────────
        private async Task InstallSelectedAsync()
        {
            var toInstall = AllApps.Where(a => a.IsSelected).ToList();
            if (ParallelMode)
                await RunInstallationBatchParallelAsync(toInstall, "Instalando selecionados");
            else
                await RunInstallationBatch(toInstall, "Instalando selecionados");
        }

        private async Task InstallAllAsync()
        {
            if (MessageBox.Show(
                $"Confirma a instalação de TODOS os {AllApps.Count} programas?",
                "Instalar Tudo", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            if (ParallelMode)
                await RunInstallationBatchParallelAsync(AllApps.ToList(), "Instalando todos");
            else
                await RunInstallationBatch(AllApps.ToList(), "Instalando todos");
        }

        public async Task InstallProfileAsync(Profile profile)
        {
            var toInstall = AllApps.Where(a => profile.Apps.Contains(a.Id)).ToList();
            if (toInstall.Count == 0)
            {
                AddLog($"⚠️ Perfil '{profile.Nome}' não possui programas válidos.", LogLevel.Warning);
                return;
            }
            if (ParallelMode)
                await RunInstallationBatchParallelAsync(toInstall, $"Perfil: {profile.Nome}");
            else
                await RunInstallationBatch(toInstall, $"Perfil: {profile.Nome}");
        }

        private async Task RunInstallationBatch(List<AppItem> apps, string label)
        {
            if (apps.Count == 0) return;
            SetBusy(true, $"Preparando: {label}...");
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _batchStartTime = DateTime.Now;

            try
            {
                AddLog($"━━━ {label} ({apps.Count} programas) ━━━", LogLevel.Info);

                await _installer.CreateRestorePointAsync(AddLog, _cts.Token);

                int done = 0;
                var results = new List<InstallationResult>();

                foreach (var app in apps)
                {
                    if (_cts.Token.IsCancellationRequested) break;

                    StatusText = $"Instalando {app.Nome} ({done + 1}/{apps.Count})...";
                    app.Status = AppStatus.Installing;

                    var result = await _installer.InstallAppAsync(app, AddLog, _cts.Token);
                    results.Add(result);

                    app.IsInstalled = result.Success;
                    app.Status      = result.Success ? "✅ Instalado" : "❌ Falhou";
                    done++;
                    Progress = (int)(done * 100.0 / apps.Count);
                }

                int ok   = results.Count(r => r.Success);
                int fail = results.Count(r => !r.Success);

                AddLog($"━━━ Concluído: {ok} instalados, {fail} com falha ━━━", ok == apps.Count ? LogLevel.Success : LogLevel.Warning);
                _lastResults = results;
                CacheSizeMB  = _cache.GetCacheSizeMB();
                CommandManager.InvalidateRequerySuggested();
                _appData.SaveApps(AllApps.ToList());
            }
            catch (OperationCanceledException)
            {
                AddLog("⛔ Instalação em lote cancelada.", LogLevel.Warning);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Erro na instalação em lote: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                SetBusy(false, "Pronto");
            }
        }

        private async Task RunInstallationBatchParallelAsync(List<AppItem> apps, string label)
        {
            if (apps.Count == 0) return;
            SetBusy(true, $"[⚡ Paralelo] {label}...");
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _batchStartTime = DateTime.Now;

            try
            {
                AddLog($"━━━ [PARALELO x{MaxParallel}] {label} ({apps.Count} programas) ━━━", LogLevel.Info);
                await _installer.CreateRestorePointAsync(AddLog, _cts.Token);

                foreach (var app in apps) app.Status = "⏳ Aguardando...";

                var results = await _installer.InstallParallelAsync(
                    apps,
                    MaxParallel,
                    AddLog,
                    (_, pct) =>
                    {
                        Application.Current?.Dispatcher.BeginInvoke(() =>
                        {
                            Progress   = pct;
                            StatusText = $"[⚡ Paralelo] {pct}% concluído...";
                        });
                    },
                    _cts.Token);

                foreach (var app in apps)
                {
                    var r = results.FirstOrDefault(x => x.AppName == app.Nome);
                    if (r != null)
                    {
                        app.IsInstalled = r.Success;
                        app.Status      = r.Success ? "✅ Instalado" : "❌ Falhou";
                    }
                }

                int ok = results.Count(r => r.Success);
                AddLog($"━━━ [PARALELO] Concluído: {ok}/{apps.Count} instalados ━━━",
                    ok == apps.Count ? LogLevel.Success : LogLevel.Warning);

                _lastResults = results;
                CacheSizeMB  = _cache.GetCacheSizeMB();
                CommandManager.InvalidateRequerySuggested();
                _appData.SaveApps(AllApps.ToList());
            }
            catch (OperationCanceledException)
            {
                AddLog("⛔ Instalação paralela cancelada.", LogLevel.Warning);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Erro na instalação paralela: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                SetBusy(false, "Pronto");
            }
        }

        private async Task UpdateAllAsync()
        {
            SetBusy(true, "Atualizando programas...");
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            try
            {
                AddLog("━━━ Atualizando todos os programas ━━━", LogLevel.Info);
                await _installer.UpgradeAllAsync(AddLog, _cts.Token);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Erro ao atualizar programas: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                SetBusy(false, "Pronto");
            }
        }

        private async Task CleanSystemAsync()
        {
            if (MessageBox.Show(
                "Confirma a limpeza de arquivos temporários?",
                "Limpeza do Sistema", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SetBusy(true, "Limpando sistema...");
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            try
            {
                await SystemService.CleanSystemAsync(AddLog, _cts.Token);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Erro na limpeza do sistema: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                SetBusy(false, "Pronto");
            }
        }

        private async Task CheckInstalledAsync()
        {
            SetBusy(true, "Verificando programas instalados...");
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            try
            {
                AddLog("🔍 Verificando programas instalados (consulta única ao winget)...", LogLevel.Info);

                var isInstalled = await _installer.GetInstalledCheckerAsync(_cts.Token);
                foreach (var app in AllApps)
                {
                    if (_cts.Token.IsCancellationRequested) break;
                    bool found = isInstalled(app.Id);
                    app.IsInstalled = found;
                    if (found) app.Status = "✅ Instalado";
                }

                int installed = AllApps.Count(a => a.IsInstalled);
                AddLog($"✅ Verificação concluída: {installed}/{AllApps.Count} instalados", LogLevel.Success);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Erro ao verificar programas instalados: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                SetBusy(false, "Pronto");
            }
        }

        // ── Seleção ──────────────────────────────────────────────────────
        private void SelectAll()   { foreach (var a in FilteredApps) a.IsSelected = true; }
        private void DeselectAll() { foreach (var a in FilteredApps) a.IsSelected = false; }

        public async Task DownloadOfflineAsync(AppItem app)
        {
            if (string.IsNullOrWhiteSpace(app.Id) && string.IsNullOrWhiteSpace(app.Url)) return;

            SetBusy(true, $"Baixando {app.Nome}...");
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            var dlProgress = new Progress<DownloadProgress>(p =>
            {
                if (p.Percentage >= 0)
                {
                    Progress   = p.Percentage;
                    StatusText = $"⬇ Baixando {app.Nome}: {p.Percentage}% ({p.SpeedMBps:F1} MB/s)";
                    app.Status = $"⬇ {p.Percentage}%";
                }
                else
                {
                    StatusText = $"⬇ Baixando {app.Nome}: {p.BytesRead / 1_048_576.0:F1} MB ({p.SpeedMBps:F1} MB/s)";
                }
            });

            try
            {
                AddLog($"⬇️ Iniciando download offline: {app.Nome}", LogLevel.Info);
                bool success = await _installer.DownloadOfflineInstallerAsync(app, AddLog, _cts.Token, dlProgress);

                if (success)
                {
                    AddLog($"✅ {app.Nome} baixado com sucesso!", LogLevel.Success);
                    app.Status = "Download OK";
                    SaveApps();
                }
                else
                {
                    AddLog($"❌ Falha ao baixar {app.Nome}.", LogLevel.Error);
                }
            }
            catch (OperationCanceledException)
            {
                AddLog($"⛔ Download offline de {app.Nome} cancelado.", LogLevel.Warning);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Erro no download: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                SetBusy(false, "Pronto");
            }
        }

        private async Task UpdateListOnlineAsync()
        {
            if (string.IsNullOrWhiteSpace(OnlineAppsUrl))
            {
                AddLog("⚠️ Configure a URL remota em settings.json (onlineAppsJsonUrl).", LogLevel.Warning);
                return;
            }

            SetBusy(true, "Atualizando lista online...");
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            try
            {
                var remoteApps = await _onlineUpdate.FetchAppsJsonAsync(OnlineAppsUrl, AddLog, _cts.Token);

                if (remoteApps != null)
                {
                    _appData.SaveApps(remoteApps);
                    LoadApps();
                    AddLog($"✅ Lista atualizada: {remoteApps.Count} programas carregados.", LogLevel.Success);
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Erro ao atualizar lista online: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                SetBusy(false, "Pronto");
            }
        }

        private void ClearCache()
        {
            if (MessageBox.Show("Limpar todos os arquivos em cache?",
                "Limpar Cache", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _cache.ClearCache();
            CacheSizeMB = 0;
            AddLog("🗑️ Cache limpo com sucesso.", LogLevel.Success);
        }

        private void GenerateReport(string format)
        {
            if (_lastResults.Count == 0)
            {
                AddLog("⚠️ Nenhuma instalação registrada para gerar relatório.", LogLevel.Warning);
                return;
            }

            string path = format == "html"
                ? _report.GenerateHtml(_lastResults, "Instalação", _batchStartTime)
                : _report.GenerateTxt(_lastResults, "Instalação", _batchStartTime);

            AddLog($"📊 Relatório gerado: {path}", LogLevel.Success);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }

        public async Task QuickInstallAsync(AppItem app)
        {
            if (IsBusy) return;

            SetBusy(true, $"Instalando {app.Nome}...");
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _batchStartTime = DateTime.Now;

            try
            {
                AddLog($"━━━ ▶ Instalação rápida: {app.Nome} ━━━", LogLevel.Info);
                app.Status = "⏳ Instalando...";

                var result = await _installer.InstallAppAsync(app, AddLog, _cts.Token);

                app.IsInstalled = result.Success;
                app.Status      = result.Success ? "✅ Instalado" : "❌ Falhou";

                AddLog(result.Success
                    ? $"✅ {app.Nome} instalado em {result.Duration.TotalSeconds:F1}s"
                    : $"❌ Falha ao instalar {app.Nome}: {result.Message}",
                    result.Success ? LogLevel.Success : LogLevel.Error);

                _lastResults = new List<InstallationResult> { result };
                CacheSizeMB  = _cache.GetCacheSizeMB();
                CommandManager.InvalidateRequerySuggested();
                _appData.SaveApps(AllApps.ToList());
            }
            catch (OperationCanceledException)
            {
                app.Status = string.Empty;
                AddLog($"⛔ Instalação de {app.Nome} cancelada.", LogLevel.Warning);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Erro na instalação rápida de {app.Nome}: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                SetBusy(false, "Pronto");
            }
        }

        public OptimizationService GetOptimizationService() => _optimization;

        // ── Gerenciar apps ───────────────────────────────────────────────
        public void AddApp(AppItem app)
        {
            HookApp(app);
            AllApps.Add(app);
            OnPropertyChanged(nameof(SelectedCount));
            BuildCategoryList();
            ApplyFilter();
            _appData.SaveApps(AllApps.ToList());
        }

        public void RemoveApp(AppItem app)
        {
            UnhookApp(app);
            if (app.IsSelected)
            {
                _selectedCount = Math.Max(0, _selectedCount - 1);
                OnPropertyChanged(nameof(SelectedCount));
            }
            AllApps.Remove(app);
            BuildCategoryList();
            ApplyFilter();
            _appData.SaveApps(AllApps.ToList());
        }

        public void SaveApps() => _appData.SaveApps(AllApps.ToList());
        public void SaveProfiles() => _profileSvc.SaveProfiles(Profiles.ToList());

        // ── Plugins ───────────────────────────────────────────────────────
        public async Task RunPluginAsync(string scriptPath)
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            SetBusy(true, $"Executando plugin: {Path.GetFileName(scriptPath)}");
            try
            {
                AddLog($"🔌 Plugin: {scriptPath}", LogLevel.Info);
                await _ps.ExecuteScriptFileAsync(scriptPath, AddLog, _cts.Token);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Erro ao executar plugin: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                SetBusy(false, "Pronto");
            }
        }

        // ── Helpers de Log Estruturado ────────────────────────────────────
        public void AddLog(string message, LogLevel level = LogLevel.Info)
        {
            var entry = new LogEntry { Message = message, Level = level, Category = "UI" };
            _logSvc.WriteToFile(entry, "UI");
            AppendEntryToUi(entry);
        }

        private void AppendEntryToUi(LogEntry entry)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null) return;

            void Append()
            {
                Logs.Add(entry);
                while (Logs.Count > MaxLogEntries)
                    Logs.RemoveAt(0);
            }

            if (dispatcher.CheckAccess()) Append();
            else dispatcher.BeginInvoke(DispatcherPriority.Background, Append);
        }

        private void SetBusy(bool busy, string status)
        {
            IsBusy     = busy;
            StatusText = status;
            if (!busy) Progress = 0;
        }

        private void Cancel()
        {
            _cts?.Cancel();
            AddLog("⛔ Operação cancelada pelo usuário.", LogLevel.Warning);
        }

        public void Dispose()
        {
            _logSvc.EntryLogged -= AppendEntryToUi;
            _searchDebounce.Stop();
            try { _cts?.Cancel(); } catch { }
            _cts?.Dispose();
            foreach (var app in AllApps) UnhookApp(app);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
