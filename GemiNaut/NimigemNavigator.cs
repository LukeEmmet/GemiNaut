//===================================================
//    GemiNaut, a friendly browser for Nimigem space on Windows

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
using System;
using System.Windows.Controls;
using SmolNetSharp.Protocols;
using static GemiNaut.Views.MainWindow;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace GemiNaut
{
 

    public class NimigemNavigator
    {
        private readonly MainWindow mMainWindow;
        private readonly WebBrowser mWebBrowser;

        public NimigemNavigator(MainWindow mainWindow, WebBrowser browserControl)
        {
            mMainWindow = mainWindow;
            mWebBrowser = browserControl;
        }


        private void HandleInvalidResponse(NimigemResponse nimigemResponse)
        {
            //for out of specification responses - raise an error
            throw new Exception(String.Format("Invalid Nimigem response: {0}{1} {2}",
                                    nimigemResponse.codeMajor, nimigemResponse.codeMinor, nimigemResponse.meta));
        }


        public void NavigateNimigemScheme(string fullQuery, System.Windows.Navigation.NavigatingCancelEventArgs e, string payload, bool requireSecure = true)
        {
            var NimigemUri = e.Uri.OriginalString;

            //at present we only support UTF8 plain text payloads
            byte[] nimigemBody = Encoding.UTF8.GetBytes(payload);
            var mime = "text/plain; charset=utf-8";

            var settings = new UserSettings();

            var uri = new Uri(fullQuery);
            //use a proxy for any other scheme that is not Nimigem
            var proxy = "";     //use none

            var connectInsecure = false;
            if (uri.Host == "localhost")
            {
                //to support local testing servers, dont require secure connection on localhost
                //**FIX ME, or have an option
                connectInsecure = true;
            }

            X509Certificate2 certificate;

            certificate = Session.Instance.CertificatesManager.GetCertificate(uri.Host);

            try
            {
                NimigemResponse nimigemResponse;
                try
                {
                    nimigemResponse = (NimigemResponse)Nimigem.Fetch(uri, nimigemBody, mime, certificate, proxy, connectInsecure, settings.MaxDownloadSizeMb * 1024, settings.MaxDownloadTimeSeconds);

                }
                catch (Exception err)
                {

                    //warn, but continue if there are server validation errors
                    //in these early days of Nimigem we dont forbid visiting a site with an expired cert or mismatched host name
                    //but we do give a warning each time
                    if (err.Message == "The remote certificate was rejected by the provided RemoteCertificateValidationCallback.")
                    {

                        mMainWindow.ToastNotify("Note: " + err.Message + " for: " + e.Uri.Authority, ToastMessageStyles.Warning);

                        //try again insecure this time
                        nimigemResponse = (NimigemResponse)Nimigem.Fetch(uri, nimigemBody, mime, certificate, proxy, connectInsecure, settings.MaxDownloadSizeMb * 1024, settings.MaxDownloadTimeSeconds);

                    }
                    
                    else
                    {
                        //reraise
                        throw;
                    }
                }

                if (nimigemResponse.codeMajor == '1')
                {
                    //invalid in nimigem
                    HandleInvalidResponse(nimigemResponse);
                }

                else if (nimigemResponse.codeMajor == '2')
                {
                    //success
                    if (nimigemResponse.codeMinor == '5')
                    {
                        //valid submission - get the new target to retrieve
                        mMainWindow.ToastNotify(String.Format("Submit successful: retrieving result: {0}", nimigemResponse.meta));

                        var successUri = new Uri(nimigemResponse.meta);

                        //must be a response redirect to gemini URL
                        if (successUri.Scheme != "gemini")
                        {
                            HandleInvalidResponse(nimigemResponse);
                        }


                        var geminiTarget = new GeminiNavigator(mMainWindow, mMainWindow.BrowserControl);
                        var normalisedUri = UriTester.NormaliseUri(successUri);

                        var siteIdentity = new SiteIdentity(normalisedUri, Session.Instance);

                        geminiTarget.NavigateGeminiScheme(successUri.OriginalString, e, siteIdentity);

                    }
                    else
                    {
                        //no other 2X responses are valid 
                        HandleInvalidResponse(nimigemResponse);
                    }
                }

                // codemajor = 3 is redirect - should eventually end in success or raise an error

                else if (nimigemResponse.codeMajor == '4')
                {
                    //same as normal Gemini
                    mMainWindow.ToastNotify("Temporary failure (status 4X)\n\n" +
                        nimigemResponse.meta + "\n\n" + 
                        e.Uri.ToString(), ToastMessageStyles.Warning);
                }
                else if (nimigemResponse.codeMajor == '5')
                {
                    //same as normal Gemini
                    if (nimigemResponse.codeMinor == '1')
                    {
                        mMainWindow.ToastNotify("Page not found\n\n" + e.Uri.ToString(), ToastMessageStyles.Warning);
                    }
                    else
                    {
                        mMainWindow.ToastNotify("Permanent failure (status 5X)\n\n" + 
                            nimigemResponse.meta + "\n\n" + 
                            e.Uri.ToString(), ToastMessageStyles.Warning);

                    }
                }
                else if (nimigemResponse.codeMajor == '6')
                {
                    mMainWindow.ToastNotify("Certificate required. Choose one and try again.\n\n" + e.Uri.ToString(), ToastMessageStyles.Warning);

                }

                else
                {
                    mMainWindow.ToastNotify("Unexpected output from server " +
                        "(status " + nimigemResponse.codeMajor + "." + nimigemResponse.codeMinor + ") " +
                        nimigemResponse.meta + "\n\n"
                        + e.Uri.ToString(), ToastMessageStyles.Warning);

                }

            }
            catch (Exception err)
            {
                //generic handler for other runtime errors
                mMainWindow.ToastNotify("Error getting Nimigem content for " + e.Uri.ToString() + "\n\n" + err.Message, ToastMessageStyles.Warning);
            }


            //make the window responsive again
            mMainWindow.ToggleContainerControlsForBrowser(true);

            //no further navigation right now
            e.Cancel = true;

        }

    }
}
