using Microsoft.Win32;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KitTecnicoPro.Views
{
    public partial class FileHasherWindow : Window
    {
        private readonly Dictionary<string, string> _hashes = new();

        public FileHasherWindow()
        {
            InitializeComponent();
            this.SourceInitialized += (s, e) =>
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch { }
            };
        }

        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Title = "Selecionar arquivo(s)", Multiselect = true };
            if (dlg.ShowDialog() == true && dlg.FileNames.Length > 0)
                ProcessFiles(dlg.FileNames);
        }

        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            TxtArquivo.Text = "Arraste um arquivo aqui ou clique em 📂 Procurar";
            TxtArquivo.Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
            PanelHashes.Children.Clear();
            _hashes.Clear();
            TxtCompare.Text = "";
            TxtCompareResult.Text = "";
            PrgBar.Value = 0;
            TxtStatus.Text = "Pronto.";
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0) ProcessFiles(files);
        }

        private async void ProcessFiles(string[] paths)
        {
            var validPaths = paths.Where(File.Exists).ToArray();
            if (validPaths.Length == 0) { TxtStatus.Text = "❌ Arquivo não encontrado."; return; }

            var firstFi = new FileInfo(validPaths[0]);
            TxtArquivo.Text = validPaths.Length == 1
                ? $"{firstFi.Name}  ({FormatSize(firstFi.Length)})"
                : $"{validPaths.Length} arquivos selecionados (primeiro: {firstFi.Name})";
            TxtArquivo.Foreground = new SolidColorBrush(Color.FromRgb(0xEC, 0xEF, 0xF1));
            PanelHashes.Children.Clear();
            _hashes.Clear();
            PrgBar.Value = 0;
            TxtStatus.Text = "⏳ Calculando hashes em leitura única...";

            for (int idx = 0; idx < validPaths.Length; idx++)
            {
                string path = validPaths[idx];
                var fi = new FileInfo(path);

                var fileHashes = await Task.Run(() =>
                {
                    using var md5    = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
                    using var sha1   = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
                    using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                    using var sha512 = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 64);

                    byte[] buffer = new byte[1024 * 64];
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        var span = buffer.AsSpan(0, read);
                        md5.AppendData(span);
                        sha1.AppendData(span);
                        sha256.AppendData(span);
                        sha512.AppendData(span);
                    }

                    return new Dictionary<string, string>
                    {
                        ["MD5"]    = Convert.ToHexString(md5.GetHashAndReset()).ToLowerInvariant(),
                        ["SHA1"]   = Convert.ToHexString(sha1.GetHashAndReset()).ToLowerInvariant(),
                        ["SHA256"] = Convert.ToHexString(sha256.GetHashAndReset()).ToLowerInvariant(),
                        ["SHA512"] = Convert.ToHexString(sha512.GetHashAndReset()).ToLowerInvariant()
                    };
                });

                if (validPaths.Length > 1)
                {
                    PanelHashes.Children.Add(new TextBlock
                    {
                        Text = $"📄 {fi.Name} ({FormatSize(fi.Length)})",
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA)),
                        Margin = new Thickness(0, idx == 0 ? 0 : 8, 0, 6)
                    });
                }

                foreach (var kv in fileHashes)
                {
                    string key = validPaths.Length == 1 ? kv.Key : $"{fi.Name}:{kv.Key}";
                    _hashes[key] = kv.Value;
                    AddHashRow(kv.Key, kv.Value);
                }

                PrgBar.Value = (int)((idx + 1) * 100.0 / validPaths.Length);
            }

            TxtStatus.Text = validPaths.Length == 1
                ? $"✅ Hashes calculados para: {firstFi.Name}"
                : $"✅ Hashes calculados para {validPaths.Length} arquivos";
            OnCompareChanged(null!, null!);
        }

        private void AddHashRow(string algo, string hash)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };

            panel.Children.Add(new TextBlock
            {
                Text = algo,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
                Margin = new Thickness(0, 0, 0, 3)
            });

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tb = new TextBox
            {
                Text = hash,
                IsReadOnly = true,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0xA5, 0xD6, 0xA7)),
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x2A, 0x3A)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 6, 8, 6),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(tb, 0);

            var btn = new Button
            {
                Content = "📋",
                FontSize = 14,
                ToolTip = "Copiar",
                Tag = hash,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            btn.SetResourceReference(StyleProperty, "MaterialDesignFlatButton");
            btn.Click += (_, _) => { Clipboard.SetText(hash); TxtStatus.Text = $"✅ {algo} copiado!"; };
            Grid.SetColumn(btn, 1);

            row.Children.Add(tb);
            row.Children.Add(btn);
            panel.Children.Add(row);
            PanelHashes.Children.Add(panel);
        }

        private void OnCompareChanged(object sender, TextChangedEventArgs e)
        {
            string compare = TxtCompare?.Text?.Trim().ToLowerInvariant() ?? "";
            if (string.IsNullOrEmpty(compare) || _hashes.Count == 0)
            { TxtCompareResult.Text = ""; return; }

            bool match = _hashes.Values.Any(h => h.Equals(compare, StringComparison.OrdinalIgnoreCase));
            TxtCompareResult.Text = match ? "✅ Corresponde!" : "❌ Não corresponde";
            TxtCompareResult.Foreground = new SolidColorBrush(
                match ? Color.FromRgb(0x4C, 0xAF, 0x50) : Color.FromRgb(0xEF, 0x53, 0x50));
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / 1024.0 / 1024.0:F1} MB";
            return $"{bytes / 1024.0 / 1024.0 / 1024.0:F2} GB";
        }
    }
}
