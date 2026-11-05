using System.Windows;
using System.Windows.Controls;
using KitTecnicoPro.Models;

namespace KitTecnicoPro.Views
{
    public partial class AddIsoDialog : Window
    {
        public IsoItem? Result { get; private set; }

        public AddIsoDialog() {
            InitializeComponent();
            this.SourceInitialized += (s, e) => 
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch {}
            };

        
        }// Auto-detecta se a URL termina em .iso/.img e atualiza o checkbox
        private void OnUrlChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (ChkDirectLink == null || TxtUrl == null || TxtUrlHint == null) return;

            string url = TxtUrl.Text.Trim();
            bool isDirectExt = url.EndsWith(".iso", StringComparison.OrdinalIgnoreCase)
                            || url.EndsWith(".img", StringComparison.OrdinalIgnoreCase);

            ChkDirectLink.IsChecked = isDirectExt;
            TxtUrlHint.Text = isDirectExt
                ? "✅ Link direto detectado — botão ⬇ Baixar será exibido"
                : "💡 URL de página — apenas o botão 🌐 Site será exibido";
            TxtUrlHint.Foreground = isDirectExt
                ? new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50))
                : new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x37, 0x47, 0x4F));
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            // Validação inline (sem MessageBox)
            if (string.IsNullOrWhiteSpace(TxtNome.Text))
            {
                ShowErro("⚠ Nome é obrigatório.");
                TxtNome.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(TxtUrl.Text))
            {
                ShowErro("⚠ URL é obrigatória.");
                TxtUrl.Focus();
                return;
            }
            if (!Uri.IsWellFormedUriString(TxtUrl.Text.Trim(), UriKind.Absolute))
            {
                ShowErro("⚠ URL inválida. Use o formato https://...");
                TxtUrl.Focus();
                return;
            }

            long.TryParse(TxtTamanho.Text, out long mb);

            // Remove emoji do nome da categoria (ex: "🪟 Windows" → "Windows")
            string categoriaRaw = (CmbCategoria.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Utilitários";
            string categoria = System.Text.RegularExpressions.Regex.Replace(categoriaRaw, @"[^\w\s\u00C0-\u024F]", "").Trim();

            string arq      = (CmbArq.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "x64";
            bool isDirect   = ChkDirectLink.IsChecked == true;

            Result = new IsoItem
            {
                Nome         = TxtNome.Text.Trim(),
                Versao       = TxtVersao.Text.Trim(),
                Arq          = arq,
                Categoria    = categoria,
                Url          = TxtUrl.Text.Trim(),
                IsDirectLink = isDirect,
                TamanhoMB    = mb,
                Descricao    = TxtDescricao.Text.Trim()
            };

            DialogResult = true;
        }

        private void ShowErro(string msg)
        {
            TxtErro.Text = msg;
            TxtErro.Visibility = Visibility.Visible;
        }
    }
}


