using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using KitTecnicoPro.Models;

namespace KitTecnicoPro.Services
{
    /// <summary>
    /// Motor de instalação: gerencia instalação via winget, downloads customizados resilientes com streaming,
    /// cache local com integridade SHA-256, modo paralelo e fila inteligente.
    /// </summary>
    public class InstallationService
    {
        private readonly PowerShellService    _ps;
        private readonly CacheService         _cache;
        private readonly ResilientHttpService _http;
        private readonly IAppLogger?          _logger;
        private readonly string               _offlineDir;

        public InstallationService(
            PowerShellService psService,
            CacheService? cacheService = null,
            ResilientHttpService? httpService = null,
            IAppLogger? logger = null)
        {
            _ps         = psService;
            _cache      = cacheService ?? new CacheService();
            _http       = httpService  ?? new ResilientHttpService(logger);
            _logger     = logger;
            _offlineDir = AppPaths.GetWritableDir("offline");
        }

        /// <summary>Instala um único aplicativo e retorna o resultado.</summary>
        public async Task<InstallationResult> InstallAppAsync(
            AppItem app,
            Action<string, LogLevel> logCallback,
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();

            logCallback($"▶ Iniciando instalação: {app.Nome}", LogLevel.Info);

            // Verificar se existe instalador offline
            string? offlinePath = FindOfflineInstaller(app.Id);
            if (offlinePath != null)
            {
                logCallback($"  📁 Instalador offline encontrado: {offlinePath}", LogLevel.Info);
                bool ok = await InstallFromFileAsync(offlinePath, app.SilentArgs ?? "/silent /norestart", logCallback, cancellationToken);
                sw.Stop();
                return new InstallationResult
                {
                    AppName  = app.Nome,
                    Success  = ok,
                    Message  = ok ? "Instalado via arquivo local" : "Falha na instalação local",
                    Duration = sw.Elapsed
                };
            }

            bool result = (app.Tipo ?? "winget").ToLowerInvariant() switch
            {
                "custom" => await InstallCustomAsync(app, logCallback, cancellationToken),
                "local"  => await InstallLocalAsync(app, logCallback, cancellationToken),
                _        => await InstallWingetAsync(app, logCallback, cancellationToken)
            };

            sw.Stop();

            if (result)
                logCallback($"  ✅ {app.Nome} instalado com sucesso ({sw.Elapsed.TotalSeconds:F1}s)", LogLevel.Success);
            else
                logCallback($"  ❌ Falha ao instalar {app.Nome}", LogLevel.Error);

            return new InstallationResult
            {
                AppName  = app.Nome,
                Success  = result,
                Message  = result ? "Instalado com sucesso" : "Falha na instalação",
                Duration = sw.Elapsed
            };
        }

        private async Task<bool> InstallWingetAsync(AppItem app, Action<string, LogLevel> log, CancellationToken ct)
        {
            if (!PowerShellService.IsSafeWingetId(app.Id))
            {
                log($"  ❌ ID winget inválido para {app.Nome}: '{app.Id}'", LogLevel.Error);
                return false;
            }
            string cmd = $"winget install --id='{PowerShellService.EscapeSingleQuotes(app.Id)}' -e --silent --accept-package-agreements --accept-source-agreements";
            log($"  💻 Comando: {cmd}", LogLevel.Info);
            return await _ps.ExecuteAsync(cmd, log, ct);
        }

        /// <summary>
        /// Faz download via streaming resiliente direto para o cache (zero alocação em RAM)
        /// com progresso real em MB/s e hash SHA-256 calculado em tempo real.
        /// </summary>
        private async Task<bool> InstallCustomAsync(AppItem app, Action<string, LogLevel> log, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(app.Url))
            {
                log($"  ❌ URL não definida para {app.Nome}", LogLevel.Error);
                return false;
            }

            string? cachedPath = _cache.GetCachedPath(app.Url);
            if (cachedPath != null)
            {
                log($"  📦 Cache local validado ({new FileInfo(cachedPath).Length / 1_048_576.0:F1} MB).", LogLevel.Info);
                return await InstallFromFileAsync(cachedPath, app.SilentArgs ?? "/S", log, ct);
            }

