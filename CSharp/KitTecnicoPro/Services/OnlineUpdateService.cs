using System.Text.Json;
using KitTecnicoPro.Models;
using KitTecnicoPro.Serialization;

namespace KitTecnicoPro.Services
{
    /// <summary>Baixa apps.json de uma URL remota usando ResilientHttpService e Source Generator, com fallback local.</summary>
    public class OnlineUpdateService
    {
        private readonly ResilientHttpService _http;

        public OnlineUpdateService(ResilientHttpService? http = null)
        {
            _http = http ?? new ResilientHttpService(timeout: TimeSpan.FromSeconds(30));
        }

        public async Task<List<AppItem>?> FetchAppsJsonAsync(
            string url,
            Action<string, LogLevel> log,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                log("⚠️ URL de atualização não configurada.", LogLevel.Warning);
                return null;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && !uri.IsLoopback))
            {
                log("⚠️ URL remota inválida ou insegura (use HTTPS).", LogLevel.Warning);
                return null;
            }

            log($"🌐 Baixando lista remota (com retry automático): {url}", LogLevel.Info);

            try
            {
                string json = await _http.GetStringWithRetryAsync(uri, TimeSpan.FromSeconds(30), ct);
                var apps = JsonSerializer.Deserialize(json, KitJsonContext.IndentedUtf8.ListAppItem);

                if (apps == null || apps.Count == 0)
                {
                    log("⚠️ Lista remota vazia ou inválida. Usando lista local.", LogLevel.Warning);
                    return null;
                }

                log($"✅ {apps.Count} programas baixados da fonte remota.", LogLevel.Success);
                return apps;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                log("⛔ Atualização online cancelada.", LogLevel.Warning);
                return null;
            }
            catch (Exception ex)
            {
                log($"❌ Falha ao acessar lista remota: {ex.Message}", LogLevel.Warning);
                log("   ↩ Usando lista local como fallback.", LogLevel.Info);
                return null;
            }
        }

        public Task<bool> IsUrlReachableAsync(string url, CancellationToken ct = default)
            => _http.IsReachableAsync(url, ct);
    }
}
