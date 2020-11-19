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
using System.IO;

namespace GemiNaut
{
    public static class AppInit
    {
        public static void UpgradeSettings()
        {
            //can also use 
            //Settings.Default.GetPreviousVersion for more finegrained control if necessary

            if (Settings.Default.UpgradeNeeded)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeNeeded = false;
                Settings.Default.Save();
            }
        }

        public static void CopyAssets()
        {
            var sessionPath = Session.Instance.SessionPath;
            var appDir = System.AppDomain.CurrentDomain.BaseDirectory;
            var finder = new ResourceFinder();

            var assetsTarget = Path.Combine(sessionPath, "Assets");

            if (!Directory.Exists(assetsTarget)) { Directory.CreateDirectory(assetsTarget); }

            var assetsFolder = ResourceFinder.LocalOrDevFolder(appDir, @"GmiConverters\Themes\Assets", @"..\..\..\GmiConverters\Themes\Assets");

            foreach (var file in Directory.GetFiles(assetsFolder))
            {
                File.Copy(file, Path.Combine(assetsTarget, Path.GetFileName(file)), true);
            }
        }
    }
}
