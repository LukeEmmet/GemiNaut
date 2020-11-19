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
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Navigation;
using static GemiNaut.Views.MainWindow;

namespace GemiNaut
{
    public class HttpNavigator
    {
        private readonly MainWindow mMainWindow;
        private readonly WebBrowser mWebBrowser;

        public HttpNavigator(MainWindow mainWindow, WebBrowser browserControl)
        {
            mMainWindow = mainWindow;
            mWebBrowser = browserControl;
        }

        public static bool IsModeSwitch(string linkId)
        {
            return linkId == "web-switch-plain" ||
                        linkId == "web-switch-simplified" ||
                        linkId == "web-switch-all";
        }

        public void NavigateHttpScheme(string fullQuery, NavigatingCancelEventArgs e, SiteIdentity siteIdentity, string linkId)
        {
            var httpUri = e.Uri.OriginalString;

            var sessionPath = Session.Instance.SessionPath;
            var appDir = AppDomain.CurrentDomain.BaseDirectory;

            var hash = HashService.GetMd5Hash(fullQuery);

            var settings = new Settings();

            //uses .txt as extension so content loaded as text/plain not interpreted by the browser
            //if user requests a view-source.
            var rawFile = sessionPath + "\\" + hash + ".txt";
            var gmiFile = sessionPath + "\\" + hash + ".gmi";
            var htmlFile = sessionPath + "\\" + hash + ".htm";

            var reRender = false;       //if just re-rendering the same content in a different mode, dont re-fetch

            var httpResponse = new Response.HttpResponse(fullQuery);

            Tuple<int, string, string> result;

            if (IsModeSwitch(linkId) && File.Exists(rawFile))
            {
                //use the existing content
                reRender = true;
                result = new Tuple<int, string, string>(0, "", "");

                //set default mode to this mode
                settings.WebRenderMode = linkId;
                settings.Save();
            }
            else
            {
                File.Delete(rawFile);

                //delete txt file as GemGet seems to sometimes overwrite not create afresh
                File.Delete(gmiFile);

                //use local or dev binary for gemget
                var httpGet = ResourceFinder.LocalOrDevFile(appDir, "HttpGet", "..\\..\\..\\..\\HttpGet", "http-get.exe");

                //pass options to gemget for download
                var command = string.Format(
                    "\"{0}\" --header -o \"{1}\" \"{2}\"",
                    httpGet,
                    rawFile,
                    fullQuery);

                result = ExecuteProcess.ExecuteCommand(command, true, true);

                if (result.Item1 != 0)
                {
                    mMainWindow.ToastNotify("Could not access web resource: " + fullQuery + "\n" + result.Item3, ToastMessageStyles.Warning);
                    mMainWindow.ToggleContainerControlsForBrowser(true);
                    e.Cancel = true;
                    return;
                }

                httpResponse.ParseGemGet(result.Item2);   //parse stdout   
                httpResponse.ParseGemGet(result.Item3);   //parse stderr

                if (httpResponse.StatusCode == 404)
                {
                    {
                        mMainWindow.ToastNotify("Resource not found:\n" + fullQuery, ToastMessageStyles.Warning);
                        mMainWindow.ToggleContainerControlsForBrowser(true);
                        e.Cancel = true;
                        return;
                    }
                    //ToastNotify(httpResponse.Status + " " + httpResponse.Meta);  
                }
                else if (httpResponse.StatusCode != 200)
                {
                    //some other error
                    mMainWindow.ToastNotify("Could not get resource: "
                        + httpResponse.Status + "\n"
                        + fullQuery, ToastMessageStyles.Warning);
                    mMainWindow.ToggleContainerControlsForBrowser(true);
                    e.Cancel = true;
                    return;
                }
            }

            //delete any existing html file to encourage webbrowser to reload it
            File.Delete(htmlFile);

            if (File.Exists(rawFile))
            {
                if (httpResponse.ContentType.Contains("text/html") || reRender)
                {
                    //use local or dev binary for goose
                    var gooseConvert = ResourceFinder.LocalOrDevFile(appDir, "Goose", "..\\..\\..\\..\\Goose", "goose-cli.exe");
                    var gooseOut = sessionPath + "\\" + hash + ".goose";

                    var gooseCommand = "";

                    var htmlPath = "";

                    if (settings.WebRenderMode == "web-switch-plain")
                    {
                        //pass options to goose to get plain text
                        gooseCommand = string.Format(
                            "\"{0}\" -t -i \"{1}\" -o \"{2}\"",
                            gooseConvert,
                            rawFile,
                            gooseOut);

                        result = ExecuteProcess.ExecuteCommand(gooseCommand, true, true);

                        //just use plain text
                        File.Copy(gooseOut, gmiFile, true);
                    }
                    else
                    {
                        if (settings.WebRenderMode == "web-switch-simplified")
                        {
                            //pass options to goose to get main content as html
                            gooseCommand = string.Format(
                                "\"{0}\" -i \"{1}\" -o \"{2}\"",
                                gooseConvert,
                                rawFile,
                                gooseOut);

                            //get main html of the page
                            result = ExecuteProcess.ExecuteCommand(gooseCommand, true, true);

                            htmlPath = gooseOut;
                        }
                        else
                        {
                            //no filtering to extract main content
                            htmlPath = rawFile;
                        }

                        //convert to gmi
                        var htmlToGmiResult = ConverterService.HtmlToGmi(htmlPath, gmiFile);

                        if (htmlToGmiResult.Item1 != 0)
                        {
                            mMainWindow.ToastNotify("Could not render HTML as GMI: " + fullQuery, ToastMessageStyles.Error);
                            mMainWindow.ToggleContainerControlsForBrowser(true);
                            e.Cancel = true;
                            return;
                        }
                    }
                }
                else if (httpResponse.ContentType.Contains("text/"))
                {
                    //convert plain text to a http version (wraps it in a preformatted section)
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

                    if (httpResponse.ContentType.Contains("image/"))
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

                if (httpResponse.Redirected)
                {
                    string redirectUri = fullQuery;

                    redirectUri = httpResponse.FinalUrl;
                    //redirected to a full http url
                    httpUri = redirectUri;

                    //regenerate the hashes using the redirected target url
                    hash = HashService.GetMd5Hash(httpUri);

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
                    httpUri = fullQuery;
                }

                var userThemesFolder = ResourceFinder.LocalOrDevFolder(appDir, @"GmiConverters\themes", @"..\..\GmiConverters\themes");

                var userThemeBase = Path.Combine(userThemesFolder, settings.Theme);

                mMainWindow.ShowUrl(httpUri, gmiFile, htmlFile, userThemeBase, siteIdentity, e);
            }
            else if (httpResponse.StatusCode == 404)
            {
                mMainWindow.ToastNotify("Page not found (status 51)\n\n" + e.Uri.ToString(), ToastMessageStyles.Warning);
            }
            else
            {
                //some othe error - show to the user for info
                mMainWindow.ToastNotify(string.Format(
                    "Cannot retrieve the content (exit code {0}): \n\n{1} \n\n{2}",
                    result.Item1,
                    string.Join("\n\n", httpResponse.Info),
                    string.Join("\n\n", httpResponse.Errors)
                    ),
                    ToastMessageStyles.Error);
            }

            mMainWindow.ToggleContainerControlsForBrowser(true);

            //no further navigation right now
            e.Cancel = true;
        }
    }
}
