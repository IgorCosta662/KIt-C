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
    public partial class ScriptsWindow : Window
    {
        private ObservableCollection<ScriptItem> _all  = new();
        private ObservableCollection<ScriptItem> _view = new();
        private static string DataFile => AppPaths.GetDataFile("scripts.json");

        public ScriptsWindow()
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
                        KitJsonContext.IndentedUtf8.ObservableCollectionScriptItem) ?? new();
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
                    JsonSerializer.Serialize(_all, KitJsonContext.IndentedUtf8.ObservableCollectionScriptItem),
                    Encoding.UTF8);
            }
            catch { }
        }

        private void Refresh()
        {
            if (CmbFiltro == null || TxtBusca == null || GridScripts == null || TxtStatus == null) return;

            string filtro = (CmbFiltro.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
            string busca  = TxtBusca.Text.Trim().ToLowerInvariant();

            _view = new ObservableCollection<ScriptItem>(_all.Where(s =>
            {
                bool catOk = filtro == "Todos" || s.Categoria == filtro.Split(' ').Last();
                bool nmOk  = string.IsNullOrEmpty(busca)
                          || s.Nome.ToLowerInvariant().Contains(busca)
                          || s.Descricao.ToLowerInvariant().Contains(busca);
                return catOk && nmOk;
            }));

            GridScripts.ItemsSource = _view;
            TxtStatus.Text = $"{_view.Count} script(s) exibido(s) de {_all.Count} total";
        }

        private void OnFiltroChanged(object s, SelectionChangedEventArgs e) => Refresh();
        private void OnBuscaChanged(object s, TextChangedEventArgs e) => Refresh();

        private void OnAddScriptClick(object sender, RoutedEventArgs e)
        {
            var win = new AddScriptDialog { Owner = this };
            if (win.ShowDialog() == true && win.Result != null)
            {
                _all.Add(win.Result);
                Save();
                Refresh();
            }
        }

        private void OnEditScriptClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not ScriptItem item) return;
            var win = new AddScriptDialog(item) { Owner = this };
            if (win.ShowDialog() == true)
            {
                Save();
                Refresh();
            }
        }

        private void OnRemoveScriptClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not ScriptItem item) return;
            if (MessageBox.Show($"Remover '{item.Nome}'?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _all.Remove(item);
                Save();
                Refresh();
            }
        }

        private async void OnRunClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not ScriptItem item) return;
            await RunScriptAsync(item);
        }

        private async void OnRunSelectedClick(object sender, RoutedEventArgs e)
        {
            var selected = GridScripts.SelectedItems.Cast<ScriptItem>().ToList();
            if (selected.Count == 0) { TxtStatus.Text = "⚠ Nenhum script selecionado."; return; }
            foreach (var s in selected) await RunScriptAsync(s);
        }

        private async Task RunScriptAsync(ScriptItem item)
        {
            if (!item.Existe)
            {
                Append($"❌ Arquivo não encontrado: {item.Caminho}\n");
                return;
            }

            TxtStatus.Text = $"⏳ Executando: {item.Nome}...";
            Append($"\n▶ [{item.Nome}] — {DateTime.Now:HH:mm:ss}\n");
            Append(new string('─', 60) + "\n");

            try
            {
                if (item.RunAsAdmin && !Services.SystemService.IsAdministrator())
                {
                    var elevatedPsi = new ProcessStartInfo
                    {
                        FileName        = "powershell.exe",
                        Arguments       = $"-NoProfile -ExecutionPolicy Bypass -File \"{item.Caminho}\"",
                        UseShellExecute = true,
                        Verb            = "runas"
                    };
                    using var elevatedProc = Process.Start(elevatedPsi);
                    if (elevatedProc != null)
                    {
                        await elevatedProc.WaitForExitAsync();
                        Append($"✅ Finalizado com elevação UAC (código {elevatedProc.ExitCode})\n");
                        TxtStatus.Text = $"✅ {item.Nome} — código {elevatedProc.ExitCode}";
                    }
                    return;
                }

                var psi = new ProcessStartInfo
                {
                    FileName               = "powershell.exe",
                    Arguments              = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{item.Caminho}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding  = Encoding.UTF8
                };

                using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
                proc.OutputDataReceived += (_, args) => { if (args.Data != null) Dispatcher.BeginInvoke(() => Append(args.Data + "\n")); };
                proc.ErrorDataReceived  += (_, args) => { if (args.Data != null) Dispatcher.BeginInvoke(() => Append("⚠ " + args.Data + "\n")); };
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                await proc.WaitForExitAsync();
                Append($"✅ Finalizado (código {proc.ExitCode})\n");
                TxtStatus.Text = $"✅ {item.Nome} — código {proc.ExitCode}";
            }
            catch (Exception ex)
            {
                Append($"❌ Erro: {ex.Message}\n");
                TxtStatus.Text = $"❌ Erro ao executar {item.Nome}";
            }
        }

        private void Append(string text)
        {
            TxtOutput.AppendText(text);
            OutputScroll.ScrollToBottom();
        }

        private void OnClearOutputClick(object sender, RoutedEventArgs e)
            => TxtOutput.Clear();

        private void OnOpenFolderClick(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(AppPaths.ScriptsDir);
            Process.Start("explorer.exe", AppPaths.ScriptsDir);
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
                if (!file.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase)) continue;
                var item = new ScriptItem
                {
                    Nome      = Path.GetFileNameWithoutExtension(file),
                    Caminho   = file,
                    Categoria = "Personalizado",
                    Descricao = "Adicionado via drag & drop"
                };
                _all.Add(item);
                added++;
            }

            if (added > 0) { Save(); Refresh(); TxtStatus.Text = $"✅ {added} script(s) adicionado(s)"; }
            else TxtStatus.Text = "⚠ Apenas arquivos .ps1 são suportados";
        }
    }
}
