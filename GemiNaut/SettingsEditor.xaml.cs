using GemiNaut.Properties;
using System.Windows;
using System.Windows.Controls;

namespace GemiNaut
{
    /// <summary>
    /// Interaction logic for SettingsEditor.xaml
    /// </summary>
    public partial class SettingsEditor : Window
    {
        public SettingsEditor()
        {
            InitializeComponent();

            var settings = new Settings();
            txtUrl.Text = settings.HomeUrl;
            MaxDownloadSize.Text = settings.MaxDownloadSizeMb.ToString();
            MaxDownloadTime.Text = settings.MaxDownloadTimeSeconds.ToString();

            HttpSchemeProxy.Text = settings.HttpSchemeProxy;
            HandleWebLinks.Text = settings.HandleWebLinks;

            ShowProxyWidget(HandleWebLinks.Text);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            var settings = new Settings();
            settings.HomeUrl = txtUrl.Text;
            settings.MaxDownloadSizeMb = int.Parse(MaxDownloadSize.Text);
            settings.MaxDownloadTimeSeconds = int.Parse(MaxDownloadTime.Text);

            settings.HttpSchemeProxy = HttpSchemeProxy.Text;
            settings.HandleWebLinks = HandleWebLinks.Text;

            settings.Save();

            this.Close();
        }

        private void ShowProxyWidget(string value)
        {
            HttpSchemeProxy.Visibility = (value == "Gemini HTTP proxy") ? Visibility.Visible : Visibility.Collapsed;
            HttpSchemeProxyLabel.Visibility = HttpSchemeProxy.Visibility;
        }
        private void HandleWebLinks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //string text = ((ComboBox)sender).SelectedValue.ToString();
            // string text = (sender as ComboBox).SelectedItem as string;
            string text = (e.AddedItems[0] as ComboBoxItem).Content as string;
            ShowProxyWidget(text);
        }
    }
}
