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
