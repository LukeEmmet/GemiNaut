using System;
using System.Windows;
using System.Windows.Input;
using GemiNaut.Serialization.Commandline;
using System.Collections.Generic;
using System.IO;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using ToastNotifications.Messages;
using System.Diagnostics;
using GemiNaut.Singletons;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using GemiNaut.Properties;
using mshtml;

namespace GemiNaut
{
    public partial class MainWindow : Window

    {
        private Dictionary<string, string> _urlsByHash;
        private Notifier _notifier;
        private BookmarkManager _bookmarkManager;


        public MainWindow()
        {
            InitializeComponent();

            _notifier = new Notifier(cfg =>
            {
                //place the notifications approximately inside the main editing area
                //(not over the toolbar area) on the top-right hand side
                cfg.PositionProvider = new WindowPositionProvider(
                    parentWindow: Application.Current.MainWindow,
                    corner: Corner.TopRight,
                    offsetX: 15,
                    offsetY: 90);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(5),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });

            _urlsByHash = new Dictionary<string, string>();
            _bookmarkManager = new BookmarkManager(this, BrowserControl);

            AppInit.UpgradeSettings();
            AppInit.CopyAssets();

            _bookmarkManager.RefreshBookmarkMenu();

            var settings = new Settings();

            BrowserControl.Navigate(settings.HomeUrl);

            BuildThemeMenu();
            TickSelectedThemeMenu();
        }




        private void txtUrl_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)

