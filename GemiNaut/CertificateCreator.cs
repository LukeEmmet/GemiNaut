using System;
using Pluralsight.Crypto;
using System.Security.Cryptography.X509Certificates;


namespace GemiNaut
{
    public class CertificateCreator
    {
        public static X509Certificate2 GenSelfSignedCert(string commonName, DateTime validFrom, DateTime validTo)
        {
            using (CryptContext ctx = new CryptContext())
            {
                ctx.Open();

                X509Certificate2 cert = ctx.CreateSelfSignedCertificate(
                    new SelfSignedCertProperties
                    {
                        IsPrivateKeyExportable = true,
                        KeyBitLength = 4096,
                        Name = new X500DistinguishedName("CN=" + commonName),
                        ValidFrom = validFrom,
                        ValidTo = validTo,
                    });

                
                return cert;

            }
        }
    }
}
