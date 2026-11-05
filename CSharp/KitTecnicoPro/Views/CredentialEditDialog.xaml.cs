using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using KitTecnicoPro.Models;
using KitTecnicoPro.Services;

namespace KitTecnicoPro.Views
{
    public partial class CredentialEditDialog : Window
    {
        public CredentialEntry? Result { get; private set; }
        private readonly CredentialEntry? _existing;
        private bool _showingPassword;

        public CredentialEditDialog(CredentialEntry? existing)
        {
            InitializeComponent();

            this.SourceInitialized += (s, e) =>
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch { }
            };
            _existing = existing;

            if (existing != null)
            {
                TxtTitle.Text = "✏️ Editar Credencial";
                for (int i = 0; i < CmbCat.Items.Count; i++)
                {
                    if ((CmbCat.Items[i] as ComboBoxItem)?.Content?.ToString() == existing.Categoria)
                    { CmbCat.SelectedIndex = i; break; }
                }
                if (CmbCat.SelectedIndex < 0) CmbCat.SelectedIndex = 5;
                TxtTitulo.Text    = existing.Titulo;
                TxtSubTitulo.Text = existing.SubTitulo;
                TxtUsuario.Text   = existing.Usuario;
                string dec        = VaultService.Decrypt(existing.SenhaEnc);
                PwdSenha.Password = dec;
                TxtSenhaVisivel.Text = dec;
                TxtNotas.Text     = existing.Notas;
            }
            else
            {
                CmbCat.SelectedIndex = 0;
            }
        }

        private string GetCurrentPassword()
            => _showingPassword ? TxtSenhaVisivel.Text : PwdSenha.Password;

        private void OnToggleMostrarSenha(object sender, RoutedEventArgs e)
        {
            if (!_showingPassword)
            {
                TxtSenhaVisivel.Text = PwdSenha.Password;
                PwdSenha.Visibility = Visibility.Collapsed;
                TxtSenhaVisivel.Visibility = Visibility.Visible;
                _showingPassword = true;
            }
            else
            {
                PwdSenha.Password = TxtSenhaVisivel.Text;
                TxtSenhaVisivel.Visibility = Visibility.Collapsed;
                PwdSenha.Visibility = Visibility.Visible;
                _showingPassword = false;
            }
        }

        private void OnSalvarClick(object sender, RoutedEventArgs e)
        {
            string titulo = TxtTitulo.Text.Trim();
            if (string.IsNullOrEmpty(titulo))
            { TxtErro.Text = "⚠ Título é obrigatório."; return; }

            string cat = (CmbCat.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "📝 Nota";
            string rawPassword = GetCurrentPassword();

            Result = new CredentialEntry
            {
                Id        = _existing?.Id ?? Guid.NewGuid(),
                Titulo    = titulo,
                Categoria = cat,
                SubTitulo = TxtSubTitulo.Text.Trim(),
                Usuario   = TxtUsuario.Text.Trim(),
                SenhaEnc  = string.IsNullOrEmpty(rawPassword)
                                ? ""
                                : VaultService.Encrypt(rawPassword),
                Notas     = TxtNotas.Text.Trim(),
                Criado    = _existing?.Criado ?? DateTime.Now
            };

            DialogResult = true;
        }

        private void OnGerarSenha(object sender, RoutedEventArgs e)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%&*";
            var bytes = RandomNumberGenerator.GetBytes(16);
            string pwd = new(bytes.Select(b => chars[b % chars.Length]).ToArray());
            PwdSenha.Password = pwd;
            TxtSenhaVisivel.Text = pwd;
        }
    }
}
