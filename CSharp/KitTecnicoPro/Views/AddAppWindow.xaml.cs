using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using KitTecnicoPro.Models;

namespace KitTecnicoPro.Views
{
    public partial class AddAppWindow : Window
    {
        public AppItem? CreatedApp { get; private set; }
        private readonly AppItem? _editTarget;

        public AddAppWindow(AppItem? editApp = null)
        {
            InitializeComponent();

            this.SourceInitialized += (s, e) =>
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch { }
            };
            _editTarget = editApp;

            if (editApp != null)
            {
                Title = "Editar Programa";
                TxtNome.Text            = editApp.Nome;
                TxtId.Text              = editApp.Id;
                TxtDescricao.Text       = editApp.Descricao;
                TxtUrl.Text             = editApp.Url ?? string.Empty;
                TxtSilentArgs.Text      = editApp.SilentArgs ?? string.Empty;
                TxtLocalPath.Text       = editApp.LocalPath ?? string.Empty;
                TxtLocalSilentArgs.Text = editApp.SilentArgs ?? "/S";

                foreach (ComboBoxItem item in CmbCategoria.Items)
                    if (item.Content?.ToString() == editApp.Categoria)
                    { CmbCategoria.SelectedItem = item; break; }

                foreach (ComboBoxItem item in CmbTipo.Items)
                    if (item.Tag?.ToString() == editApp.Tipo.ToLowerInvariant())
                    { CmbTipo.SelectedItem = item; break; }
            }
            else
            {
                CmbTipo.SelectedIndex      = 0;
                CmbCategoria.SelectedIndex = 11; // Geral
            }
        }

        private void CmbTipo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbTipo.SelectedItem is not ComboBoxItem item) return;
            string tag = item.Tag?.ToString() ?? "winget";
            PanelCustom.Visibility = tag == "custom" ? Visibility.Visible : Visibility.Collapsed;
            PanelLocal.Visibility  = tag == "local"  ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnBrowseLocalClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title           = "Selecionar instalador",
                Filter          = "Instaladores|*.exe;*.msi;*.msp|Todos os arquivos|*.*",
                CheckFileExists = true
            };

            if (dlg.ShowDialog() == true)
            {
                TxtLocalPath.Text = dlg.FileName;
                if (string.IsNullOrWhiteSpace(TxtNome.Text))
                    TxtNome.Text = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNome.Text))
            {
                MessageBox.Show("Nome é obrigatório.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string tipo = (CmbTipo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "winget";
            string categoria = (CmbCategoria.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Geral";

            if (tipo == "winget" && string.IsNullOrWhiteSpace(TxtId.Text))
            {
                MessageBox.Show("Informe o ID Winget do pacote (ex: Google.Chrome).", "Validação",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (tipo == "custom" && (string.IsNullOrWhiteSpace(TxtUrl.Text) || !Uri.TryCreate(TxtUrl.Text.Trim(), UriKind.Absolute, out _)))
            {
                MessageBox.Show("Informe uma URL válida (https://...) para o instalador customizado.", "Validação",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (tipo == "local" && string.IsNullOrWhiteSpace(TxtLocalPath.Text))
            {
                MessageBox.Show("Selecione o arquivo instalador local.", "Validação",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string appId = string.IsNullOrWhiteSpace(TxtId.Text)
                ? TxtNome.Text.Trim().Replace(" ", ".")
                : TxtId.Text.Trim();

            string? url        = tipo == "custom" && !string.IsNullOrWhiteSpace(TxtUrl.Text)       ? TxtUrl.Text.Trim()       : null;
            string? localPath  = tipo == "local"  && !string.IsNullOrWhiteSpace(TxtLocalPath.Text) ? TxtLocalPath.Text.Trim() : null;
            string? silentArgs = tipo == "local"
                ? (string.IsNullOrWhiteSpace(TxtLocalSilentArgs.Text) ? "/S" : TxtLocalSilentArgs.Text.Trim())
                : (string.IsNullOrWhiteSpace(TxtSilentArgs.Text)      ? null : TxtSilentArgs.Text.Trim());

            if (_editTarget != null)
            {
                _editTarget.Nome       = TxtNome.Text.Trim();
                _editTarget.Id         = appId;
                _editTarget.Descricao  = TxtDescricao.Text.Trim();
                _editTarget.Categoria  = categoria;
                _editTarget.Tipo       = tipo;
                _editTarget.Url        = url;
                _editTarget.LocalPath  = localPath;
                _editTarget.SilentArgs = silentArgs;
                CreatedApp = _editTarget;
            }
            else
            {
                CreatedApp = new AppItem
                {
                    Nome       = TxtNome.Text.Trim(),
                    Id         = appId,
                    Descricao  = TxtDescricao.Text.Trim(),
                    Categoria  = categoria,
                    Tipo       = tipo,
                    Url        = url,
                    LocalPath  = localPath,
                    SilentArgs = silentArgs
                };
            }

            DialogResult = true;
        }
    }
}
