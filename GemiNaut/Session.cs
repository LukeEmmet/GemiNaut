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

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace GemiNaut.Singletons
{
    /// <summary>
    /// simple singleton logger, based on
    /// http://csharpindepth.com/Articles/General/Singleton.aspx#cctor
    /// </summary>
    public class Session : ObservableObject, IDisposable
    {
        private static readonly Session instance = new Session();

        private string _sessionPath;
        private CertificatesManager _certificates;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static Session()
        {
        }

        private Session()
        {
            CreateSessionFolder();
            _certificates =  new CertificatesManager();
        }

        public static Session Instance
        {
            get
            {
                return instance;
            }
        }

        private void CreateSessionFolder()
        {
            _sessionPath = GetTemporaryDirectory("geminaut_");
        }

        private static string GetTemporaryDirectory(string prefix)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), prefix + Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        private void DestroySessionFolder()
        {
            //assumes the any file viewers do not retain a lock on any files therein
            GC.Collect();

            //**TBD would be nice to handle this gracefully by marking
            //any files or folder for deletion on reboot...

            //check folder exists - it might have been deleted on a separate thread on closing down
            if (Directory.Exists(_sessionPath)) {

                try
                {
                    Directory.Delete(_sessionPath, true);
                } catch (Exception e)
                {
                    //**log this maybe ??
                }
            }
        }

        public string SessionPath
        {
            get { return _sessionPath; }
        }

        public CertificatesManager CertificatesManager
        {
            get {
                    return _certificates;
                }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            DestroySessionFolder();
        }
    }
}
