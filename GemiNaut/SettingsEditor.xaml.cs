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
            MaxDownloadSize.Text = settings.MaxDownloadSize;
            MaxDownloadTime.Text = settings.MaxDownloadTime.ToString();


        }


        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {

            this.Close();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            var settings = new Settings();
            settings.HomeUrl = txtUrl.Text;
            settings.MaxDownloadSize = MaxDownloadSize.Text;
            settings.MaxDownloadTime = int.Parse(MaxDownloadTime.Text);

            settings.Save();


            this.Close();

        }
    }
}
