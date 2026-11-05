using System.Windows;
using System.Windows.Controls;
using KitTecnicoPro.Models;
using KitTecnicoPro.Services;

namespace KitTecnicoPro.Views
{
    public partial class CredentialVaultWindow : Window
    {
        private List<CredentialEntry> _all = new();
        private string _busca = "";
        private string _cat   = "Todas";

        public CredentialVaultWindow()
        {
            InitializeComponent();

            this.SourceInitialized += (s, e) => 
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch {}
            };
            Load();
        }

        private void Load()
        {
            _all = VaultService.Load();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (LstCredenciais == null) return;
            var view = _all.AsEnumerable();

            if (!string.IsNullOrEmpty(_busca))
                view = view.Where(c => c.Titulo.Contains(_busca, StringComparison.OrdinalIgnoreCase)
                                    || c.Usuario.Contains(_busca, StringComparison.OrdinalIgnoreCase)
                                    || c.SubTitulo.Contains(_busca, StringComparison.OrdinalIgnoreCase)
                                    || c.Notas.Contains(_busca, StringComparison.OrdinalIgnoreCase));

            if (_cat != "Todas")
                view = view.Where(c => c.Categoria == _cat);

            var list = view.OrderBy(c => c.Categoria).ThenBy(c => c.Titulo).ToList();
            LstCredenciais.ItemsSource = list;
            TxtStatus.Text = $"🔒 {_all.Count} entrada(s) armazenada(s) — criptografadas com AES-256";
        }

        private void OnBuscaChanged(object s, TextChangedEventArgs e)
        { _busca = TxtBusca.Text.Trim(); ApplyFilter(); }

        private void OnCatChanged(object s, SelectionChangedEventArgs e)
        {
            var txt = (CmbCategoria.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todas";
            _cat = txt == "Todas" ? "Todas" : txt;
            ApplyFilter();
        }

        private void OnRefreshClick(object s, RoutedEventArgs e) => Load();

        private void OnNovaClick(object sender, RoutedEventArgs e) => OpenDialog(null);
        private void OnEditClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is CredentialEntry entry) OpenDialog(entry);
        }

        private void OpenDialog(CredentialEntry? existing)
        {
            var dlg = new CredentialEditDialog(existing) { Owner = this };
            if (dlg.ShowDialog() != true) return;

            if (existing != null)
            {
                int idx = _all.FindIndex(c => c.Id == existing.Id);
                if (idx >= 0) _all[idx] = dlg.Result!;
            }
            else { _all.Add(dlg.Result!); }

            VaultService.Save(_all);
            ApplyFilter();
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not CredentialEntry entry) return;
            if (MessageBox.Show($"Remover '{entry.Titulo}'?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            _all.RemoveAll(c => c.Id == entry.Id);
            VaultService.Save(_all);
            ApplyFilter();
            TxtStatus.Text = $"✅ '{entry.Titulo}' removido.";
        }

        private void OnSelectionChanged(object s, SelectionChangedEventArgs e) { }

        private void OnToggleSenha(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not CredentialEntry entry) return;
            string plain = VaultService.Decrypt(entry.SenhaEnc);
            MessageBox.Show($"Senha de '{entry.Titulo}':\n\n{plain}",
                "🔑 Senha", MessageBoxButton.OK, MessageBoxImage.None);
        }

        private void OnCopySenha(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not CredentialEntry entry) return;
            string plain = VaultService.Decrypt(entry.SenhaEnc);
            Clipboard.SetText(plain);
            TxtStatus.Text = $"✅ Senha de '{entry.Titulo}' copiada. Limpe o clipboard após usar.";
        }
    }
}

