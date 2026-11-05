using System.Windows;
using System.Windows.Controls;
using KitTecnicoPro.Models;

namespace KitTecnicoPro.Views
{
    public partial class AddScriptDialog : Window
    {
        public ScriptItem? Result { get; private set; }
        private readonly ScriptItem? _editing;

        public AddScriptDialog(ScriptItem? item = null)
        {
            InitializeComponent();

            this.SourceInitialized += (s, e) => 
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch {}
            };
            _editing = item;
            if (item != null) Populate(item);
        }

        private void Populate(ScriptItem item)
        {
            TxtNome.Text      = item.Nome;
            TxtCaminho.Text   = item.Caminho;
            TxtDescricao.Text = item.Descricao;
            ChkAdmin.IsChecked = item.RunAsAdmin;

            string[] cats = { "Sistema", "Rede", "Segurança", "Manutenção", "Personalizado" };
            int idx = Array.IndexOf(cats, item.Categoria);
            if (idx >= 0) CmbCategoria.SelectedIndex = idx;

            Title = "✏️ Editar Script";
        }

        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title           = "Selecionar script PowerShell",
                Filter          = "Scripts PowerShell|*.ps1|Todos os arquivos|*.*",
                InitialDirectory = AppPaths.ScriptsDir,
                CheckFileExists  = true
            };
            if (dlg.ShowDialog() == true)
                TxtCaminho.Text = dlg.FileName;
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNome.Text))
            { ShowErro("⚠ Nome é obrigatório."); TxtNome.Focus(); return; }

            if (string.IsNullOrWhiteSpace(TxtCaminho.Text))
            { ShowErro("⚠ Selecione o arquivo .ps1."); return; }

            string categoria = (CmbCategoria.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Personalizado";

            if (_editing != null)
            {
                _editing.Nome      = TxtNome.Text.Trim();
                _editing.Descricao = TxtDescricao.Text.Trim();
                _editing.Categoria = categoria;
                _editing.Caminho   = TxtCaminho.Text.Trim();
                _editing.RunAsAdmin = ChkAdmin.IsChecked == true;
                _editing.Refresh();
                Result = _editing;
            }
            else
            {
                Result = new ScriptItem
                {
                    Nome      = TxtNome.Text.Trim(),
                    Descricao = TxtDescricao.Text.Trim(),
                    Categoria = categoria,
                    Caminho   = TxtCaminho.Text.Trim(),
                    RunAsAdmin = ChkAdmin.IsChecked == true
                };
            }

            DialogResult = true;
        }

        private void ShowErro(string msg)
        {
            TxtErro.Text = msg;
            TxtErro.Visibility = Visibility.Visible;
        }
    }
}

