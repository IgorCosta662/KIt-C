using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace KitTecnicoPro.Services
{
    /// <summary>Gerencia cache local de instaladores baixados via URL, evitando re-downloads.</summary>
    public class CacheService
    {
        private readonly string _cacheDir;

        public CacheService()
        {
            _cacheDir = AppPaths.CacheDir;
            Directory.CreateDirectory(_cacheDir);
        }

        /// <summary>Retorna o caminho do arquivo em cache se existir e não estiver vazio, ou null.</summary>
        public string? GetCachedPath(string url)
        {
            string path = BuildCachePath(url);
            return File.Exists(path) && new FileInfo(path).Length > 0 ? path : null;
        }

        /// <summary>Retorna o caminho determinístico onde o instalador dessa URL deve ser salvo em cache.</summary>
        public string GetTargetCachePath(string url)
        {
            Directory.CreateDirectory(_cacheDir);
            return BuildCachePath(url);
        }

        /// <summary>Salva um arquivo no cache.</summary>
        public async Task SaveToCacheAsync(string url, byte[] data, CancellationToken ct = default)
        {
            string path = GetTargetCachePath(url);
            await File.WriteAllBytesAsync(path, data, ct);
        }

        /// <summary>Remove todos os arquivos da pasta /cache.</summary>
        public void ClearCache()
        {
            if (!Directory.Exists(_cacheDir)) return;
            foreach (var file in Directory.GetFiles(_cacheDir))
            {
                try { File.Delete(file); } catch { }
            }
        }

        /// <summary>Retorna o tamanho total do cache em MB.</summary>
        public double GetCacheSizeMB()
        {
            if (!Directory.Exists(_cacheDir)) return 0;
            return Directory.GetFiles(_cacheDir)
                .Where(f => !f.EndsWith(".part", StringComparison.OrdinalIgnoreCase))
                .Sum(f => new FileInfo(f).Length) / 1024.0 / 1024.0;
        }

        public int GetCacheFileCount()
            => Directory.Exists(_cacheDir) ? Directory.GetFiles(_cacheDir).Length : 0;

        public List<(string Name, double SizeMB, DateTime Created)> GetCacheItems()
        {
            if (!Directory.Exists(_cacheDir)) return new();
            return Directory.GetFiles(_cacheDir)
                .Select(f =>
                {
                    var fi = new FileInfo(f);
                    return (fi.Name, fi.Length / 1024.0 / 1024.0, fi.CreationTime);
                })
                .ToList();
        }

        private string BuildCachePath(string url)
        {
            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(Encoding.UTF8.GetBytes(url), hash);
            string hashStr = Convert.ToHexString(hash[..8]).ToLowerInvariant();
            string ext = ".exe";
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                string rawExt = Path.GetExtension(uri.LocalPath);
                if (!string.IsNullOrWhiteSpace(rawExt) && rawExt.Length <= 8) ext = rawExt;
            }
            return Path.Combine(_cacheDir, $"{hashStr}{ext}");
        }
    }
}
