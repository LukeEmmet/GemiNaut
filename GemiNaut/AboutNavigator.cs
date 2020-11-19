using GemiNaut.Singletons;
using System.IO;
using System.Windows.Controls;
using static GemiNaut.MainWindow;

namespace GemiNaut
{
    public class AboutNavigator
    {
        private readonly MainWindow mMainWindow;
        private readonly WebBrowser mWebBrowser;

        public AboutNavigator(MainWindow window, WebBrowser browser)
        {
            mMainWindow = window;
            mWebBrowser = browser;
        }

        public void NavigateAboutScheme(System.Windows.Navigation.NavigatingCancelEventArgs e, SiteIdentity siteIdentity)
        {
            var sessionPath = Session.Instance.SessionPath;
            var appDir = System.AppDomain.CurrentDomain.BaseDirectory;

            string fullQuery;
            //just load the help file
            //no further action
            mMainWindow.ToggleContainerControlsForBrowser(true);

            var sourceFileName = e.Uri.PathAndQuery.Substring(1);      //trim off leading /

            //this expects uri has a "geminaut" domain so gmitohtml converter can proceed for now
            //I think it requires a domain for parsing...
            fullQuery = e.Uri.OriginalString;

            string hash;
            hash = HashService.GetMd5Hash(fullQuery);

            var hashFile = Path.Combine(sessionPath, hash + ".txt");
            var htmlCreateFile = Path.Combine(sessionPath, hash + ".htm");

            var finder = new ResourceFinder();
            var helpFolder = finder.LocalOrDevFolder(appDir, @"Docs", @"..\..\Docs");
            var helpFile = Path.Combine(helpFolder, sourceFileName);

            //use a specific theme so about pages look different to user theme
            var templateBaseName = Path.Combine(helpFolder, "help-theme");

            if (File.Exists(helpFile))
            {
                File.Copy(helpFile, hashFile, true);
                mMainWindow.ShowUrl(fullQuery, hashFile, htmlCreateFile, templateBaseName, siteIdentity, e);
            }
            else
            {
                mMainWindow.ToastNotify("No content was found for: " + fullQuery, ToastMessageStyles.Warning);
                e.Cancel = true;
            }
        }
    }
}
