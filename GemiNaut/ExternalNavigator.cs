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

using GemiNaut.Views;

namespace GemiNaut
{
    public class ExternalNavigator
    {
        private readonly MainWindow mMainWindow;

        public ExternalNavigator(MainWindow Window)
        {
            mMainWindow = Window;
        }
        //launch url in system browser
        public void LaunchExternalUri(string uri)
        {
            System.Diagnostics.Process.Start(uri);
            mMainWindow.ToastNotify("Launching in system browser: " + uri);
        }
    }
}
