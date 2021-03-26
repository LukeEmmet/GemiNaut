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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace GemiNaut
{
    public class CertificatesManager
    {
        private Dictionary<string, X509Certificate2> _certificates;                //certificates by thumbprint
        private Dictionary<string, string> _mappings;     //domains as keys and certificate thumbprint as the content

        public CertificatesManager()
        {
            Clear();

            //automatically load certificates from the user profile that do not need a password
            var settings = new UserSettings();
            AutoLoadUserCertificates(settings.ClientCertificatesFolder, "");
        }

        /// <summary>
        /// Automatically load all *.pfx and *.p12 certificates in some folder, that may be loaded with some default password (typically ""). 
        /// Other certificates will need to be loaded individually and passwords provided
        /// </summary>
        /// <param name="certsFolder"></param>
        /// <returns>returns an array of any certs that could not be automatically loaded without the password</returns>
        private List<string> AutoLoadUserCertificates(string certsFolder, string defaultPassword)
        {
            var result = new List<string>();
            foreach (var fileName in Directory.GetFiles(certsFolder))
            {
                X509Certificate2 cert;
                var ext = Path.GetExtension(fileName).ToLower();
                if (ext == ".pfx" || ext == ".p12")
                {
                    var certFile = Path.Combine(certsFolder, fileName);
                    try
                    {
                        //load the certificate with the default password
                        cert = new X509Certificate2(certFile, defaultPassword);
                        if (!_certificates.ContainsKey(cert.Thumbprint))
                        {
                            AddCertificate(cert);
                        }
                    }
                    catch (CryptographicException)
                    {
                        //could not load - add to the list of ones we will return
                        result.Add(fileName);
                    }
                    
                }
            }

            return result;
        }

        public Dictionary<string, X509Certificate2> Certificates
        {
            get
            {
                return _certificates;
            }
        }

        public Dictionary<string, string> Mappings
        {
            get
            {
                return _mappings;
            }
        }

        public void AddCertificate(X509Certificate2 cert)
        {
            _certificates.Add(cert.Thumbprint, cert);
        }

        public void RegisterMapping(string domain, string thumbprint)
        {
            _mappings[domain] = thumbprint;
        }

        public void UnRegisterMapping(string domain)
        {
            if (_mappings.ContainsKey(domain))
            {
                _mappings.Remove(domain);
            }
        }

        public X509Certificate2 GetCertificate(string domain)
        {
            //look up the certificate, or return null if none available for the domain
            if (_mappings.ContainsKey(domain))
            {
                if (_certificates.ContainsKey(_mappings[domain]))
                {
                    return _certificates[_mappings[domain]];
                } else
                {
                    return null;
                }
            } else
            {
                return null;
            }
        }

        public void Clear()
        {
            _certificates = new Dictionary<string, X509Certificate2>();
            _mappings = new Dictionary<string, string>();
        }

    }
}
