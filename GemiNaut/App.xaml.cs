//===================================================
//    GemiNaut, a friendly browser for Gemini space on Windows
//    (and for related plucked instruments).

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

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GemiNaut.Singletons;

namespace GemiNaut
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string[] Args;

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Session.Instance.Dispose();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // If no command line arguments were provided, don't process them if (e.Args.Length == 0) return;  
            if (e.Args.Length > 0)
            {
                Args = e.Args;
            }
        }
    
    }
}
