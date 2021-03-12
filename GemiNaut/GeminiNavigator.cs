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
using GemiNaut.Singletons;
using GemiNaut.Views;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Controls;
using SmolNetSharp.Protocols;
using static GemiNaut.Views.MainWindow;
using System.Security.Cryptography.X509Certificates;

namespace GemiNaut
{
 

    public class GeminiNavigator
    {
        private readonly MainWindow mMainWindow;
        private readonly WebBrowser mWebBrowser;

        public GeminiNavigator(MainWindow mainWindow, WebBrowser browserControl)
        {
            mMainWindow = mainWindow;
            mWebBrowser = browserControl;
        }


        public void NavigateGeminiScheme(string fullQuery, System.Windows.Navigation.NavigatingCancelEventArgs e, SiteIdentity siteIdentity, bool requireSecure = true)
        {
            var geminiUri = e.Uri.OriginalString;

            var sessionPath = Session.Instance.SessionPath;
            var appDir = AppDomain.CurrentDomain.BaseDirectory;

            var settings = new Settings();

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

            var uri = new Uri(fullQuery);
            //use a proxy for any other scheme that is not gemini
            var proxy = "";     //use none
            var connectInsecure = false;

            X509Certificate2 certificate;
            certificate = Session.Instance.CertificatesManager.GetCertificate(uri.Host);   //may be null if none assigned or available


        

            if (uri.Scheme != "gemini")
            {
                proxy = settings.HttpSchemeProxy;
            }

            try
            {
                GeminiResponse geminiResponse;
                try
                {
                    geminiResponse = (GeminiResponse)Gemini.Fetch(uri, certificate, proxy, connectInsecure, settings.MaxDownloadSizeMb * 1024, settings.MaxDownloadTimeSeconds);

                }
                catch (Exception err)
                {

                    //warn, but continue if there are server validation errors
                    //in these early days of Gemini we dont forbid visiting a site with an expired cert or mismatched host name
                    //but we do give a warning each time
                    if (err.Message == "The remote certificate was rejected by the provided RemoteCertificateValidationCallback.")
                    {

                        mMainWindow.ToastNotify("Note: the certificate from: " + e.Uri.Authority + " is expired or invalid.", ToastMessageStyles.Warning);

                        //try again insecure this time
                        geminiResponse = (GeminiResponse)Gemini.Fetch(uri, certificate, proxy, true, settings.MaxDownloadSizeMb * 1024, settings.MaxDownloadTimeSeconds);

                    }
                    else
                    {
                        //reraise
                        throw;
                    }
                }

                if (geminiResponse.codeMajor == '1')
                {
                    //needs input, then refetch

                    mMainWindow.ToggleContainerControlsForBrowser(true);

                    NavigateGeminiWithInput(e, geminiResponse);
                }

                else if (geminiResponse.codeMajor == '2')
                {
                    //success
                    File.WriteAllBytes(rawFile, geminiResponse.bytes.ToArray());


                    if (File.Exists(rawFile))
                    {
                        if (geminiResponse.meta.Contains("text/gemini"))
                        {
                            File.Copy(rawFile, gmiFile);
                        }
                        else if (geminiResponse.meta.Contains("text/html"))
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
                        else if (geminiResponse.meta.Contains("text/"))
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
                        }
                        else
                        {
                            //a download
                            //its an image - rename the raw file and just show it
                            var pathFragment = (new UriBuilder(fullQuery)).Path;
                            var ext = Path.GetExtension(pathFragment);

                            var binFile = rawFile + (ext == "" ? ".tmp" : ext);
                            File.Copy(rawFile, binFile, true); //rename overwriting

                            if (geminiResponse.meta.Contains("image/"))
                            {
                                mMainWindow.ShowImage(fullQuery, binFile, e);
                            }
                            else
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

                        if (geminiResponse.uri.ToString() != fullQuery)
                        {
                            string redirectUri = fullQuery;

                            if (geminiResponse.uri.ToString().Contains("://"))
                            {
                                //a full url
                                //normalise the URi (e.g. remove default port if specified)
                                redirectUri = UriTester.NormaliseUri(new Uri(geminiResponse.uri.ToString())).ToString();
                            }
                            else
                            {
                                //a relative one
                                var baseUri = new Uri(fullQuery);
                                var targetUri = new Uri(baseUri, geminiResponse.uri.ToString());
                                redirectUri = UriTester.NormaliseUri(targetUri).ToString();
                            }

                            var finalUri = new Uri(redirectUri);

                            if (e.Uri.Scheme == "gemini" && finalUri.Scheme != "gemini")
                            {
                                //cross-scheme redirect, not supported
                                mMainWindow.ToastNotify("Cross scheme redirect from Gemini not supported: " + redirectUri, ToastMessageStyles.Warning);
                                mMainWindow.ToggleContainerControlsForBrowser(true);
                                e.Cancel = true;
                                return;
                            }
                            else
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

                        var userThemesFolder = ResourceFinder.LocalOrDevFolder(appDir, @"GmiConverters\themes", @"..\..\..\GmiConverters\themes");

                        var userThemeBase = Path.Combine(userThemesFolder, settings.Theme);

                        mMainWindow.ShowUrl(geminiUri, gmiFile, htmlFile, userThemeBase, siteIdentity, e);
                    }
                }

                // codemajor = 3 is redirect - should eventually end in success or raise an error

                else if (geminiResponse.codeMajor == '4')
                {
                    mMainWindow.ToastNotify("Temporary failure (status 4X)\n\n" + e.Uri.ToString(), ToastMessageStyles.Warning);
                }
                else if (geminiResponse.codeMajor == '5')
                {
                    if (geminiResponse.codeMinor == '1')
                    {
                        mMainWindow.ToastNotify("Page not found\n\n" + e.Uri.ToString(), ToastMessageStyles.Warning);
                    }
                    else
                    {
                        mMainWindow.ToastNotify("Permanent failure (status 5X)\n\n" + geminiResponse.meta + "\n\n" + e.Uri.ToString(), ToastMessageStyles.Warning);

                    }
                }
                else if (geminiResponse.codeMajor == '6')
                {
                    mMainWindow.ToastNotify("Certificate requried. Choose one and try again.\n\n" + e.Uri.ToString(), ToastMessageStyles.Warning);


                }

                else
                {
                    mMainWindow.ToastNotify("Unexpected output from server " +
                        "(status " + geminiResponse.codeMajor + "." + geminiResponse.codeMinor + ") " +
                        geminiResponse.meta + "\n\n"
                        + e.Uri.ToString(), ToastMessageStyles.Warning);

                }

            }
            catch (Exception err)
            {
                //generic handler for other runtime errors
                mMainWindow.ToastNotify("Error getting gemini content for " + e.Uri.ToString() + "\n\n" + err.Message, ToastMessageStyles.Warning);
            }


            //make the window responsive again
            mMainWindow.ToggleContainerControlsForBrowser(true);

            //no further navigation right now
            e.Cancel = true;

        }

        //navigate to a url but get some user input first
        public void NavigateGeminiWithInput(System.Windows.Navigation.NavigatingCancelEventArgs e, GeminiResponse geminiResponse)
        {
            //position input box approx in middle of main window

            var targetUri = geminiResponse.uri;
            var message = geminiResponse.meta;

            var windowCentre = WindowGeometry.WindowCentre(mMainWindow);
            var inputPrompt = "Input request from Gemini server\n\n" +
                "  " + targetUri.Host + targetUri.LocalPath + "\n\n" +
                message;

            string input = Interaction.InputBox(inputPrompt, "Server input request", "", windowCentre.Item1, windowCentre.Item2);

            if (input != "")
            {
                //encode the query
                var b = new UriBuilder();
                b.Scheme = targetUri.Scheme;
                b.Host = targetUri.Host;
                if (targetUri.Port != -1) { b.Port = targetUri.Port; }
                b.Path = targetUri.LocalPath;
                //!%22%C2%A3$%25%5E&*()_+1234567890-=%7B%7D:@~%3C%3E?[];'#,./
                b.Query = Uri.EscapeDataString(input);      //escape the query result

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
