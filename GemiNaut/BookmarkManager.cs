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
using GemiNaut.Views;
using mshtml;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using static GemiNaut.Views.MainWindow;

namespace GemiNaut
{
    public class BookmarkManager
    {
        private readonly MainWindow mMainWindow;
        private readonly WebBrowser mWebBrowser;

        public BookmarkManager(MainWindow window, WebBrowser browser)
        {
            mMainWindow = window;
            mWebBrowser = browser;
        }

        public static string[] BookmarkLines()
        {
            var settings = new UserSettings();
            string[] array = new string[2];
            array[0] = "\r\n";

            return settings.Bookmarks.Split(array, StringSplitOptions.RemoveEmptyEntries);
        }

        public void AddBookmark(string url, string title)
        {
            var settings = new UserSettings();

            var doc = (HTMLDocument)mWebBrowser.Document;

            foreach (var line in BookmarkLines())
            {
                var linkParts = ParseGeminiLink(line);
                if (linkParts[0] == url)
                {
                    //already exists
                    mMainWindow.ToastNotify("That URL is already in the bookmarks, skipping.\n" + url, ToastMessageStyles.Warning);
                    return;
                }
            }

            //a new one
            settings.Bookmarks += "\r\n" + "=> " + url + "  " + doc.title;
            settings.Save();
            RefreshBookmarkMenu();
            mMainWindow.ToastNotify("Bookmark added: " + (title + " " + url).Trim(), ToastMessageStyles.Success);
        }

        public void RefreshBookmarkMenu()
        {
            var mnuBookMarks = mMainWindow.mnuBookMarks;
            RoutedEventHandler clickHandler = mMainWindow.mnuMenuBookmarksGo_Click;

            mnuBookMarks.Items.Clear();

            foreach (var line in BookmarkLines())
            {
                var bmMenu = new MenuItem();

                var linkParts = ParseGeminiLink(line);
                if (UriTester.TextIsUri(linkParts[0]))
                {
                    bmMenu.CommandParameter = linkParts[0];
                    bmMenu.Header = linkParts[1];
                    bmMenu.ToolTip = linkParts[0];

                    bmMenu.Click += clickHandler;

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
        public static string[] ParseGeminiLink(string line)
        {
            var linkRegex = new Regex(@"\s*=>\s([^\s]*)(.*)");
            string[] array = new string[2];

            if (linkRegex.IsMatch(line))
            {
                Match match = linkRegex.Match(line);
                array[0] = match.Groups[1].ToString().Trim();
                array[1] = match.Groups[2].ToString().Trim();

                //if display text is empty, use url
                if (array[1] == "")
                {
                    array[1] = array[0];
                }
            }
            else
            {
                //isnt a link, return null,null
                array[0] = null;
                array[1] = null;
            }

            return array;
        }
    }
}