                if (UriTester.TextIsUri(txtUrl.Text))
                {
                    BrowserControl.Navigate(txtUrl.Text);

                } else
                {
                    ToastNotify("Not a valid URI: " + txtUrl.Text, ToastMessageStyles.Error);
                }
        }



        public void ToggleContainerControlsForBrowser(bool toState)
        {

            //we need to turn off other elements so focus doesnt move elsewhere
            //in that case the keyboard events go elsewhere and you have to click 
            //into the browser to get it to work again
            //see https://stackoverflow.com/questions/8495857/webbrowser-steals-focus

            TopDock.IsEnabled = toState;
            //DockMenu.IsEnabled = toState;     //not necessary as it is not a container for the webbrowser
            DockLower.IsEnabled = toState;
            GridMain.IsEnabled = toState;
        }



        private void BrowserControl_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {


            ////doc might be null - you need to check when using!
            //var doc = (HTMLDocument)BrowserControl.Document;
            ////this is how we could detect a click on a link to an image...
            //if (doc?.activeElement != null) {ToastNotify(doc.activeElement.outerHTML); }

            var normalisedUri = UriTester.NormaliseUri(e.Uri);

            var siteIdentity = new SiteIdentity(normalisedUri, Session.Instance);

            var fullQuery = normalisedUri.OriginalString;

            //sanity check we have a valid URL syntax at least
            if (e.Uri.Scheme == null)
            {
                ToastNotify("Invalid URL: " + normalisedUri.OriginalString, ToastMessageStyles.Error);
                e.Cancel = true;
            }


            ToggleContainerControlsForBrowser(false);

            //these are the only ones we "navigate" to. We do this by downloading the GMI content
            //converting to HTML and then actually navigating to that.
            if (normalisedUri.Scheme == "gemini")
            {
                var geminiNavigator = new GeminiNavigator(this, this.BrowserControl);
                geminiNavigator.NavigateGeminiScheme(fullQuery, e, siteIdentity);
            }

            else if (normalisedUri.Scheme == "gopher")
            {
                var gopherNavigator = new GopherNavigator(this, this.BrowserControl);
                gopherNavigator.NavigateGopherScheme(fullQuery, e, siteIdentity);
            }

            else if (normalisedUri.Scheme == "about")
            {
                var aboutNavigator = new AboutNavigator(this, this.BrowserControl);
                aboutNavigator.NavigateAboutScheme(e, siteIdentity);

            }
            else if (normalisedUri.Scheme == "file")
            {
                //just load the converted html file
                //no further action.
            }
            else
            {
                //we don't care about any other protocols
                //so we open those in system web browser to deal with
                var launcher = new ExternalNavigator(this);
                launcher.LaunchExternalUri(e.Uri.ToString());
                ToggleContainerControlsForBrowser(true);
                e.Cancel = true;
            }
        }


        public void ShowImage(string sourceUrl, string imgFile, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            string hash;

            hash = HashService.GetMd5Hash(sourceUrl);
            

            _urlsByHash[hash] = sourceUrl;

            //no further navigation right now
            e.Cancel = true;

            //instead tell the browser to load the content
            BrowserControl.Navigate(@"file:///" + imgFile);


        }
        public void ShowUrl(string sourceUrl, string gmiFile, string htmlFile, string themePath, SiteIdentity siteIdentity, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {

            string hash;

            hash = HashService.GetMd5Hash(sourceUrl);
            

            //create the html file
            var result = GmiToHtml(gmiFile, htmlFile, sourceUrl, siteIdentity, themePath);

            if (!File.Exists(htmlFile))
            {
                ToastNotify("GMIToHTML did not create content for " + sourceUrl + "\n\n" + "File: " + gmiFile, ToastMessageStyles.Error);

                ToggleContainerControlsForBrowser(true);
                e.Cancel = true;
            }
            else
            {

                _urlsByHash[hash] = sourceUrl;

                //no further navigation right now
                e.Cancel = true;

                //instead tell the browser to load the content
                BrowserControl.Navigate(@"file:///" + htmlFile);
            }

        }



        //convert GMI to HTML for display and save to outpath
        public Tuple<int, string, string> GmiToHtml (string gmiPath, string outPath, string uri, SiteIdentity siteIdentity, string theme)
        {
            var appDir = System.AppDomain.CurrentDomain.BaseDirectory;
            var finder = new ResourceFinder();

            //allow for rebol and converters to be in sub folder of exe (e.g. when deployed)
            //otherwise we use the development ones which are version controlled
            var rebolPath = finder.LocalOrDevFile(appDir, @"Rebol", @"..\..\Rebol", "r3-core.exe");
            var scriptPath = finder.LocalOrDevFile(appDir, @"GmiConverters", @"..\..\GmiConverters", "GmiToHtml.r3");

            var identiconUri = new System.Uri(siteIdentity.IdenticonImagePath());
            var fabricUri = new System.Uri(siteIdentity.FabricImagePath());

            //due to bug in rebol 3 at the time of writing (mid 2020) there is a known bug in rebol 3 in 
            //working with command line parameters, so we need to escape quotes
            //see https://stackoverflow.com/questions/6721636/passing-quoted-arguments-to-a-rebol-3-script
            //also hypens are also problematic, so we base64 each param and unpack in the script
            var command = String.Format("\"{0}\" -cs \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\" \"{6}\" \"{7}\" \"{8}\" ",
                rebolPath, 
                scriptPath,
                Base64Service.Base64Encode(gmiPath),
                Base64Service.Base64Encode(outPath),
                Base64Service.Base64Encode(uri),
                Base64Service.Base64Encode(theme),
                Base64Service.Base64Encode(identiconUri.AbsoluteUri),
                Base64Service.Base64Encode(fabricUri.AbsoluteUri),
                Base64Service.Base64Encode(siteIdentity.GetSiteId())

                );

            var execProcess = new ExecuteProcess();

            var result = execProcess.ExecuteCommand(command);

            return (result);
        }


        public enum ToastMessageStyles
        {
            Information, Warning, Error,
            Success
        }

        public void ToastNotify(string message, ToastMessageStyles style)
        {
            try
            {
                if (style == ToastMessageStyles.Information) { _notifier.ShowInformation(message); }
                if (style == ToastMessageStyles.Error) { _notifier.ShowError(message); }
                if (style == ToastMessageStyles.Success) { _notifier.ShowSuccess(message); }
                if (style == ToastMessageStyles.Warning) { _notifier.ShowWarning(message); }

            }
            catch
            {
                //for example main window might not be visible yet, so just ignore those.
            }
        }

        //simple overload method without style
        public void ToastNotify(string message)
        {
            ToastNotify(message, ToastMessageStyles.Information);
        }


        

        private void BrowseBack_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ((BrowserControl != null) && (BrowserControl.CanGoBack));
        }

        private void BrowseBack_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            BrowserControl.GoBack();
        }

        private void BrowseHome_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (BrowserControl != null);
        }

        private void BrowseHome_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var settings = new Settings();
            BrowserControl.Navigate(settings.HomeUrl);
        }

        private void BrowseForward_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ((BrowserControl != null) && (BrowserControl.CanGoForward));
        }

        private void BrowseForward_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            BrowserControl.GoForward();
        }

        private void GoToPage_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void GoToPage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (UriTester.TextIsUri(txtUrl.Text))
            {
                BrowserControl.Navigate(txtUrl.Text);
            }
            else
            {
                ToastNotify("Not a valid URI: " + txtUrl.Text, ToastMessageStyles.Error);
            }
        }

        
        private void MenuFileExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuHelpAbout_Click(object sender, RoutedEventArgs e)
        {

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;

            var message = "GemiNaut v" + version + ", copyright Luke Emmet 2020\n";

            ToastNotify(message, ToastMessageStyles.Information);
        }

        private void MenuHelpContents_Click(object sender, RoutedEventArgs e)
        {

            //this will be a look up into docs folder
            BrowserControl.Navigate("about://geminaut/help.gmi");

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //shutdown and dispose of session singleton
            Session.Instance.Dispose();
            Application.Current.Shutdown();

        }

        private void MenuViewSource_Click(object sender, RoutedEventArgs e)
        {
            var menu = (MenuItem)sender;
            string hash;

            //use the current session folder
            var sessionPath = Session.Instance.SessionPath;

            hash = HashService.GetMd5Hash(txtUrl.Text);
            

            //uses .txt as extension so content loaded as text/plain not interpreted by the browser
            var gmiFile = sessionPath + "\\" + hash + ".txt";

             BrowserControl.Navigate(gmiFile);


        }

        private void MenuViewSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsEditor = new SettingsEditor();
            settingsEditor.Owner = this;
            settingsEditor.ShowDialog(); 


        }

        //after any navigate (even back/forwards) 
        //update the address box depending on the current location
        private void BrowserControl_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            var uri = e.Uri.ToString();

            //look up the URL that this HTML page shows
            var regex = new Regex(@".*/([a-f0-9]+)\.(.*)");
            if (regex.IsMatch(uri)) {

                var match = regex.Match(uri);
                var hash = match.Groups[1].ToString();

                string geminiUrl = _urlsByHash[hash];
                if (geminiUrl != null) {

                    //now show the actual gemini URL in the address bar
                    txtUrl.Text = geminiUrl;

                    ShowTitle(BrowserControl.Document);
                }
                
             //if a text file (i.e. view->source), explicitly set the charset
             //to UTF-8 so ascii art looks correct etc.
              if ("txt" == match.Groups[2].ToString().ToLower())
                {
                    //set text files (GMI source) to be UTF-8 for now
                    var doc = (HTMLDocument)BrowserControl.Document;
                    doc.charset = "UTF-8";

                }

            }

            BrowserControl.Focus();
            ((HTMLDocument)BrowserControl.Document).focus();



        }

        private void ShowTitle(dynamic document)
        {
            //update title, this might fail when called from Navigated as the document might not be ready yet
            //but we also call on LoadCompleted. This should catch both situations
            //of real navigation and also back and forth in history
            try
            {
                Application.Current.MainWindow.Title = document.Title + " - GemiNaut, a friendly GUI browser";
            }
            catch (Exception e)
            {
                ToastNotify(e.Message, ToastMessageStyles.Error);
            }
        }

        private void BuildThemeMenu()
        {
            var appDir = System.AppDomain.CurrentDomain.BaseDirectory;
            var finder = new ResourceFinder();

            var themeFolder = finder.LocalOrDevFolder(appDir, @"GmiConverters\themes", @"..\..\GmiConverters\themes");


            foreach (var file in Directory.EnumerateFiles(themeFolder, "*.htm")) {

                var newMenu = new MenuItem();
                newMenu.Header = Path.GetFileNameWithoutExtension(Path.Combine(themeFolder, file));
                newMenu.Click += ViewThemeItem_Click;

                mnuTheme.Items.Add(newMenu);
            }

        }

        private void TickSelectedThemeMenu()
        {
            var settings = new Settings();

            foreach (MenuItem themeMenu in mnuTheme.Items)
            {
                themeMenu.IsChecked = (themeMenu.Header.ToString() == settings.Theme);
   
            }
            
        }

        private void ViewThemeItem_Click(object sender, RoutedEventArgs e)
        {
            var menu = (MenuItem)sender;
            var themeName = menu.Header.ToString();

            var settings = new Settings();
            if (settings.Theme != themeName)
            {
                settings.Theme = themeName;
                settings.Save();

                TickSelectedThemeMenu();

                //redisplay
                BrowserControl.Navigate(txtUrl.Text);
            }

        }

        private void BrowserControl_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            var doc = (HTMLDocument)BrowserControl.Document;

            ShowTitle(doc);


            //we need to turn on/off other elements so focus doesnt move elsewhere
            //in that case the keyboard events go elsewhere and you have to click 
            //into the browser to get it to work again
            //see https://stackoverflow.com/questions/8495857/webbrowser-steals-focus
            ToggleContainerControlsForBrowser(true);
        }

        private void mnuMenuBookmarksAdd_Click(object sender, RoutedEventArgs e)
        {
            var bmManager = new BookmarkManager(this, BrowserControl);
            bmManager.AddBookmark(txtUrl.Text, ((HTMLDocument)BrowserControl.Document).title);
            var url = txtUrl.Text;

        }

        private void mnuMenuBookmarksEdit_Click(object sender, RoutedEventArgs e)
        {
            var settings = new Settings();

            Bookmarks winBookmarks = new Bookmarks(this, BrowserControl);

            //show modally
            winBookmarks.Owner = this;
            winBookmarks.ShowDialog();

        }

        public void mnuMenuBookmarksGo_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;

            BrowserControl.Navigate(menuItem.CommandParameter.ToString());

        }



    }
}
