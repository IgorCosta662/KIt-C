using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using KitTecnicoPro.Models;
using KitTecnicoPro.Serialization;

namespace KitTecnicoPro.Views
{
    public partial class ToolsWindow : Window
    {
        private ObservableCollection<ToolItem> _all  = new();
        private string _tabAtiva = "Todos";
        private static string DataFile => AppPaths.GetDataFile("tools.json");

        public ToolsWindow()
        {
            InitializeComponent();

            this.SourceInitialized += (_, _) =>
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch { }
            };
            Load();
            Refresh();
        }

        private void Load()
        {
            string path = DataFile;
            if (File.Exists(path))
            {
                try
                {
                    _all = JsonSerializer.Deserialize(
                        File.ReadAllText(path, Encoding.UTF8),
                        KitJsonContext.IndentedUtf8.ObservableCollectionToolItem) ?? new();
                }
                catch { }
            }
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(
                    DataFile,
                    JsonSerializer.Serialize(_all, KitJsonContext.IndentedUtf8.ObservableCollectionToolItem),
                    Encoding.UTF8);
            }
            catch { }
        }

        private void Refresh()
        {
            if (TxtBusca == null || GridTools == null || TxtStatus == null) return;

            string busca = TxtBusca.Text.Trim().ToLowerInvariant();
            var view = _all.Where(t =>
            {
                bool catOk = _tabAtiva == "Todos" || t.Categoria == _tabAtiva;
                bool nmOk  = string.IsNullOrEmpty(busca)
                          || t.Nome.ToLowerInvariant().Contains(busca)
                          || t.Descricao.ToLowerInvariant().Contains(busca);
                return catOk && nmOk;
            });
            var list = view.ToList();
            GridTools.ItemsSource = list;
            TxtStatus.Text = $"{list.Count} item(s) — categoria: {_tabAtiva}";
        }

        private void OnTabClick(object sender, RoutedEventArgs e)
        {
            _tabAtiva = (sender as Button)?.Tag?.ToString() ?? "Todos";
            Refresh();
        }

        private void OnBuscaChanged(object s, TextChangedEventArgs e) => Refresh();

        private void OnAddToolClick(object sender, RoutedEventArgs e)
        {
            var win = new AddToolDialog { Owner = this };
            if (win.ShowDialog() == true && win.Result != null)
            {
                _all.Add(win.Result);
                Save();
                Refresh();
            }
        }

        private void OnEditToolClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not ToolItem item) return;
            var win = new AddToolDialog(item) { Owner = this };
            if (win.ShowDialog() == true)
            {
                Save();
                Refresh();
            }
        }

        private void OnRemoveToolClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not ToolItem item) return;
            if (MessageBox.Show($"Remover '{item.Nome}'?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _all.Remove(item);
                Save();
                Refresh();
            }
        }

        private void OnRunToolClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not ToolItem item) return;

            if (!item.Existe)
            {
                MessageBox.Show($"Arquivo não encontrado:\n{item.Caminho}", item.Nome,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName        = item.Caminho,
                    Arguments       = item.Args,
                    UseShellExecute = true,
                    Verb            = item.RunAsAdmin ? "runas" : ""
                };
                Process.Start(psi);
                TxtStatus.Text = $"✅ Aberto: {item.Nome}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir:\n{ex.Message}", item.Nome,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnOpenFileLocation(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not ToolItem item) return;
            if (File.Exists(item.Caminho))
                Process.Start("explorer.exe", $"/select,\"{item.Caminho}\"");
            else if (!string.IsNullOrWhiteSpace(item.Caminho))
            {
                string? dir = Path.GetDirectoryName(item.Caminho);
                if (dir != null && Directory.Exists(dir))
                    Process.Start("explorer.exe", dir);
            }
        }

        private void OnOpenFolderClick(object sender, RoutedEventArgs e)
        {
            string pasta = _tabAtiva == "Cracks" ? AppPaths.CracksDir : AppPaths.ToolsDir;
            Directory.CreateDirectory(pasta);
            Process.Start("explorer.exe", pasta);
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            int added = 0;
            foreach (var file in files)
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is not (".exe" or ".bat" or ".cmd" or ".msi" or ".msp")) continue;

                string cat = _tabAtiva == "Todos" ? "Ferramentas" : _tabAtiva;
                var item = new ToolItem
                {
                    Nome      = Path.GetFileNameWithoutExtension(file),
                    Caminho   = file,
                    Categoria = cat,
                    Descricao = "Adicionado via drag & drop"
                };
                _all.Add(item);
                added++;
            }

            if (added > 0) { Save(); Refresh(); TxtStatus.Text = $"✅ {added} arquivo(s) adicionado(s)"; }
            else TxtStatus.Text = "⚠ Nenhum arquivo compatível (.exe, .bat, .msi...)";
        }
    }
}
