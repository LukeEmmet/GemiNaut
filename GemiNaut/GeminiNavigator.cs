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
using GemiNaut.Views;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Windows.Controls;
using SmolNetSharp.Protocols;
using static GemiNaut.Views.MainWindow;
using System.Collections.Generic;
using GeminiProtocol;
using System.Net;
using System.Text;

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


        private Tuple<int, string, string> GemGet(string fullQuery, string rawFile, string scheme, bool requireSecure)
        {
            var sessionPath = Session.Instance.SessionPath;
            var appDir = AppDomain.CurrentDomain.BaseDirectory;

            //use local or dev binary for gemget
            var gemGet = ResourceFinder.LocalOrDevFile(appDir, "Gemget", "..\\..\\..\\..\\..\\Gemget", "gemget-windows-386.exe");
            var settings = new Settings();
            string command = "";

            var secureFlag = requireSecure ? "" : " -i ";

            if (scheme == "gemini")
            {
                //pass options to gemget for download
                command = string.Format(
                    "\"{0}\" {1} --header --no-progress-bar -m \"{2}\"Mb -t {3} -o \"{4}\" \"{5}\"",
                    gemGet,
                    secureFlag,
                    settings.MaxDownloadSizeMb,
                    settings.MaxDownloadTimeSeconds,
                    rawFile,
                    fullQuery);
            }
            else
            {
                //pass options to gemget for download using the assigned http proxy, such as 
                //duckling-proxy https://github.com/LukeEmmet/duckling-proxy
                //this should obviously be a trusted server since it is in the middle of the 
                //request
                command = string.Format(
                    "\"{0}\" {1} --header --no-progress-bar -m \"{2}\"Mb -t {3} -o \"{4}\"  -p \"{5}\" \"{6}\"",
                    gemGet,
                    secureFlag,
                    settings.MaxDownloadSizeMb,
                    settings.MaxDownloadTimeSeconds,
                    rawFile,
                    settings.HttpSchemeProxy,
                    fullQuery);
            }

            var result = ExecuteProcess.ExecuteCommand(command, true, true);

            return (result);

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


            try
            {

                GeminiResponse geminiResponse;
                try
                {
                 geminiResponse = (GeminiResponse)Gemini.Fetch(new Uri(fullQuery), settings.MaxDownloadSizeMb * 1024, settings.MaxDownloadTimeSeconds);

                } catch
                {
                    //very strange situation, for some servers, they work every other request, and in between send a malformed response. 
                    //Maybe a particular flavour of server has the problem
                    //for example
                    //gemini://calcuode.com/
                    //not seen in some other clients, so may need investigating **FIXME

                    try
                    {
                        //send the request again
                        geminiResponse = (GeminiResponse)Gemini.Fetch(new Uri(fullQuery), settings.MaxDownloadSizeMb * 1024, settings.MaxDownloadTimeSeconds);
                    } catch 
                    {
                        //re raise
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
                    File.WriteAllBytes(rawFile, geminiResponse.pyld.ToArray());


                    //in these early days of Gemini we dont forbid visiting a site with an expired cert or mismatched host name
                    //but we do give a warning each time
                    //if (result.Item1 == 1 && requireSecure)
                    //{
                    //    var tryInsecure = false;
                    //    var securityError = "";
                    //    if (geminiResponse.Errors[0].Contains("server cert is expired"))
                    //    {
                    //        tryInsecure = true;
                    //        securityError = "Server certificate is expired";
                    //    }
                    //    else if (geminiResponse.Errors[0].Contains("hostname does not verify"))
                    //    {
                    //        tryInsecure = true;
                    //        securityError = "Host name does not verify";
                    //    }
                    //    if (tryInsecure)
                    //    {
                    //        //give a warning and try again with insecure
                    //        mMainWindow.ToastNotify("Note: " + securityError + " for: " + e.Uri.Authority, ToastMessageStyles.Warning);
                    //        NavigateGeminiScheme(fullQuery, e, siteIdentity, false);
                    //        return;
                    //    }
                    //}


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
                    mMainWindow.ToastNotify("Certificate required, not yet supported (status 61)\n\n" + e.Uri.ToString(), ToastMessageStyles.Error);

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
