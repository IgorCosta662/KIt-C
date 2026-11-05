using System.Windows;
using System.Windows.Controls;
using KitTecnicoPro.Models;

namespace KitTecnicoPro.Views
{
    public partial class AddToolDialog : Window
    {
        public ToolItem? Result { get; private set; }
        private readonly ToolItem? _editing;

        public AddToolDialog(ToolItem? item = null)
        {
            InitializeComponent();

            this.SourceInitialized += (s, e) =>
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch { }
            };
            _editing = item;
            if (item != null) Populate(item);
        }

        private void Populate(ToolItem item)
        {
            TxtNome.Text       = item.Nome;
            TxtCaminho.Text    = item.Caminho;
            TxtArgs.Text       = item.Args;
            TxtDescricao.Text  = item.Descricao;
            ChkAdmin.IsChecked = item.RunAsAdmin;

            string[] cats = { "Ferramentas", "Plugins", "Cracks" };
            int idx = Array.IndexOf(cats, item.Categoria);
            if (idx >= 0) CmbCategoria.SelectedIndex = idx;

            Title = "✏️ Editar Ferramenta";
        }

        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title            = "Selecionar executável",
                Filter           = "Executáveis|*.exe;*.bat;*.cmd;*.msi;*.msp|Todos os arquivos|*.*",
                InitialDirectory = AppPaths.ToolsDir,
                CheckFileExists  = true
            };
            if (dlg.ShowDialog() == true)
            {
                TxtCaminho.Text = dlg.FileName;
                if (string.IsNullOrWhiteSpace(TxtNome.Text))
                    TxtNome.Text = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNome.Text))
            { ShowErro("⚠ Nome é obrigatório."); TxtNome.Focus(); return; }

            if (string.IsNullOrWhiteSpace(TxtCaminho.Text))
            { ShowErro("⚠ Caminho do executável é obrigatório."); TxtCaminho.Focus(); return; }

            string rawCat = (CmbCategoria.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Ferramentas";
            string categoria = rawCat.Contains(' ') ? rawCat[(rawCat.IndexOf(' ') + 1)..].Trim() : rawCat.Trim();

            if (_editing != null)
            {
                _editing.Nome       = TxtNome.Text.Trim();
                _editing.Descricao  = TxtDescricao.Text.Trim();
                _editing.Categoria  = categoria;
                _editing.Caminho    = TxtCaminho.Text.Trim();
                _editing.Args       = TxtArgs.Text.Trim();
                _editing.RunAsAdmin = ChkAdmin.IsChecked == true;
                _editing.Refresh();
                Result = _editing;
            }
            else
            {
                Result = new ToolItem
                {
                    Nome       = TxtNome.Text.Trim(),
                    Descricao  = TxtDescricao.Text.Trim(),
                    Categoria  = categoria,
                    Caminho    = TxtCaminho.Text.Trim(),
                    Args       = TxtArgs.Text.Trim(),
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
