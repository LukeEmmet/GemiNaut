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
using System.Collections.Generic;
using System.IO;
using GemiNaut.Singletons;
using Jdenticon;

namespace GemiNaut
{
    public class SiteIdentity
    {
        private readonly Uri mUri;
        private readonly Session mSession;

        public SiteIdentity(Uri uri, Session session)
        {
            mUri = uri;
            mSession = session;

            CreateIdenticon(GetId());
        }

        private static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public string GetThemeId()
        {
            // for the purposes of themeing, the theme id is same as site id except
            // ~foo is treated as same as /users/foo, for consistency
            // in case links are made to both uris

            var siteId = GetSiteId();

            siteId = siteId.Replace("/~", "/users/");
            siteId = siteId.Replace("/users/users/", "/users/"); //in case for some reason path was of the form / users / ~foo

            if (mUri.Scheme == "gopher")
            {
                siteId = ReplaceFirst(siteId, "/1/", "/");    // dont use leading item type to drive the theme id -now the user gets same theme in gemini and gopher on the same serve
            }

            return siteId;
        }

        public static string ReverseString(string s)
        {
            char[] arr = s.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        public string IdenticonImagePath()
        {
            var sessionPath = Session.Instance.SessionPath;
            return Path.Combine(sessionPath, GetId() + ".png");
        }
        public string FabricImagePath()
        {
            var sessionPath = Session.Instance.SessionPath;
            return Path.Combine(sessionPath, ReverseString(GetId()) + ".png");
        }

        public string GetId()
        {
            return HashService.GetMd5Hash(GetThemeId());
        }

        public void CreateIdenticon(string identifier)
        {
            //these are fine tuned. Fabric should be not too light, as there is quite a bit of white space in the design

            //identicon
            var identiconId = identifier;
            var identiconStyle = new IdenticonStyle
            {
                ColorLightness = Jdenticon.Range.Create(0.22f, 0.75f),
                GrayscaleLightness = Jdenticon.Range.Create(0.52f, 0.7f),
                GrayscaleSaturation = 0.10f,
                ColorSaturation = 0.75f,
                Padding = 0f
            };

            var identicon = Identicon.FromValue(identiconId, size: 80);
            identicon.Style = identiconStyle;

            if (!File.Exists(IdenticonImagePath()))
            {
                identicon.SaveAsPng(IdenticonImagePath());
            }

            //fabric
            var fabricId = ReverseString(identifier);
            var fabricStyle = new IdenticonStyle
            {
                ColorLightness = Jdenticon.Range.Create(0.33f, 0.48f),
                GrayscaleLightness = Jdenticon.Range.Create(0.30f, 0.45f),
                ColorSaturation = 0.7f,
                GrayscaleSaturation = 0.7f,
                Padding = 0f
            };

            var fabricIcon = Identicon.FromValue(fabricId, size: 13);
            fabricIcon.Style = fabricStyle;

            if (!File.Exists(FabricImagePath()))
            {
                fabricIcon.SaveAsPng(FabricImagePath());
            }
        }

        public string GetSiteId()
        {
            var uri = mUri;

            var siteId = uri.Authority;

            var pageScheme = uri.Scheme;

            char[] array = new char[2];
            array[0] = '/';
            var pathParts = uri.LocalPath.Split(array);

            var keepParts = new List<string>();

            var count = 0;

            //try to guess the user path, if any by looking for "/users/foo" or "/~foo"
            if ((uri.LocalPath.Contains("/~")) || (uri.LocalPath.Contains("/users/")))
            {
                foreach (var pathPartOriginal in pathParts)
                {
                    var pathPart = pathPartOriginal;

                    //special treatment of the first component of gopher paths - ensure site home is accessied as type 1
                    //e.g. /1/foo/bar not /n/foo/bar even if n is the current page selector
                    if (count == 1 && pageScheme == "gopher")
                    {
                        if (pathPart != "1") { pathPart = "1"; }
                    }

                    if (pathPart == "users")
                    {
                        if (pathParts[count + 1] != "")
                        {
                            keepParts.Add(pathPart);
                            keepParts.Add(pathParts[count + 1].ToString());
                        }
                        break;
                    }

                    if (0 < pathPart.Length && pathPart.Substring(0, 1) == "~")
                    {
                        keepParts.Add(pathPart);
                        break;
                    }

                    keepParts.Add(pathPart);

                    count++;
                }

                var usePath = string.Join("/", keepParts.ToArray());

                siteId = uri.Authority + usePath;
            }

            return siteId;
        }
    }
}
