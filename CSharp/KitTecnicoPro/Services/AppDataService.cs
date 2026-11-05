using System.IO;
using System.Text;
using System.Text.Json;
using KitTecnicoPro.Models;
using KitTecnicoPro.Serialization;

namespace KitTecnicoPro.Services
{
    /// <summary>Gerencia leitura e gravação atômica de apps.json via System.Text.Json Source Generator.</summary>
    public class AppDataService
    {
        private readonly string _filePath;

        public AppDataService()
        {
            _filePath = Path.Combine(AppPaths.DataDir, "apps.json");
            string defaultJson = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "apps.json");
            if (!File.Exists(_filePath) && File.Exists(defaultJson))
            {
                try { File.Copy(defaultJson, _filePath, true); } catch { }
            }
        }

        public List<AppItem> LoadApps()
        {
            List<AppItem> result;
            if (!File.Exists(_filePath))
            {
                result = GetDefaultApps();
            }
            else
            {
                try
                {
                    var json = File.ReadAllText(_filePath, Encoding.UTF8);
                    result = JsonSerializer.Deserialize(json, KitJsonContext.IndentedUtf8.ListAppItem) ?? GetDefaultApps();
                }
                catch
                {
                    result = GetDefaultApps();
                }
            }
            foreach (var item in result) item.UpdateLocalPathState();
            return result;
        }

        public void SaveApps(List<AppItem> apps) => WriteAtomic(_filePath, apps);

        public void ExportApps(List<AppItem> apps, string targetPath) => WriteAtomic(targetPath, apps);

        public List<AppItem> ImportApps(string sourcePath)
        {
            var json = File.ReadAllText(sourcePath, Encoding.UTF8);
            var result = JsonSerializer.Deserialize(json, KitJsonContext.IndentedUtf8.ListAppItem) ?? new List<AppItem>();
            foreach (var item in result) item.UpdateLocalPathState();
            return result;
        }

        private static void WriteAtomic(string path, List<AppItem> apps)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(apps, KitJsonContext.IndentedUtf8.ListAppItem);
            string tmp  = $"{path}.{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(tmp, json, Encoding.UTF8);
                File.Move(tmp, path, overwrite: true);
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
        }

        private static List<AppItem> GetDefaultApps() => new()
        {
            new AppItem { Nome = "Google Chrome",        Id = "Google.Chrome",                     Categoria = "Navegadores",     Descricao = "Navegador web do Google" },
            new AppItem { Nome = "Mozilla Firefox",      Id = "Mozilla.Firefox",                   Categoria = "Navegadores",     Descricao = "Navegador web da Mozilla" },
            new AppItem { Nome = "Microsoft Edge",       Id = "Microsoft.Edge",                    Categoria = "Navegadores",     Descricao = "Navegador da Microsoft" },
            new AppItem { Nome = "Brave Browser",        Id = "Brave.Brave",                       Categoria = "Navegadores",     Descricao = "Navegador focado em privacidade" },
            new AppItem { Nome = "Kaspersky Free",       Id = "Kaspersky.KasperskyFree",           Categoria = "Segurança",       Descricao = "Antivírus gratuito" },
            new AppItem { Nome = "Malwarebytes",         Id = "Malwarebytes.Malwarebytes",         Categoria = "Segurança",       Descricao = "Anti-malware" },
            new AppItem { Nome = "7-Zip",                Id = "7zip.7zip",                         Categoria = "Compactadores",   Descricao = "Compactador de arquivos gratuito" },
            new AppItem { Nome = "WinRAR",               Id = "RARLab.WinRAR",                     Categoria = "Compactadores",   Descricao = "Compactador de arquivos" },
            new AppItem { Nome = "VLC Media Player",     Id = "VideoLAN.VLC",                      Categoria = "Multimídia",      Descricao = "Player multimídia completo" },
            new AppItem { Nome = "K-Lite Codec Pack",    Id = "CodecGuide.K-LiteCodecPack.Full",   Categoria = "Multimídia",      Descricao = "Pacote de codecs de vídeo" },
            new AppItem { Nome = "WhatsApp",             Id = "9NKSQGP7F2NH",                      Categoria = "Comunicação",     Descricao = "Mensagens instantâneas" },
            new AppItem { Nome = "Telegram Desktop",     Id = "Telegram.TelegramDesktop",          Categoria = "Comunicação",     Descricao = "Mensagens seguras" },
            new AppItem { Nome = "Discord",              Id = "Discord.Discord",                   Categoria = "Comunicação",     Descricao = "Chat para gamers" },
            new AppItem { Nome = "Zoom",                 Id = "Zoom.Zoom",                         Categoria = "Comunicação",     Descricao = "Videoconferência" },
            new AppItem { Nome = "LibreOffice",          Id = "TheDocumentFoundation.LibreOffice", Categoria = "Office",          Descricao = "Suite de escritório gratuita" },
            new AppItem { Nome = "Adobe Acrobat Reader", Id = "Adobe.Acrobat.Reader.64-bit",       Categoria = "Office",          Descricao = "Leitor de PDF da Adobe" },
            new AppItem { Nome = "Visual Studio Code",   Id = "Microsoft.VisualStudioCode",        Categoria = "Desenvolvimento", Descricao = "Editor de código da Microsoft" },
            new AppItem { Nome = "Git",                  Id = "Git.Git",                           Categoria = "Desenvolvimento", Descricao = "Controle de versão" },
            new AppItem { Nome = "Python",               Id = "Python.Python.3.12",                Categoria = "Desenvolvimento", Descricao = "Linguagem de programação Python" },
            new AppItem { Nome = "Node.js",              Id = "OpenJS.NodeJS.LTS",                 Categoria = "Desenvolvimento", Descricao = "Runtime JavaScript" },
            new AppItem { Nome = ".NET SDK",             Id = "Microsoft.DotNet.SDK.8",            Categoria = "Desenvolvimento", Descricao = "SDK do .NET 8" },
            new AppItem { Nome = "Steam",                Id = "Valve.Steam",                       Categoria = "Games",           Descricao = "Plataforma de jogos" },
            new AppItem { Nome = "Visual C++ Redist",    Id = "Microsoft.VCRedist.2022.x64",       Categoria = "Games",           Descricao = "Runtime Visual C++ 2022" },
        };
    }
}
