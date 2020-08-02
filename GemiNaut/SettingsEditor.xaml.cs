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

            settings.Save();


            this.Close();

        }
    }
}
