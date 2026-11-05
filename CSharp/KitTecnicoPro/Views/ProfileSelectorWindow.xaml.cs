using System.Windows;
using KitTecnicoPro.Models;

namespace KitTecnicoPro.Views
{
    public partial class ProfileSelectorWindow : Window
    {
        public Profile? SelectedProfile { get; private set; }

        public ProfileSelectorWindow(List<Profile> profiles)
        {
            InitializeComponent();

            this.SourceInitialized += (s, e) => 
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch {}
            };
            ProfileList.ItemsSource = profiles;
            if (profiles.Any()) ProfileList.SelectedIndex = 0;
        }

        private void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            if (ProfileList.SelectedItem is not Profile profile)
            {
                MessageBox.Show("Selecione um perfil.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SelectedProfile = profile;
            DialogResult = true;
        }
    }
}

