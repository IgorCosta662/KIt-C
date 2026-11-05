using System.Windows;
using System.Windows.Controls;
using KitTecnicoPro.Models;

namespace KitTecnicoPro.Views
{
    public partial class ProfileWindow : Window
    {
        public List<Profile> UpdatedProfiles { get; private set; }
        private Profile? _current;

        public ProfileWindow(List<Profile> profiles, List<AppItem> allApps)
        {
            InitializeComponent();

            this.SourceInitialized += (s, e) => 
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch {}
            };
            UpdatedProfiles = new List<Profile>(profiles);
            ProfileList.ItemsSource = UpdatedProfiles;
            if (UpdatedProfiles.Any())
                ProfileList.SelectedIndex = 0;
        }

        private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileList.SelectedItem is Profile p)
            {
                _current = p;
                TxtProfileName.Text = p.Nome;
                TxtProfileDesc.Text = p.Descricao;
                TxtApps.Text        = string.Join(Environment.NewLine, p.Apps);
            }
        }

        private void OnNewProfileClick(object sender, RoutedEventArgs e)
        {
            var p = new Profile { Nome = "Novo Perfil", Descricao = "", Icone = "👤" };
            UpdatedProfiles.Add(p);
            ProfileList.Items.Refresh();
            ProfileList.SelectedItem = p;
        }

        private void OnDeleteProfileClick(object sender, RoutedEventArgs e)
        {
            if (_current == null) return;
            if (MessageBox.Show($"Excluir perfil '{_current.Nome}'?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                UpdatedProfiles.Remove(_current);
                ProfileList.Items.Refresh();
                _current = null;
                TxtProfileName.Text = "";
                TxtProfileDesc.Text = "";
                TxtApps.Text        = "";
            }
        }

        private void OnSaveProfileClick(object sender, RoutedEventArgs e)
        {
            if (_current == null) return;
            _current.Nome     = TxtProfileName.Text.Trim();
            _current.Descricao= TxtProfileDesc.Text.Trim();
            _current.Apps     = TxtApps.Text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
            ProfileList.Items.Refresh();
            MessageBox.Show("Perfil salvo!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

