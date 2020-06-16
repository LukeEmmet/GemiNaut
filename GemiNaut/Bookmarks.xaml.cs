using GemiNaut.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GemiNaut
{
    /// <summary>
    /// Interaction logic for Bookmarks.xaml
    /// </summary>
    public partial class Bookmarks : Window
    {
        MainWindow _mainWindow;

        public Bookmarks()
        {
            InitializeComponent();

            var settings = new Settings();
            txtBookmarks.Text = settings.Bookmarks;

        }

        public void  MainWindow(MainWindow window)
        {
            _mainWindow = window;
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

            _mainWindow.RefreshBookmarkMenu();

            this.Close();

        }
    }
}
