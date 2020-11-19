using GemiNaut.Properties;
using System.Windows;
using System.Windows.Controls;

namespace GemiNaut.Views
{
    /// <summary>
    /// Interaction logic for Bookmarks.xaml
    /// </summary>
    public partial class Bookmarks : Window
    {
        private readonly MainWindow _mainWindow;
        private readonly WebBrowser _webBrowser;

        public Bookmarks(MainWindow window, WebBrowser browser)
        {
            InitializeComponent();

            var settings = new Settings();
            txtBookmarks.Text = settings.Bookmarks;

            _mainWindow = window;
            _webBrowser = browser;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            var settings = new Settings();
            settings.Bookmarks = txtBookmarks.Text;
            settings.Save();

            var bmManager = new BookmarkManager(_mainWindow, _webBrowser);
            bmManager.RefreshBookmarkMenu();

            this.Close();
        }
    }
}
