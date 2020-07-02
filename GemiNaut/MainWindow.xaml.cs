using System;
using System.Security.Cryptography;
using System.Text;
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
using System.Web;
using GemiNaut.Properties;
using mshtml;
using System.Net;
using GemiNaut.Response;



namespace GemiNaut
{
    public partial class MainWindow : Window

    {
        private Dictionary<string, string> _urlsByHash;
        private Notifier _notifier;



        public enum GopherParseTypes
        {
            Map, Text
        }

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


            RefreshBookmarkMenu();

            var settings = new Settings();

            BrowserControl.Navigate(settings.HomeUrl);

            BuildThemeMenu();
            TickSelectedThemeMenu();
        }

        private bool TextIsUri(string text)
        {
            Uri outUri;
            return (Uri.TryCreate(text, UriKind.Absolute, out outUri));
        }

        private void txtUrl_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)

                if (TextIsUri(txtUrl.Text))
                {
                    BrowserControl.Navigate(txtUrl.Text);

                } else
                {
                    ToastNotify("Not a valid URI: " + txtUrl.Text, ToastMessageStyles.Error);
                }
        }


        public static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        private void ToggleContainerControlsForBrowser(bool toState)
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
            var appDir = System.AppDomain.CurrentDomain.BaseDirectory;
            string geminiUri;


            geminiUri = e.Uri.OriginalString;

            var fullQuery = e.Uri.OriginalString;

            //sanity check we have a valid URL syntax at least
            if (e.Uri.Scheme == null)
            {
                ToastNotify("Invalid URL: " + e.Uri.OriginalString, ToastMessageStyles.Error);
                e.Cancel = true;
            }

            //ToastNotify(string.Format("{0}\n\n{1}\n\n{2}", e.Uri.ToString(), fullQuery, e.Uri.OriginalString));

            //use the current session folder
            var sessionPath = Session.Instance.SessionPath;

            ToggleContainerControlsForBrowser(false);

            if (e.Uri.Scheme == "gemini")
            {

                //these are the only ones we "navigate" to. We do this by downloading the GMI content
                //converting to HTML and then actually navigating to that.

                var proc = new ExecuteProcess();

                //use local or dev binary for gemget
                var gemGet = LocalOrDevFile(appDir, "Gemget", "..\\..\\..\\Gemget", "gemget-windows-386.exe");

                string hash;

                using (MD5 md5Hash = MD5.Create())
                {
                    hash = GetMd5Hash(md5Hash, fullQuery);
                }

                //uses .txt as extension so content loaded as text/plain not interpreted by the browser
                //if user requests a view-source.
                var rawFile = sessionPath + "\\" + hash + ".txt";
                var gmiFile = sessionPath + "\\" + hash + ".gmi";
                var htmlFile = sessionPath + "\\" + hash + ".htm";

                //delete txt file as GemGet seems to sometimes overwrite not create afresh
                File.Delete(rawFile);
                File.Delete(gmiFile);

                //delete any existing html file to encourage webbrowser to reload it
                File.Delete(htmlFile);

                //use insecure flag as gemget does not check certs correctly in current version
                var command = string.Format("\"{0}\" --header -o \"{1}\" \"{2}\"", gemGet, rawFile, fullQuery);

                var result = proc.ExecuteCommand(command, true, true);

                var geminiResponse = new GemiNaut.Response.GeminiResponse(fullQuery);

                geminiResponse.ParseGemGet(result.Item2);   //parse stdout   
                geminiResponse.ParseGemGet(result.Item3);   //parse stderr

                //ToastNotify(geminiResponse.Status + " " + geminiResponse.Meta);

                if (File.Exists(rawFile))
                {

                    if (geminiResponse.Meta.Contains("text/gemini"))
                    {
                        File.Copy(rawFile, gmiFile);

                    } else
                    {
                        //convert plain text to a gemini version (wraps it in a preformatted section)
                        var textToGmiResult = TextToGmi(rawFile, gmiFile);

                        if (textToGmiResult.Item1 != 0)
                        {
                            ToastNotify("Could not render text as GMI: " + fullQuery, ToastMessageStyles.Error);
                            ToggleContainerControlsForBrowser(true);
                            e.Cancel = true;
                            return;
                        }

                    }
                    if (geminiResponse.Redirected)
                    {
                        var redirectUri = geminiResponse.FinalUrl;

                        if (redirectUri.Substring(0, 9) != "gemini://")
                        {
                            //need to unpack
                            var redirectUriObj = new Uri(redirectUri);
                            if (redirectUriObj.Scheme != "gemini")
                            {
                                //is external
                                LaunchExternalUri(redirectUri);
                                e.Cancel = true;
                                ToggleContainerControlsForBrowser(true);
                            }
                            else
                            {
                                //is a relative url, not yet implemented
                                ToastNotify("Redirect to relative URL not yet implemented: " + redirectUri, ToastMessageStyles.Warning);
                                ToggleContainerControlsForBrowser(true);
                                e.Cancel = true;
                            }
                        }
                        else
                        {
                            //redirected to a full gemini url
                            geminiUri = redirectUri;
                        }

                        //regenerate the hashes using the redirected target url
                        using (MD5 md5Hash = MD5.Create())
                        {
                            hash = GetMd5Hash(md5Hash, geminiUri);
                        }

                        var gmiFileNew = sessionPath + "\\" + hash + ".txt";
                        var htmlFileNew = sessionPath + "\\" + hash + ".htm";

                        //move the source file
                        try
                        {
                            if (File.Exists(gmiFileNew))
                            {
                                File.Delete(gmiFileNew);
                            }
                            File.Move(gmiFile, gmiFileNew);
                        }
                        catch (Exception err)
                        {
                            ToastNotify(err.ToString(), ToastMessageStyles.Error);
                        }

                        //update locations of gmi and html file
                        gmiFile = gmiFileNew;
                        htmlFile = htmlFileNew;

                    }
                    else
                    {
                        geminiUri = fullQuery;
                    }

                    var settings = new Settings();
                    var userThemesFolder = LocalOrDevFolder(appDir, @"GmiConverters\themes", @"..\..\GmiConverters\themes");

                    var userThemeBase = Path.Combine(userThemesFolder, settings.Theme);

                    ShowUrl(geminiUri, gmiFile, htmlFile, userThemeBase, e);

                }
                else if (geminiResponse.Status == 10 || geminiResponse.Status == 11)
                {

                    //needs input

                    ToggleContainerControlsForBrowser(true);

                    NavigateGeminiWithInput(e, geminiResponse.Meta);
                    

                } else if (geminiResponse.Status == 50 || geminiResponse.Status == 51) {

                    ToastNotify("Page not found (status 51)\n\n" + e.Uri.ToString(), ToastMessageStyles.Warning);
                }
                else
                {
                    //some othe error - show to the user for info
                    ToastNotify(String.Format("Invalid request or server not found: \n\n{0} \n(gemget exit code: {1})", result.Item3, result.Item1), ToastMessageStyles.Error);
                }

                ToggleContainerControlsForBrowser(true);

                //no further navigation right now
                e.Cancel = true;



            }

            else if (e.Uri.Scheme == "gopher")
            {

                //check if it is a query selector without a parameter
                if (!e.Uri.OriginalString.Contains("%09") && e.Uri.PathAndQuery.StartsWith("/7/"))
                {
                    NavigateGopherWithInput(e);

                    ToggleContainerControlsForBrowser(true);

                    //no further navigation right now
                    e.Cancel = true;

                    return;

                }


                var proc = new ExecuteProcess();

                //use local or dev binary for gemget
                var gopherClient = LocalOrDevFile(appDir, "GoGopher", "..\\..\\..\\GoGopher", "main.exe");

                string hash;

                using (MD5 md5Hash = MD5.Create())
                {
                    hash = GetMd5Hash(md5Hash, fullQuery);
                }

                //uses .txt as extension so content loaded as text/plain not interpreted by the browser
                //if user requests a view-source.
                var gopherFile = sessionPath + "\\" + hash + ".txt";
                var gmiFile = sessionPath + "\\" + hash + ".gmi";
                var htmlFile = sessionPath + "\\" + hash + ".htm";

                //delete txt file as GemGet seems to sometimes overwrite not create afresh
                File.Delete(gopherFile);

                //delete any existing html file to encourage webbrowser to reload it
                File.Delete(gmiFile);

                //save to the file
                var command = string.Format("\"{0}\" \"{1}\" \"{2}\"", gopherClient, fullQuery, gopherFile);


                var result = proc.ExecuteCommand(command, true, true);

                var exitCode = result.Item1;
                var stdOut = result.Item2;

                if (exitCode != 0)
                {
                    ToastNotify(result.Item3, ToastMessageStyles.Error);
                    ToggleContainerControlsForBrowser(true);    //reenable browser
                    e.Cancel = true;
                    return;

                }


                if (File.Exists(gopherFile))
                {
                    string parseFile;

                    if (stdOut.Contains("DIR") || stdOut.Contains("QRY")) {
                        //convert gophermap to text/gemini

                        //ToastNotify("Converting gophermap to " + gmiFile);
                        GophertoGmi(gopherFile, gmiFile, fullQuery, GopherParseTypes.Map);
                        parseFile = gmiFile;

                    } else if (stdOut.Contains("TXT"))
                    {
                        GophertoGmi(gopherFile, gmiFile, fullQuery, GopherParseTypes.Text);
                        parseFile = gmiFile;

                    }
                    else
                    {
                        //treat as text, but notify the type
                        ToastNotify("Loading a " + result.Item2.ToString());

                        parseFile = gopherFile;

                    }

                    if (!File.Exists(gmiFile))
                    {
                        ToastNotify("Did not create expected GMI file for " + fullQuery + " in " + gmiFile, ToastMessageStyles.Error);
                        ToggleContainerControlsForBrowser(true);
                        e.Cancel = true;


                    }
                    else
                    {
                        var settings = new Settings();
                        var userThemesFolder = LocalOrDevFolder(appDir, @"GmiConverters\themes", @"..\..\GmiConverters\themes");

                        var userThemeBase = Path.Combine(userThemesFolder, settings.Theme);

                        ShowUrl(fullQuery, parseFile, htmlFile, userThemeBase, e);

                    }

                }


            }

            else if (e.Uri.Scheme == "file")
            {
                //just load the converted html file
                //no further action.
            }
            else if (e.Uri.Scheme == "about")
            {
                //just load the help file
                //no further action
                ToggleContainerControlsForBrowser(true);

                var sourceFileName = e.Uri.PathAndQuery.Substring(1);      //trim off leading /

                //this expects uri has a "geminaut" domain so gmitohtml converter can proceed for now
                //I think it requires a domain for parsing...
                fullQuery = e.Uri.OriginalString;

                string hash;
                using (MD5 md5Hash = MD5.Create())
                {
                    hash = GetMd5Hash(md5Hash, fullQuery);
                }

                var hashFile = Path.Combine(sessionPath, hash + ".txt");
                var htmlCreateFile = Path.Combine(sessionPath, hash + ".htm");

                var helpFolder = LocalOrDevFolder(appDir, @"Docs", @"..\..\Docs");
                var helpFile = Path.Combine(helpFolder, sourceFileName);

                //use a specific theme so about pages look different to user theme
                var templateBaseName = Path.Combine(helpFolder, "help-theme");



                if (File.Exists(helpFile))
                {
                    File.Copy(helpFile, hashFile, true);
                    ShowUrl(fullQuery, hashFile, htmlCreateFile, templateBaseName, e);
                }
                else
                {
                    ToastNotify("No content was found for: " + fullQuery, ToastMessageStyles.Warning);
                    e.Cancel = true;
                }

            }
            else
            {
                //we don't care about any other protocols
                //so we open those in system web browser to deal with
                LaunchExternalUri(e.Uri.ToString());
                ToggleContainerControlsForBrowser(true);
                e.Cancel = true;
            }
        }

        private void ShowUrl(string sourceUrl, string gmiFile, string htmlFile, string themePath, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {

            string hash;

            using (MD5 md5Hash = MD5.Create())
            {
                hash = GetMd5Hash(md5Hash, sourceUrl);
            }

            //create the html file
            var result = GmiToHtml(gmiFile, htmlFile, sourceUrl, themePath);

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

        //launch url in system browser
        private void LaunchExternalUri(string uri)
        {
                System.Diagnostics.Process.Start(uri);
                ToastNotify("Launching in system browser: " + uri);
        }


        //navigate to a url but get some user input first
        private void NavigateGeminiWithInput(System.Windows.Navigation.NavigatingCancelEventArgs e, string message)
        {

            //position input box approx in middle of main window

            var windowCentre = WindowCentre(Application.Current.MainWindow);
            var inputPrompt = "Input request from Gemini server\n\n" +
                "  " + e.Uri.Host + e.Uri.LocalPath.ToString() + "\n\n" +
                message;

            string input = Interaction.InputBox(inputPrompt, "Server input request", "", windowCentre.Item1, windowCentre.Item2);

            if (input != "")
            {
                //encode the query
                var b = new UriBuilder();
                b.Scheme = e.Uri.Scheme;
                b.Host = e.Uri.Host;
                if (e.Uri.Port != -1) { b.Port = e.Uri.Port; }
                b.Path = e.Uri.LocalPath;   
                //!%22%C2%A3$%25%5E&*()_+1234567890-=%7B%7D:@~%3C%3E?[];'#,./
                b.Query = System.Uri.EscapeDataString(input);      //escape the query result

                //ToastNotify(b.ToString());

                BrowserControl.Navigate(b.ToString());
            }
            else
            {
                //dont do anything further with navigating the browser
                e.Cancel = true;
            }
        }

        //navigate to a url but get some user input first
        private void NavigateGopherWithInput(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {

            //position input box approx in middle of main window

            var windowCentre = WindowCentre(Application.Current.MainWindow);
            var inputPrompt = "Input request from Gopher server\n\n" +
                "  " + e.Uri.Host + e.Uri.LocalPath.ToString() + "\n\n" +
                "Please provide your input:";

            string input = Interaction.InputBox(inputPrompt, "Server input request", "", windowCentre.Item1, windowCentre.Item2);

            if (input != "")
            {
                //encode the query
                var b = new UriBuilder();
                b.Scheme = e.Uri.Scheme;
                b.Host = e.Uri.Host;
                if (e.Uri.Port != -1) { b.Port = e.Uri.Port; }
                b.Path = e.Uri.LocalPath;


                //!%22%C2%A3$%25%5E&*()_+1234567890-=%7B%7D:@~%3C%3E?[];'#,./

                //use an escaped tab then the content
                var query = b.ToString() + "%09" + System.Uri.EscapeDataString(input);      //escape the query result;
                //ToastNotify(query);

                BrowserControl.Navigate(query);
            }
            else
            {
                //dont do anything further with navigating the browser
                e.Cancel = true;
            }
        }

        //get the position of the centre of a window
        private Tuple<int, int> WindowCentre(Window window)
        {
            var inputLeft = (int) (window.Left + (window.Width / 2) - 180);
            var inputTop = (int) (window.Top + (window.Height / 2) - 20);

            return new Tuple<int, int>(inputLeft, inputTop);
        }

        //return the expected location of a file in two possible folders, preferring the local one
        //(only checks for folder existence, not file)
        public string LocalOrDevFile(string startFolder, string localFolder, string devFolder, string filename)
        {
            var useFolder = LocalOrDevFolder(startFolder, localFolder, devFolder);
            
            return Path.GetFullPath(Path.Combine(startFolder, useFolder, filename));
        }

        public string LocalOrDevFolder(string startFolder, string localFolder, string devFolder)
        {
            return Directory.Exists(Path.Combine(startFolder, localFolder))
                ? startFolder + localFolder
                : Path.Combine(startFolder, devFolder);
        }


        //convert text to GMI for raw text
        public Tuple<int, string, string> TextToGmi(string rawPath, string outPath)
        {
            var appDir = System.AppDomain.CurrentDomain.BaseDirectory;

            //allow for rebol and converters to be in sub folder of exe (e.g. when deployed)
            //otherwise we use the development ones which are version controlled
            var rebolPath = LocalOrDevFile(appDir, @"Rebol", @"..\..\Rebol", "r3-core.exe");
            var scriptPath = LocalOrDevFile(appDir, @"GmiConverters", @"..\..\GmiConverters", "TextAsIs.r3");


            //due to bug in rebol 3 at the time of writing (mid 2020) there is a known bug in rebol 3 in 
            //working with command line parameters, so we need to escape quotes
            //see https://stackoverflow.com/questions/6721636/passing-quoted-arguments-to-a-rebol-3-script
            //also hypens are also problematic, so we base64 each param and unpack in the script
            var command = String.Format("\"{0}\" -cs \"{1}\" \"{2}\" \"{3}\"  ",
                rebolPath,
                scriptPath,
                Base64Encode(rawPath),
                Base64Encode(outPath)

                );

            var execProcess = new ExecuteProcess();

            var result = execProcess.ExecuteCommand(command);

            return (result);
        }

        //convert GMI to HTML for display and save to outpath
        public Tuple<int, string, string> GmiToHtml (string gmiPath, string outPath, string uri, string theme)
        {
            var appDir = System.AppDomain.CurrentDomain.BaseDirectory;

            //allow for rebol and converters to be in sub folder of exe (e.g. when deployed)
            //otherwise we use the development ones which are version controlled
            var rebolPath = LocalOrDevFile(appDir, @"Rebol", @"..\..\Rebol", "r3-core.exe");
            var scriptPath = LocalOrDevFile(appDir, @"GmiConverters", @"..\..\GmiConverters", "GmiToHtml.r3");


            //due to bug in rebol 3 at the time of writing (mid 2020) there is a known bug in rebol 3 in 
            //working with command line parameters, so we need to escape quotes
            //see https://stackoverflow.com/questions/6721636/passing-quoted-arguments-to-a-rebol-3-script
            //also hypens are also problematic, so we base64 each param and unpack in the script
            var command = String.Format("\"{0}\" -cs \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\" ",
                rebolPath, 
                scriptPath,
                Base64Encode(gmiPath),
                Base64Encode(outPath),
                Base64Encode(uri),
                Base64Encode(theme)

                );

            var execProcess = new ExecuteProcess();

            var result = execProcess.ExecuteCommand(command);

            return (result);
        }

        //convert GopherText to GMI and save to outpath
        public void GophertoGmi(string gopherPath, string outPath, string uri, GopherParseTypes parseType)
        {
            var appDir = System.AppDomain.CurrentDomain.BaseDirectory;

            var parseScript = (parseType == GopherParseTypes.Map) ? "GophermapToGmi.r3" : "GophertextToGmi.r3";

            //allow for rebol and converters to be in sub folder of exe (e.g. when deployed)
            //otherwise we use the development ones which are version controlled
            var rebolPath = LocalOrDevFile(appDir, @"Rebol", @"..\..\Rebol", "r3-core.exe");
            var scriptPath = LocalOrDevFile(appDir, @"GmiConverters", @"..\..\GmiConverters", parseScript);
            
            //due to bug in rebol 3 at the time of writing (mid 2020) there is a known bug in rebol 3 in 
            //working with command line parameters, so we need to escape quotes
            //see https://stackoverflow.com/questions/6721636/passing-quoted-arguments-to-a-rebol-3-script
            //also hypens are also problematic, so we base64 each parameter and unpack it in the script
            var command = String.Format("\"{0}\" -cs \"{1}\" \"{2}\" \"{3}\" \"{4}\" ",
                rebolPath,
                scriptPath,
                Base64Encode(gopherPath),
                Base64Encode(outPath),
                Base64Encode(uri)

                );

            var execProcess = new ExecuteProcess();

            var result = execProcess.ExecuteCommand(command);

            Debug.Print(command);

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

        /// <summary>
        /// base 64 function from https://stackoverflow.com/questions/11743160/how-do-i-encode-and-decode-a-base64-string
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
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
            if (TextIsUri(txtUrl.Text))
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

            using (MD5 md5Hash = MD5.Create())
            {

                hash = GetMd5Hash(md5Hash, txtUrl.Text);
            }

            //uses .txt as extension so content loaded as text/plain not interpreted by the browser
            var gmiFile = sessionPath + "\\" + hash + ".txt";

             BrowserControl.Navigate(gmiFile);


        }

        private void MenuViewSettingsHome_Click(object sender, RoutedEventArgs e)
        {
            var settings = new Settings();
            var position = WindowCentre(Application.Current.MainWindow);
            var newHome = Interaction.InputBox("Enter your home URL", "Home URL", settings.HomeUrl, position.Item1, position.Item2);

            if (newHome != "")
            {
                settings.HomeUrl = newHome;
                settings.Save();
            }

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
            var themeFolder = LocalOrDevFolder(appDir, @"GmiConverters\themes", @"..\..\GmiConverters\themes");


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
            ShowTitle(BrowserControl.Document);

            //we need to turn on/off other elements so focus doesnt move elsewhere
            //in that case the keyboard events go elsewhere and you have to click 
            //into the browser to get it to work again
            //see https://stackoverflow.com/questions/8495857/webbrowser-steals-focus
            ToggleContainerControlsForBrowser(true);
        }

        private void mnuMenuBookmarksAdd_Click(object sender, RoutedEventArgs e)
        {
            var settings = new Settings();

            var doc = (HTMLDocument)BrowserControl.Document;

            var url = txtUrl.Text;

            foreach (var line in BookmarkLines())
            {
                var linkParts = ParseGeminiLink(line);
                if (linkParts[0] == url)
                {
                    //already exists
                    ToastNotify("That URL is already in the bookmarks, skipping.\n" + url, ToastMessageStyles.Warning);
                    return;
                }
            }

            //a new one
            settings.Bookmarks += "\r\n" + "=> " + url + "  " + doc.title;
            settings.Save();
            RefreshBookmarkMenu();
            ToastNotify("Bookmark added: " + (doc.title + " " + txtUrl.Text).Trim(), ToastMessageStyles.Success);
        }

        private void mnuMenuBookmarksEdit_Click(object sender, RoutedEventArgs e)
        {
            var settings = new Settings();

            Bookmarks winBookmarks = new Bookmarks();
            winBookmarks.MainWindow(this);


            //show modally
            winBookmarks.Owner = this;
            winBookmarks.ShowDialog();

        }

        private void mnuMenuBookmarksGo_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;

            BrowserControl.Navigate(menuItem.CommandParameter.ToString());

        }

        private string[] BookmarkLines()
        {
            var settings = new Settings();
            string[] array = new string[2];
            array[0] = "\r\n";

            return (settings.Bookmarks.Split(array, StringSplitOptions.RemoveEmptyEntries));

        }

        private string[] ParseGeminiLink(string line)
        {
            var linkRegex = new Regex(@"\s*=>\s([^\s]*)(.*)");
            string[] array = new string[2];

            if (linkRegex.IsMatch(line))
            {
                Match match = linkRegex.Match(line);
                array[0] = match.Groups[1].ToString().Trim();
                array[1] = match.Groups[2].ToString().Trim();

                //if display text is empty, use url
                if (array[1] == "") {
                    array[1] = array[0];
                }

            } else
            {
                //isnt a link, return null,null
                array[0] = null;
                array[1] = null;
            }

            return (array);
        }

        public void RefreshBookmarkMenu()
        {

            mnuBookMarks.Items.Clear();

            foreach (var line in BookmarkLines())
            {
                var bmMenu = new MenuItem();

                var linkParts = ParseGeminiLink(line);
                if (TextIsUri(linkParts[0]))
                {
                    bmMenu.CommandParameter = linkParts[0];
                    bmMenu.Header = linkParts[1];
                    bmMenu.ToolTip = linkParts[0];

                    bmMenu.Click += mnuMenuBookmarksGo_Click;

                    mnuBookMarks.Items.Add(bmMenu);

                }
                else if (line.Substring(0, 2) == "--")
                {
                    mnuBookMarks.Items.Add(new Separator());
                }
                else if (line.Substring(0, 2) == "__")
                {
                    mnuBookMarks.Items.Add(new Separator());
                }
                {
                    //anything else in the bookmarks file ignored for now
                }


            }



        }
    }
}
