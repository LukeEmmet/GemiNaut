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


namespace GemiNaut
{
    static class UriTester
    {

        public static bool TextIsUri(string text)
        {
            Uri outUri;
            return (Uri.TryCreate(text, UriKind.Absolute, out outUri));
        }

        static public int DefaultPort(Uri uri)
        {

            var port = -1;

            if (uri.Scheme == "gemini") { port = 1965; }
            if (uri.Scheme == "gopher") { port = 70; }
            if (uri.Scheme == "http") { port = 80; }
            if (uri.Scheme == "https") { port = 443; }

            return (port);
        }

        static public Uri NormaliseUri(Uri uri)
        {

            UriBuilder outUri = new UriBuilder();

            outUri.Scheme = uri.Scheme;
            outUri.Host = uri.Host;

            //if the port is the default one, dont include it
            if (uri.Port != DefaultPort(uri)) { outUri.Port = uri.Port; }
            outUri.Path = uri.AbsolutePath;

            if (uri.Query.Length > 1) { outUri.Query = uri.Query.Substring(1); }

            return outUri.Uri;
        }
    }
}
