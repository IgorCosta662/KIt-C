using System.IO;
using System.Text;
using System.Text.Json;
using KitTecnicoPro.Models;
using KitTecnicoPro.Serialization;

namespace KitTecnicoPro.Services
{
    /// <summary>Gerencia leitura e gravação atômica dos perfis de instalação (profiles.json) via Source Generator.</summary>
    public class ProfileService
    {
        private readonly string _filePath;

        public ProfileService()
        {
            _filePath = Path.Combine(AppPaths.DataDir, "profiles.json");
            string defaultJson = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "profiles.json");
            if (!File.Exists(_filePath) && File.Exists(defaultJson))
            {
                try { File.Copy(defaultJson, _filePath, true); } catch { }
            }
        }

        public List<Profile> LoadProfiles()
        {
            if (!File.Exists(_filePath)) return GetDefaultProfiles();
            try
            {
                var json = File.ReadAllText(_filePath, Encoding.UTF8);
                return JsonSerializer.Deserialize(json, KitJsonContext.IndentedUtf8.ListProfile) ?? GetDefaultProfiles();
            }
            catch
            {
                return GetDefaultProfiles();
            }
        }

        public void SaveProfiles(List<Profile> profiles)
        {
            string? dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(profiles, KitJsonContext.IndentedUtf8.ListProfile);
            string tmp  = $"{_filePath}.{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(tmp, json, Encoding.UTF8);
                File.Move(tmp, _filePath, overwrite: true);
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
        }

        private static List<Profile> GetDefaultProfiles() => new()
        {
            new Profile
            {
                Nome = "Básico",
                Descricao = "Programas essenciais para uso cotidiano",
                Icone = "🖥️",
                Apps = new List<string>
                {
                    "Google.Chrome",
                    "7zip.7zip",
                    "Adobe.Acrobat.Reader.64-bit",
                    "VideoLAN.VLC",
                    "Telegram.TelegramDesktop",
                    "Zoom.Zoom",
                    "Malwarebytes.Malwarebytes"
                }
            },
            new Profile
            {
                Nome = "Gamer",
                Descricao = "Setup completo para jogadores",
                Icone = "🎮",
                Apps = new List<string>
                {
                    "Google.Chrome",
                    "Valve.Steam",
                    "EpicGames.EpicGamesLauncher",
                    "Discord.Discord",
                    "Microsoft.DirectX",
                    "Microsoft.VCRedist.2022.x64",
                    "7zip.7zip",
                    "VideoLAN.VLC"
                }
            },
            new Profile
            {
                Nome = "Programador",
                Descricao = "Ferramentas de desenvolvimento completas",
                Icone = "💻",
                Apps = new List<string>
                {
                    "Microsoft.VisualStudioCode",
                    "Git.Git",
                    "Python.Python.3.12",
                    "OpenJS.NodeJS.LTS",
                    "Microsoft.DotNet.SDK.8",
                    "Google.Chrome",
                    "Mozilla.Firefox",
                    "7zip.7zip",
                    "Telegram.TelegramDesktop"
                }
            },
            new Profile
            {
                Nome = "Escritório",
                Descricao = "Ambiente produtivo para trabalho",
                Icone = "📄",
                Apps = new List<string>
                {
                    "Google.Chrome",
                    "TheDocumentFoundation.LibreOffice",
                    "Adobe.Acrobat.Reader.64-bit",
                    "Zoom.Zoom",
                    "Telegram.TelegramDesktop",
                    "7zip.7zip",
                    "Malwarebytes.Malwarebytes",
                    "VideoLAN.VLC"
                }
            }
        };
    }
}
