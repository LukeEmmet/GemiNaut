using System;
using System.Windows;
using System.Windows.Input;
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
using GemiNaut.Properties;
using mshtml;
using System.Windows.Navigation;
using Microsoft.Win32;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualBasic;
using System.Security.Cryptography;
using System.Linq;

namespace GemiNaut.Views
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, string> _urlsByHash;
        private readonly Notifier _notifier;
        private readonly BookmarkManager _bookmarkManager;
        private bool _isNavigating;

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

            var settings = new UserSettings();
            var launchUri = settings.HomeUrl;

            string[] args = App.Args;
            if (args != null)
            {
                launchUri = App.Args[0];
            }

            BrowserControl.Navigate(launchUri);

            BuildThemeMenu();
            TickSelectedThemeMenu();
        }

        private void TxtUrl_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
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

        private void BrowserControl_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            _isNavigating = true;

            var normalisedUri = UriTester.NormaliseUri(e.Uri);

            var siteIdentity = new SiteIdentity(normalisedUri, Session.Instance);

            var fullQuery = normalisedUri.OriginalString;

            //sanity check we have a valid URL syntax at least
            if (e.Uri.Scheme == null)
            {
                ToastNotify("Invalid URL: " + normalisedUri.OriginalString, ToastMessageStyles.Error);
                e.Cancel = true;
            }

            var settings = new UserSettings();

            ToggleContainerControlsForBrowser(false);

            //these are the only ones we "navigate" to. We do this by downloading the GMI content
            //converting to HTML and then actually navigating to that.
            if (normalisedUri.Scheme == "gemini")
            {
                var geminiNavigator = new GeminiNavigator(this, this.BrowserControl);
                geminiNavigator.NavigateGeminiScheme(fullQuery, e, siteIdentity);
            }
            else if (normalisedUri.Scheme == "nimigem")
            {
                var nimigemNavigator = new NimigemNavigator(this, this.BrowserControl);

                var document = (HTMLDocument)BrowserControl.Document;

                var firstTextarea = (IHTMLTextAreaElement)document.getElementsByTagName("textarea").item(0);

                string payload;
                if (firstTextarea != null)
                {
                    payload = firstTextarea.value;
                } else
                {
                    payload = "";
                }

                nimigemNavigator.NavigateNimigemScheme(fullQuery, e, payload);
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
            else if (normalisedUri.Scheme.StartsWith("http"))       //both http and https
            {
                var linkId = "";
                ////doc might be null - you need to check when using!
                var doc = (HTMLDocument)BrowserControl.Document;
                ////this is how we could detect a click on a link to an image...
                if (doc?.activeElement != null)
                {
                    linkId = doc.activeElement.id;
                }

                //detect ctrl click
                if (
                     Keyboard.IsKeyDown(Key.LeftCtrl) ||
                     Keyboard.IsKeyDown(Key.RightCtrl) ||
                     settings.HandleWebLinks == "System web browser" ||
                     linkId == "web-launch-external"
                    )
                {
                    //open in system web browser
                    var launcher = new ExternalNavigator(this);
                    launcher.LaunchExternalUri(e.Uri.ToString());
                    ToggleContainerControlsForBrowser(true);
                    e.Cancel = true;
                }
                else if (settings.HandleWebLinks == "Gemini HTTP proxy")
                {
                    // use a gemini proxy for http links
                    var geminiNavigator = new GeminiNavigator(this, this.BrowserControl);
                    geminiNavigator.NavigateGeminiScheme(fullQuery, e, siteIdentity);
                }
                else
                {
                    //use internal navigator
                    var httpNavigator = new HttpNavigator(this, this.BrowserControl);
                    httpNavigator.NavigateHttpScheme(fullQuery, e, siteIdentity, linkId);
                }
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

            if (e.Cancel)
            {
                _isNavigating = false;
            }
        }

        public void ShowImage(string sourceUrl, string imgFile, NavigatingCancelEventArgs e)
        {
            var hash = HashService.GetMd5Hash(sourceUrl);

            _urlsByHash[hash] = sourceUrl;

            //no further navigation right now
            e.Cancel = true;

            //instead tell the browser to load the content
            BrowserControl.Navigate(@"file:///" + imgFile);
        }

        public void ShowUrl(string sourceUrl, string gmiFile, string htmlFile, string themePath, SiteIdentity siteIdentity, NavigatingCancelEventArgs e)
        {
            var hash = HashService.GetMd5Hash(sourceUrl);

            var usedShowWebHeaderInfo = false;

            var settings = new UserSettings();
            var uri = new UriBuilder(sourceUrl);

            //only show web header for self generated content, not proxied
            usedShowWebHeaderInfo = uri.Scheme.StartsWith("http") && settings.HandleWebLinks != "Gemini HTTP proxy";

            //create the html file
            ConverterService.CreateDirectoriesIfNeeded(gmiFile, htmlFile, themePath);
            var result = ConverterService.GmiToHtml(gmiFile, htmlFile, sourceUrl, siteIdentity, themePath, usedShowWebHeaderInfo);

            if (!File.Exists(htmlFile))
            {
                ToastNotify("GMIToHTML did not create content for " + sourceUrl + "\n\nFile: " + gmiFile, ToastMessageStyles.Error);

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
            var settings = new UserSettings();
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
            e.CanExecute = !_isNavigating;
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
        private void BrowserControl_Navigated(object sender, NavigationEventArgs e)
        {
            var uri = e.Uri.ToString();

            //look up the URL that this HTML page shows
            var regex = new Regex(@".*/([a-f0-9]+)\.(.*)");
            if (regex.IsMatch(uri))
            {
                var match = regex.Match(uri);
                var hash = match.Groups[1].ToString();

                string geminiUrl = _urlsByHash[hash];
                if (geminiUrl != null)
                {
                    //now show the actual gemini URL in the address bar
                    txtUrl.Text = geminiUrl;

                    ShowTitle(BrowserControl.Document);


                    var originalUri = new UriBuilder(geminiUrl);

                    if (originalUri.Scheme == "http" || originalUri.Scheme == "https")
                    {
                        ShowLinkRenderMode(BrowserControl.Document);
                    }

                     BuildCertsMenu();
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

            _isNavigating = false;
        }

        //decorate links that switch the mode so the current mode is highlighted
        private static void ShowLinkRenderMode(dynamic document)
        {
            var doc = (HTMLDocument)document;
            var settings = new UserSettings();

            //decrate the current mode
            if (doc.getElementById(settings.WebRenderMode) != null)
            {
                var modeLink = doc.getElementById(settings.WebRenderMode);
                modeLink.innerText = "[" + modeLink.innerText + "]";
                modeLink.style.fontWeight = "bold";
            }
        }

        private void ShowTitle(dynamic document)
        {
            //update title, this might fail when called from Navigated as the document might not be ready yet
            //but we also call on LoadCompleted. This should catch both situations
            //of real navigation and also back and forth in history

            const string geminiTitle = "GemiNaut, a friendly GUI browser";

            try
            {
                Application.Current.MainWindow.Title = document == null ? geminiTitle : document.Title + " - " + geminiTitle;
            }
            catch (Exception e)
            {
                ToastNotify(e.Message, ToastMessageStyles.Error);
            }
        }

        private void BuildThemeMenu()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;

            var themeFolder = ResourceFinder.LocalOrDevFolder(appDir, @"GmiConverters\themes", @"..\..\..\GmiConverters\themes");

            foreach (var file in Directory.EnumerateFiles(themeFolder, "*.htm"))
            {
                var newMenu = new MenuItem();
                newMenu.Header = Path.GetFileNameWithoutExtension(Path.Combine(themeFolder, file));
                newMenu.Click += ViewThemeItem_Click;

                mnuTheme.Items.Add(newMenu);
            }
        }

        private void BuildCertsMenu()
        {
            mnuCerts.Items.Clear();

            Uri uri = new Uri(txtUrl.Text);
            var domain = uri.Host;


            foreach (var hashPair in Session.Instance.CertificatesManager.Certificates)
            { 
                var newMenu = new MenuItem();

                var displayName = hashPair.Value.Subject;
                if (displayName.Substring(0,3).ToUpper() == "CN=")
                {
                    displayName = displayName.Substring(3);     //trim leading "CN=
                }

                var maxLen = 30;

                if (displayName.Length > maxLen)
                {
                    displayName = displayName.Substring(0, maxLen - 1) + "…";
                }

                displayName = displayName.PadRight(maxLen);

                newMenu.Header = displayName + "\t" + hashPair.Value.Thumbprint;
                newMenu.Tag = hashPair.Value.Thumbprint + ":" + domain;
                newMenu.FontFamily = new System.Windows.Media.FontFamily("Consolas");

                if (Session.Instance.CertificatesManager.Mappings.ContainsKey(domain))
                {
                    newMenu.IsChecked = (Session.Instance.CertificatesManager.Mappings[domain] == hashPair.Value.Thumbprint);
                } else
                {
                    newMenu.IsChecked = false;
                }
                newMenu.Click += ToggleCertItem_Click;
                mnuCerts.Items.Add(newMenu);
            }

            //show separator if there were some loaded.
            if (mnuCerts.Items.Count > 0) { mnuCerts.Items.Add(new Separator()); }
           
            var CreateCertMenu = new MenuItem();
            CreateCertMenu.Header = "_Create certificate...";
            CreateCertMenu.Click += CreateCert_Click;
            mnuCerts.Items.Add(CreateCertMenu);

        }

        public static string CleanFileName(string filename)
        {
            var target = filename;
            string regexSearch = new string(Path.GetInvalidFileNameChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            target = r.Replace(target, ""); 
            
            return target;
        }

        private void CreateCert_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            DateTime validFrom = DateTime.Today.AddDays(-1);         //from yesterday, in case server we might want to use it on is still within the previous calendar day
            DateTime validTo = new DateTime(9999, 12, 31);          //dont ever expire.

            var windowCentre = WindowGeometry.WindowCentre(this);
            var inputPrompt = "Name of this identity that will be visible to servers as the certificate 'Common Name'. For example you can use your name/username. " +
                "\n\nDo not include any sensitive information, such as a password.";

            string commonName = Interaction.InputBox(inputPrompt, "Name of the identity/certificate", "", windowCentre.Item1, windowCentre.Item2);
            
            if (commonName == "")
            {
                //user pressed cancel or left it empty
                ToastNotify("Identity certificate creation abandoned - identity name was not provided", ToastMessageStyles.Warning);
                return;
            }

            var cert = CertificateCreator.GenSelfSignedCert(commonName, validFrom, validTo);
            var fileBase = CleanFileName(commonName).Trim();
            if (fileBase.Length > 20)
            {
                fileBase = fileBase.Substring(0, 20);
            }

            //make a filename based on the common name and thumbprint
            fileBase = fileBase + "_" + cert.Thumbprint.Substring(0, 6) + ".pfx";    //Foo_ABC1.pfx, with forbidden chars removed

            var settings = new UserSettings();

            var certBytes = cert.Export(X509ContentType.Pfx);       //save with no password

            var certFile = Path.Combine(settings.ClientCertificatesFolder, fileBase);

            using (FileStream fileStream = new FileStream(certFile, FileMode.Create))
            {

                for (int i = 0; i < certBytes.Length; i++)
                {
                    fileStream.WriteByte(certBytes[i]);
                }

                fileStream.Seek(0, SeekOrigin.Begin);
                fileStream.Close();
            }
            ToastNotify("Certificate created and saved in user profile", ToastMessageStyles.Success);


            //load the cert
            var certManager = Session.Instance.CertificatesManager;
            
            certManager.AddCertificate(cert);
            
            

            ToastNotify("Certificate loaded - select it on in the menu to use it on any site");
            
            BuildCertsMenu();
        }


        private void ToggleCertItem_Click(object sender, RoutedEventArgs e)
        {
            var menuInfo = ((MenuItem)sender).Tag.ToString().Split(':');

            var certsManager = Session.Instance.CertificatesManager;
            var mappings = certsManager.Mappings;

            //dont try to associate certificate for non gemini/nimigem sites
            var uri = new Uri(txtUrl.Text);
            if (uri.Scheme != "gemini" && uri.Scheme != "nimigem")
            {
                ToastNotify("Cannot use client certificates for " + uri.Scheme + " sites.", ToastMessageStyles.Warning);
                return;
            }

            if (mappings.ContainsKey(menuInfo[1]))
            {
                if (mappings[menuInfo[1]] == menuInfo[0])
                {
                    certsManager.UnRegisterMapping(menuInfo[1]);
                    ToastNotify("Certificate will no longer be used on " + menuInfo[1], ToastMessageStyles.Information);
                }
                else
                {
                    certsManager.RegisterMapping(menuInfo[1], menuInfo[0]);
                    ToastNotify("Certificate changed for " + menuInfo[1], ToastMessageStyles.Information);
                }
            } else
            {
                certsManager.RegisterMapping(menuInfo[1], menuInfo[0]);
                ToastNotify("Certificate will now be used on " + menuInfo[1], ToastMessageStyles.Information);
            }

            BuildCertsMenu();
        }

        private void TickSelectedThemeMenu()
        {
            var settings = new UserSettings();

            foreach (MenuItem themeMenu in mnuTheme.Items)
            {
                themeMenu.IsChecked = (themeMenu.Header.ToString() == settings.Theme);
            }
        }

        private void ViewThemeItem_Click(object sender, RoutedEventArgs e)
        {
            var menu = (MenuItem)sender;
            var themeName = menu.Header.ToString();

            var settings = new UserSettings();
            if (settings.Theme != themeName)
            {
                settings.Theme = themeName;
                settings.Save();

                TickSelectedThemeMenu();

                //redisplay
                BrowserControl.Navigate(txtUrl.Text);
            }
        }

        private void BrowserControl_LoadCompleted(object sender, NavigationEventArgs e)
        {
            var doc = ((WebBrowser)sender).Document;

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

        private void MenuFileNew_Click(object sender, RoutedEventArgs e)
        {
            //start a completely new GemiNaut session, with the current URL
            var location = System.Reflection.Assembly.GetEntryAssembly().Location;

            var info = new ProcessStartInfo()
            {
                FileName = "dotnet",
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(location),
                ArgumentList = { Path.GetFileName(location), txtUrl.Text }
            };

            Process.Start(info);
        }
    }
}