            string targetCachePath = _cache.GetTargetCachePath(app.Url);
            log($"  ⬇ Baixando via streaming resiliente: {app.Url}", LogLevel.Info);

            int lastLoggedMilestone = -1;
            var progress = new Progress<DownloadProgress>(p =>
            {
                app.Status = p.Percentage >= 0
                    ? $"⬇ {p.Percentage}% ({p.SpeedMBps:F1} MB/s)"
                    : $"⬇ {p.BytesRead / 1_048_576.0:F1} MB";

                if (p.Percentage >= 0)
                {
                    int milestone = p.Percentage / 25 * 25;
                    if (milestone > 0 && milestone != lastLoggedMilestone && milestone < 100)
                    {
                        lastLoggedMilestone = milestone;
                        log($"  ⏳ {app.Nome}: {p.Percentage}% ({p.BytesRead / 1_048_576.0:F1}/{p.TotalBytes / 1_048_576.0:F1} MB @ {p.SpeedMBps:F1} MB/s)", LogLevel.Info);
                    }
                }
            });

            try
            {
                var dl = await _http.DownloadFileAsync(app.Url, targetCachePath, progress, expectedSha256: null, ct);
                log($"  ✅ Download concluído: {dl.TotalBytes / 1_048_576.0:F1} MB em {dl.Duration.TotalSeconds:F1}s (SHA-256: {dl.Sha256Hex[..12]}…)", LogLevel.Info);
                app.Status = AppStatus.Installing;
                return await InstallFromFileAsync(dl.FilePath, app.SilentArgs ?? "/S", log, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Erro no download de {app.Nome}", ex, "Installer");
                log($"  ❌ Erro no download: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private async Task<bool> InstallLocalAsync(AppItem app, Action<string, LogLevel> log, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(app.LocalPath))
            {
                log($"  ❌ Caminho local não definido para {app.Nome}", LogLevel.Error);
                return false;
            }

            if (!File.Exists(app.LocalPath))
            {
                log($"  ❌ Arquivo não encontrado: {app.LocalPath}", LogLevel.Error);
                return false;
            }

            log($"  📂 Instalando a partir do arquivo local: {app.LocalPath}", LogLevel.Info);
            return await InstallFromFileAsync(app.LocalPath, app.SilentArgs ?? "/S", log, ct);
        }

        private async Task<bool> InstallFromFileAsync(string filePath, string args, Action<string, LogLevel> log, CancellationToken ct)
        {
            log($"  ⚙ Executando: {filePath} {args}", LogLevel.Info);

            var psi = new ProcessStartInfo
            {
                FileName        = filePath,
                Arguments       = args,
                UseShellExecute = false,
                CreateNoWindow  = true
            };

            Process? process = null;
            try
            {
                process = Process.Start(psi);
                if (process == null) return false;
                await process.WaitForExitAsync(ct);
                return process.ExitCode == 0 || process.ExitCode == 3010;
            }
            catch (OperationCanceledException)
            {
                try { if (process is { HasExited: false }) process.Kill(entireProcessTree: true); } catch { }
                throw;
            }
            catch (Exception ex)
            {
                log($"  ❌ Erro ao executar instalador: {ex.Message}", LogLevel.Error);
                return false;
            }
            finally
            {
                process?.Dispose();
            }
        }

        public async Task<bool> UpgradeAllAsync(Action<string, LogLevel> log, CancellationToken ct)
        {
            log("🔄 Iniciando atualização de todos os programas...", LogLevel.Info);
            return await _ps.ExecuteAsync("winget upgrade --all --silent --accept-package-agreements --accept-source-agreements", log, ct);
        }

        public async Task<bool> CreateRestorePointAsync(Action<string, LogLevel> log, CancellationToken ct)
        {
            log("🛡️ Criando ponto de restauração...", LogLevel.Info);
            string cmd = "Enable-ComputerRestore -Drive 'C:\\'; Checkpoint-Computer -Description 'Kit Técnico Pro' -RestorePointType 'MODIFY_SETTINGS'";
            bool ok = await _ps.ExecuteAsync(cmd, log, ct);
            if (ok) log("✅ Ponto de restauração criado com sucesso!", LogLevel.Success);
            return ok;
        }

        public async Task<bool> IsInstalledAsync(string appId)
        {
            if (!PowerShellService.IsSafeWingetId(appId)) return false;
            bool found = false;
            await _ps.ExecuteAsync(
                $"winget list --id '{PowerShellService.EscapeSingleQuotes(appId)}' -e",
                (line, _) => { if (line.Contains(appId, StringComparison.OrdinalIgnoreCase)) found = true; });
            return found;
        }

        public async Task<Func<string, bool>> GetInstalledCheckerAsync(CancellationToken ct = default)
        {
            var tokens    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var truncated = new List<string>();
            var sync      = new object();

            await _ps.ExecuteAsync(
                "winget list --accept-source-agreements | Out-String -Width 4096",
                (line, _) =>
                {
                    foreach (var raw in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        lock (sync)
                        {
                            if (raw.EndsWith('…') && raw.Length > 4) truncated.Add(raw.TrimEnd('…'));
                            else tokens.Add(raw);
                        }
                    }
                }, ct);

            return id =>
            {
                if (string.IsNullOrWhiteSpace(id)) return false;
                if (tokens.Contains(id)) return true;
                foreach (var prefix in truncated)
                    if (id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            };
        }

        private string? FindOfflineInstaller(string appId)
        {
            if (string.IsNullOrWhiteSpace(appId) || !Directory.Exists(_offlineDir)) return null;
            string safe = appId.Replace(".", "_").Replace(" ", "_").ToLowerInvariant();
            foreach (var file in Directory.GetFiles(_offlineDir))
            {
                string name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                if (name.Contains(safe) || name.Contains(appId.ToLowerInvariant()))
                    return file;
            }
            return null;
        }

        /// <summary>Baixa a versão offline de um aplicativo com streaming HTTP resiliente (para URLs) ou winget download.</summary>
        public async Task<bool> DownloadOfflineInstallerAsync(
            AppItem app,
            Action<string, LogLevel> log,
            CancellationToken ct,
            IProgress<DownloadProgress>? downloadProgress = null)
        {
            Directory.CreateDirectory(_offlineDir);

            if (app.Tipo == "custom" && !string.IsNullOrWhiteSpace(app.Url))
            {
                log("  🌐 Baixando instalador offline via streaming resiliente...", LogLevel.Info);
                string fileName = Uri.TryCreate(app.Url, UriKind.Absolute, out var uri)
                    ? Path.GetFileName(uri.AbsolutePath)
                    : string.Empty;
                fileName = string.Concat(fileName.Split(Path.GetInvalidFileNameChars()));
                if (string.IsNullOrWhiteSpace(fileName) || !fileName.Contains('.'))
                    fileName = $"{string.Concat((app.Id ?? app.Nome).Split(Path.GetInvalidFileNameChars()))}.exe";

                string targetPath = Path.Combine(_offlineDir, fileName);

                try
                {
                    var dl = await _http.DownloadFileAsync(app.Url, targetPath, downloadProgress, expectedSha256: null, ct);
                    log($"  ✅ Salvo em {dl.FilePath} ({dl.TotalBytes / 1_048_576.0:F1} MB | SHA-256: {dl.Sha256Hex[..12]}…)", LogLevel.Success);
                    app.LocalPath = targetPath;
                    app.Tipo = "local";
                    return true;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    log($"  ❌ Falha no download HTTP: {ex.Message}", LogLevel.Error);
                    return false;
                }
            }
            else if (app.Tipo == "winget" || app.Tipo == "store")
            {
                if (!PowerShellService.IsSafeWingetId(app.Id))
                {
                    log($"  ❌ ID winget inválido: {app.Id}", LogLevel.Error);
                    return false;
                }
                log("  📦 Solicitando download offline ao winget...", LogLevel.Info);

                string safeName = string.Concat(app.Id.Replace(".", "_").Replace(" ", "_").Split(Path.GetInvalidFileNameChars()));
                string outDir = Path.Combine(_offlineDir, safeName);
                Directory.CreateDirectory(outDir);

                string args = $"download --id '{PowerShellService.EscapeSingleQuotes(app.Id)}' --exact --accept-package-agreements --accept-source-agreements --download-directory '{PowerShellService.EscapeSingleQuotes(outDir)}'";

                bool ok = await _ps.ExecuteAsync($"winget {args}", log, ct);

                if (ok)
                {
                    var files = Directory.GetFiles(outDir, "*.*", SearchOption.AllDirectories)
                        .Where(f => f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                                    f.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) ||
                                    f.EndsWith(".msix", StringComparison.OrdinalIgnoreCase) ||
                                    f.EndsWith(".msixbundle", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (files.Count > 0)
                    {
                        app.LocalPath = files[0];
                        app.Tipo = "local";
                        return true;
                    }

                    log("  ⚠️ Winget relatou sucesso, mas o instalador não foi encontrado na pasta.", LogLevel.Warning);
                    return false;
                }
                return false;
            }
            else if (app.Tipo == "local")
            {
                log($"  ℹ️ {app.Nome} já é um instalador local.", LogLevel.Info);
                return true;
            }

            return false;
        }

        public async Task<List<InstallationResult>> InstallParallelAsync(
            IEnumerable<AppItem> apps,
            int maxConcurrent,
            Action<string, LogLevel> log,
            Action<string, int> progressCallback,
            CancellationToken ct = default)
        {
            var appList = apps.ToList();
            int total   = appList.Count;
            if (total == 0) return new List<InstallationResult>();

            int done    = 0;
            var results = new ConcurrentBag<InstallationResult>();
            using var semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            var tasks = appList.Select(async app =>
            {
                try
                {
                    await semaphore.WaitAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                try
                {
                    if (ct.IsCancellationRequested) return;

                    log($"[⚡] Iniciando paralelo: {app.Nome}", LogLevel.Info);
                    var result = await InstallAppAsync(app, log, ct);
                    results.Add(result);

                    int pct = (int)(Interlocked.Increment(ref done) * 100.0 / total);
                    progressCallback(app.Id, pct);
                }
                catch (OperationCanceledException) { }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return results.ToList();
        }

        public async Task<List<InstallationResult>> RunQueueAsync(
            IEnumerable<QueueItem> queue,
            int maxConcurrent,
            Action<string, LogLevel> log,
            Action<QueueItem, int> progressCallback,
            CancellationToken ct = default)
        {
            var ordered = queue.OrderBy(q => q.Priority).ToList();
            int total   = ordered.Count;
            if (total == 0) return new List<InstallationResult>();

            int done    = 0;
            var results = new ConcurrentBag<InstallationResult>();
            using var semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            var tasks = ordered.Select(async item =>
            {
                try
                {
                    await semaphore.WaitAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    item.Status = QueueItemStatus.Cancelled;
                    return;
                }
                try
                {
                    if (ct.IsCancellationRequested)
                    {
                        item.Status = QueueItemStatus.Cancelled;
                        return;
                    }

                    item.Status = QueueItemStatus.Running;
                    InstallationResult? result = null;

                    for (int attempt = 0; attempt <= item.MaxRetries; attempt++)
                    {
                        if (attempt > 0)
                        {
                            log($"  🔄 Retry {attempt}/{item.MaxRetries}: {item.App.Nome}", LogLevel.Warning);
                            item.RetryCount = attempt;
                            await Task.Delay(2000, ct);
                        }

                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        timeoutCts.CancelAfter(item.Timeout);

                        try
                        {
                            result = await InstallAppAsync(item.App, log, timeoutCts.Token);
                            if (result.Success) break;
                        }
                        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                        {
                            log($"  ⏱ Timeout na instalação de {item.App.Nome}", LogLevel.Warning);
                            result = new InstallationResult
                            {
                                AppName = item.App.Nome,
                                Success = false,
                                Message = "Timeout excedido"
                            };
                        }
                    }

                    item.Status = result?.Success == true
                        ? QueueItemStatus.Success : QueueItemStatus.Failed;

                    if (result != null) results.Add(result);

                    int pct = (int)(Interlocked.Increment(ref done) * 100.0 / total);
                    progressCallback(item, pct);
                }
                catch (OperationCanceledException)
                {
                    item.Status = QueueItemStatus.Cancelled;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return results.ToList();
        }
    }
}
