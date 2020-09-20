//===================================================
//    GemiNaut, a friendly browser for Gemini space on Windows

//    Copyright (C) 2020, Luke Emmet 

//    Email: luke [dot] emmet [at] gmail [dot] com

//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <https://www.gnu.org/licenses/>.
//===================================================

using GemiNaut.Properties;
using GemiNaut.Serialization.Commandline;
using GemiNaut.Singletons;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using static GemiNaut.MainWindow;



namespace GemiNaut
{
    public class GeminiNavigator
    {
        private MainWindow mMainWindow;
        private WebBrowser mWebBrowser;
        private ResourceFinder mFinder;

        public GeminiNavigator(MainWindow mainWindow, WebBrowser browserControl)
        {
            mMainWindow = mainWindow;
            mWebBrowser = browserControl;

            mFinder = new ResourceFinder();
        }



        public void NavigateGeminiScheme(string fullQuery, System.Windows.Navigation.NavigatingCancelEventArgs e, SiteIdentity siteIdentity)
        {
            string geminiUri;
            geminiUri = e.Uri.OriginalString;

            var sessionPath = Session.Instance.SessionPath;
            var appDir = System.AppDomain.CurrentDomain.BaseDirectory;

            var proc = new ExecuteProcess();

            //use local or dev binary for gemget
            var gemGet = mFinder.LocalOrDevFile(appDir, "Gemget", "..\\..\\..\\Gemget", "gemget-windows-386.exe");

            var hash = HashService.GetMd5Hash(fullQuery);


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

            var settings = new Settings();
            string command = "";

            if (e.Uri.Scheme == "gemini")
            {
                //pass options to gemget for download
                command = string.Format(
                    "\"{0}\" --header --no-progress-bar -m \"{1}\"Mb -t {2} -o \"{3}\" \"{4}\"",
                    gemGet,
                    settings.MaxDownloadSizeMb,
                    settings.MaxDownloadTimeSeconds,
                    rawFile,
                    fullQuery);
            } else
            {
                //pass options to gemget for download using the assigned http proxy, such as 
                //duckling-proxy https://github.com/LukeEmmet/duckling-proxy
                //this should obviously be a trusted server since it is in the middle of the 
                //request
                command = string.Format(
                    "\"{0}\" --header --no-progress-bar -m \"{1}\"Mb -t {2} -o \"{3}\"  -p \"{4}\" \"{5}\"",
                    gemGet,
                    settings.MaxDownloadSizeMb,
                    settings.MaxDownloadTimeSeconds,
                    rawFile,
                    settings.HttpSchemeProxy,
                    fullQuery);
            }

            
            var result = proc.ExecuteCommand(command, true, true);

            var geminiResponse = new GemiNaut.Response.GeminiResponse(fullQuery);

            geminiResponse.ParseGemGet(result.Item2);   //parse stdout   
            geminiResponse.ParseGemGet(result.Item3);   //parse stderr

            //ToastNotify(geminiResponse.Status + " " + geminiResponse.Meta);


            if (geminiResponse.AbandonedTimeout || geminiResponse.AbandonedSize)
            {
                var abandonMessage = String.Format(
                        "Download was abandoned as it exceeded the max size ({0}) or time ({1} s). See GemiNaut settings for details.\n\n{2}",
                        settings.MaxDownloadSizeMb,
                        settings.MaxDownloadTimeSeconds,
                        fullQuery);

                mMainWindow.ToastNotify(abandonMessage, ToastMessageStyles.Warning);
                e.Cancel = true;
                mMainWindow.ToggleContainerControlsForBrowser(true);
                return;
            }


            if (File.Exists(rawFile))
            {

                if (geminiResponse.Meta.Contains("text/gemini"))
                {
                    File.Copy(rawFile, gmiFile);

                }
                
                else if (geminiResponse.Meta.Contains("text/html"))
                {
                    //is an html file served over gemini - probably not common, but not unheard of
                    var htmltoGmiResult = ConverterService.HtmlToGmi(rawFile, gmiFile);

                    if (htmltoGmiResult.Item1 != 0)
                    {
                        mMainWindow.ToastNotify("Could not convert HTML to GMI: " + fullQuery, ToastMessageStyles.Error);
                        mMainWindow.ToggleContainerControlsForBrowser(true);
                        e.Cancel = true;
                        return;
                    }
                }
                else if (geminiResponse.Meta.Contains("text/"))
                {
                    //convert plain text to a gemini version (wraps it in a preformatted section)
                    var textToGmiResult = ConverterService.TextToGmi(rawFile, gmiFile);

                    if (textToGmiResult.Item1 != 0)
                    {
                        mMainWindow.ToastNotify("Could not render text as GMI: " + fullQuery, ToastMessageStyles.Error);
                        mMainWindow.ToggleContainerControlsForBrowser(true);
                        e.Cancel = true;
                        return;
                    }

                } else
                {

                    //a download
                    //its an image - rename the raw file and just show it
                    var pathFragment = (new UriBuilder(fullQuery)).Path;
                    var ext = Path.GetExtension(pathFragment);

                    var binFile = rawFile + (ext == "" ? ".tmp" : ext);
                    File.Copy(rawFile, binFile, true); //rename overwriting

                    if (geminiResponse.Meta.Contains("image/"))
                    {

                        mMainWindow.ShowImage(fullQuery, binFile, e);
                    } else
                    {
                        SaveFileDialog saveFileDialog = new SaveFileDialog();

                        saveFileDialog.FileName = Path.GetFileName(pathFragment);

                        if (saveFileDialog.ShowDialog() == true)
                        {

                            try
                            {
                                //save the file
                                var savePath = saveFileDialog.FileName;

                                File.Copy(binFile, savePath, true); //rename overwriting

                                mMainWindow.ToastNotify("File saved to " + savePath, ToastMessageStyles.Success);
                            }
                            catch (SystemException err)
                            {
                                mMainWindow.ToastNotify("Could not save the file due to: " + err.Message, ToastMessageStyles.Error);
                            }
                        }

                        mMainWindow.ToggleContainerControlsForBrowser(true);
                        e.Cancel = true;
                    }

                    return;

                }



                if (geminiResponse.Redirected)
                {
                    string redirectUri = fullQuery;

                    if (geminiResponse.FinalUrl.Contains("://"))
                    {
                        //a full url
                        //normalise the URi (e.g. remove default port if specified)
                        redirectUri = UriTester.NormaliseUri(new Uri(geminiResponse.FinalUrl)).ToString();
                    } else
                    {
                        //a relative one
                        var baseUri = new Uri(fullQuery);
                        var targetUri = new Uri(baseUri, geminiResponse.FinalUrl);
                        redirectUri = UriTester.NormaliseUri(targetUri).ToString();
                    }

                    var finalUri = new Uri(redirectUri);

                    if (e.Uri.Scheme == "gemini" &&  finalUri.Scheme != "gemini")
                    {
                        //cross-scheme redirect, not supported
                        mMainWindow.ToastNotify("Cross scheme redirect from Gemini not supported: " + redirectUri, ToastMessageStyles.Warning);
                        mMainWindow.ToggleContainerControlsForBrowser(true);
                        e.Cancel = true;
                        return;
                    } else
                    {
                        //others e.g. http->https redirect are fine
                    }

                    //redirected to a full gemini url
                    geminiUri = redirectUri;
                    

                    //regenerate the hashes using the redirected target url
                    hash = HashService.GetMd5Hash(geminiUri);
                    

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
                        mMainWindow.ToastNotify(err.ToString(), ToastMessageStyles.Error);
                    }

                    //update locations of gmi and html file
                    gmiFile = gmiFileNew;
                    htmlFile = htmlFileNew;

                }
                else
                {
                    geminiUri = fullQuery;
                }

                var userThemesFolder = mFinder.LocalOrDevFolder(appDir, @"GmiConverters\themes", @"..\..\GmiConverters\themes");

                var userThemeBase = Path.Combine(userThemesFolder, settings.Theme);

                mMainWindow.ShowUrl(geminiUri, gmiFile, htmlFile, userThemeBase, siteIdentity, e);

            }
            else if (geminiResponse.Status == 10 || geminiResponse.Status == 11)
            {

                //needs input

                mMainWindow.ToggleContainerControlsForBrowser(true);

                NavigateGeminiWithInput(e, geminiResponse.Meta);


            }
            else if (geminiResponse.Status == 50 || geminiResponse.Status == 51)
            {

                mMainWindow.ToastNotify("Page not found (status 51)\n\n" + e.Uri.ToString(), ToastMessageStyles.Warning);
            }
            else
            {
                //some othe error - show to the user for info
                mMainWindow.ToastNotify(String.Format(
                    "Cannot retrieve the content (exit code {0}): \n\n{1} \n\n{2}",
                    result.Item1,
                    String.Join("\n\n", geminiResponse.Info),
                    String.Join("\n\n", geminiResponse.Errors)
                    ),
                    ToastMessageStyles.Error);
            }

            mMainWindow.ToggleContainerControlsForBrowser(true);

            //no further navigation right now
            e.Cancel = true;


        }





        //navigate to a url but get some user input first
        public void NavigateGeminiWithInput(System.Windows.Navigation.NavigatingCancelEventArgs e, string message)
        {

            //position input box approx in middle of main window

            var windowCentre = WindowGeometry.WindowCentre((Window) mMainWindow);
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

                mWebBrowser.Navigate(b.ToString());
            }
            else
            {
                //dont do anything further with navigating the browser
                e.Cancel = true;
            }
        }



    }
}
