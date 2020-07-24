using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GemiNaut.Singletons;
using Jdenticon;


namespace GemiNaut
{
    public class SiteIdentity
    {
        private Uri mUri;
        private Session  mSession;

        public SiteIdentity(Uri uri, Session session)
        {
            mUri = uri;
            mSession = session;

            CreateIdenticon(GetId());

        }


        public static string GetMd5Hash(string input)
        {

            string hash;
            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();
            //regenerate the hashes using the redirected target url
            using (MD5 md5 = MD5.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(input));


                // Loop through each byte of the hashed data
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
            }
            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        static string ReplaceFirst(string text, string search, string replace)
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

            return (siteId);
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
            return (Path.Combine(sessionPath, GetId() + ".png"));
        }
        public string FabricImagePath()
        {
            var sessionPath = Session.Instance.SessionPath;
            return (Path.Combine(sessionPath, ReverseString(GetId()) + ".png"));
        }

        public string GetId()
        {
            return( GetMd5Hash(GetThemeId()));
        }

        public void CreateIdenticon(string identifier)
        {

            //these are fine tuned. Fabric should be not too light, as there is quite a bit of white space in the design

            //identicon
            var identiconId = identifier;
            var identiconStyle = new IdenticonStyle
            {
                ColorLightness = Range.Create(0.22f, 0.75f),
                GrayscaleLightness = Range.Create(0.52f, 0.7f),
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
                ColorLightness = Range.Create(0.33f, 0.48f),
                GrayscaleLightness = Range.Create(0.30f, 0.45f),
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
                        if ("" != pathParts[count + 1].ToString())
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

            return (siteId);

        }


    }
}
