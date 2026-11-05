using System.IO;

namespace KitTecnicoPro
{
    /// <summary>
    /// Centraliza os caminhos de pastas do aplicativo e do usuário.
    /// Suporta modo portátil (pasta local / pendrive) e fallback para %LocalAppData% caso instalado em Program Files.
    /// </summary>
    public static class AppPaths
    {
        private static string? _rootDir;
        private static string? _dataDir;
        private static string? _logsDir;
        private static string? _cacheDir;

        /// <summary>Raiz do projeto ou diretório executável.</summary>
        public static string RootDir
        {
            get
            {
                if (_rootDir != null) return _rootDir;

                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                while (dir != null)
                {
                    if (File.Exists(Path.Combine(dir.FullName, "Iniciar.bat")) || File.Exists(Path.Combine(dir.FullName, "Dev.bat")))
                    {
                        _rootDir = dir.FullName;
                        return _rootDir;
                    }
                    dir = dir.Parent;
                }

                _rootDir = AppDomain.CurrentDomain.BaseDirectory;
                return _rootDir;
            }
        }

        /// <summary>Retorna um diretório gravável (preferencialmente portátil, com fallback para AppData local).</summary>
        public static string GetWritableDir(string subFolder)
        {
            string localPath = Path.Combine(RootDir, subFolder);
            try
            {
                Directory.CreateDirectory(localPath);
                // Teste rápido de escrita
                string testFile = Path.Combine(localPath, ".write_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return localPath;
            }
            catch
            {
                // Fallback para %LocalAppData%\KitTecnicoPro\<subFolder>
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KitTecnicoPro", subFolder);
                Directory.CreateDirectory(appDataPath);
                return appDataPath;
            }
        }

        public static string DataDir    => _dataDir  ??= GetWritableDir("Data");
        public static string LogsDir    => _logsDir  ??= GetWritableDir("Logs");
        public static string CacheDir   => _cacheDir ??= GetWritableDir("cache");
        public static string PluginsDir => GetWritableDir("Plugins");

        /// <summary>
        /// Retorna o caminho de um arquivo de dados na pasta gravável DataDir.
        /// Se ainda não existir lá, copia o arquivo semente padrão do diretório do executável/projeto.
        /// </summary>
        public static string GetDataFile(string fileName)
        {
            string targetPath = Path.Combine(DataDir, fileName);
            if (!File.Exists(targetPath))
            {
                string[] seeds =
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", fileName),
                    Path.Combine(RootDir, "Data", fileName),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
                    Path.Combine(RootDir, fileName)
                };

                foreach (var seed in seeds)
                {
                    if (File.Exists(seed))
                    {
                        try
                        {
                            Directory.CreateDirectory(DataDir);
                            File.Copy(seed, targetPath, overwrite: false);
                        }
                        catch { }
                        break;
                    }
                }
            }
            return targetPath;
        }

        /// <summary>Pasta raiz de todos os setups e ISOs visíveis ao usuário.</summary>
        public static string SetupsDir => Path.Combine(RootDir, "Setups");

        /// <summary>Pasta de ISOs (subpastas por categoria).</summary>
        public static string IsosDir   => Path.Combine(RootDir, "Setups", "ISOs");

        /// <summary>Pasta de setup.exe organizados por categoria.</summary>
        public static string SetupsCatDir(string categoria)
            => Path.Combine(RootDir, "Setups", categoria);

        /// <summary>Pasta de scripts PowerShell.</summary>
        public static string ScriptsDir  => Path.Combine(RootDir, "Scripts");

        /// <summary>Subpasta de scripts por categoria.</summary>
        public static string ScriptsCatDir(string cat) => Path.Combine(ScriptsDir, cat);

        /// <summary>Pasta raiz de ferramentas manuais e plugins.</summary>
        public static string ToolsDir    => Path.Combine(RootDir, "Ferramentas");

        /// <summary>Subpasta de ferramentas/plugins por categoria.</summary>
        public static string ToolsCatDir(string cat) => Path.Combine(ToolsDir, cat);

        /// <summary>Pasta de cracks / ativadores.</summary>
        public static string CracksDir   => Path.Combine(RootDir, "Ferramentas", "Cracks");
    }
}
