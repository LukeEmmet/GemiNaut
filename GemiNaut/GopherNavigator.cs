﻿//===================================================
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


namespace GemiNaut
{
    public class GopherNavigator
    {
        private readonly MainWindow mMainWindow;
        private readonly WebBrowser mWebBrowser;

        public GopherNavigator(MainWindow window, WebBrowser browser)
        {
            mMainWindow = window;
            mWebBrowser = browser;
        }

        public enum GopherParseTypes
        {
            Map, Text
        }

        //navigate to a url but get some user input first
        private void NavigateGopherWithInput(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            //position input box approx in middle of main window

            var windowCentre = WindowGeometry.WindowCentre(mMainWindow);
            var inputPrompt = "Input request from Gopher server\n\n" +
                "  " + e.Uri.Host + e.Uri.LocalPath + "\n\n" +
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
                var query = b.ToString() + "%09" + Uri.EscapeDataString(input);      //escape the query result;
                //ToastNotify(query);

                mWebBrowser.Navigate(query);
            }
            else
            {
                //dont do anything further with navigating the browser
                e.Cancel = true;
            }
        }



        public void NavigateGopherScheme(string fullQuery, System.Windows.Navigation.NavigatingCancelEventArgs e, SiteIdentity siteIdentity)
        {
            var sessionPath = Session.Instance.SessionPath;
            var appDir = AppDomain.CurrentDomain.BaseDirectory;

            //check if it is a query selector without a parameter
            if (!e.Uri.OriginalString.Contains("%09") && e.Uri.PathAndQuery.StartsWith("/7/"))
            {
                NavigateGopherWithInput(e);

                mMainWindow.ToggleContainerControlsForBrowser(true);

                //no further navigation right now
                e.Cancel = true;

                return;
            }

            var settings = new UserSettings();

            var hash = HashService.GetMd5Hash(fullQuery);

            //uses .txt as extension so content loaded as text/plain not interpreted by the browser
            //if user requests a view-source.
            var gopherFile = sessionPath + "\\" + hash + ".txt";
            var gmiFile = sessionPath + "\\" + hash + ".gmi";
            var htmlFile = sessionPath + "\\" + hash + ".htm";

            //delete txt file as GemGet seems to sometimes overwrite not create afresh
            File.Delete(gopherFile);

            //delete any existing html file to encourage webbrowser to reload it
            File.Delete(gmiFile);

            //get the content from Gopher using SmolNetSharp
            IResponse response;
            try
            {
                 response = Gopher.Fetch(new Uri(fullQuery), settings.MaxDownloadSizeMb * 1024, settings.MaxDownloadTimeSeconds);

                File.WriteAllBytes(gopherFile, response.bytes.ToArray());

            }
            catch (Exception err)
            {
            
                mMainWindow.ToastNotify(err.Message, ToastMessageStyles.Error);
                mMainWindow.ToggleContainerControlsForBrowser(true);    //reenable browser
                e.Cancel = true;
                return;
            }

            var mime = response.mime;
            Tuple<int, string, string> result;

            if (File.Exists(gopherFile))
            {
                string parseFile;

                if (mime == "application/gopher-menu")
                {
                    //convert gophermap to text/gemini

                    //ToastNotify("Converting gophermap to " + gmiFile);
                    result = ConverterService.GophertoGmi(gopherFile, gmiFile, fullQuery, GopherParseTypes.Map);
                    parseFile = gmiFile;
                }
                else if (mime == "text/plain")
                {
                    result = ConverterService.GophertoGmi(gopherFile, gmiFile, fullQuery, GopherParseTypes.Text);
                    parseFile = gmiFile;
                }
                else
                {
                    //a download

                    //copy to a file having its source extension
                    var pathFragment = (new UriBuilder(fullQuery)).Path;
                    var ext = Path.GetExtension(pathFragment);

                    var binFile = gopherFile + (ext ?? "");

                    File.Copy(gopherFile, binFile, true); //rename overwriting

                    if (mime == "image/png" || mime == "image/gif" || mime == "image/jpeg")
                    {
                        //show the image
                        mMainWindow.ShowImage(fullQuery, binFile, e);
                    }
                    else
                    {
                        //show a save as dialog
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

                if (!File.Exists(gmiFile) || result.Item1 != 0)
                {
                    mMainWindow.ToastNotify("Did not create expected GMI file for " + fullQuery + " in " + gmiFile, ToastMessageStyles.Error);
                    mMainWindow.ToggleContainerControlsForBrowser(true);
                    e.Cancel = true;
                }
                else
                {
                    var userThemesFolder = ResourceFinder.LocalOrDevFolder(appDir, @"GmiConverters\themes", @"..\..\..\GmiConverters\themes");

                    var userThemeBase = Path.Combine(userThemesFolder, settings.Theme);

                    mMainWindow.ShowUrl(fullQuery, parseFile, htmlFile, userThemeBase, siteIdentity, e);
                }
            }
        }
    }
}
