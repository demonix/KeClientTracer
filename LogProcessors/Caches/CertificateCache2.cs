using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.Caching;
using LogProcessors.AuthRegistratorHelper;
using LogProcessors.CertificateHelper;

namespace LogProcessors.Caches
{
    public static class CertificateCache2
    {
        static AuthRegistratorClient arClient = new AuthRegistratorClient();
        public static OrganizationCertificateDescription Get(string thumbprint)
        {
            OrganizationCertificateDescription result = (OrganizationCertificateDescription)HttpRuntime.Cache[thumbprint];
            if (result == null)
            {
                byte[] certificateData = arClient.GetCertificate(thumbprint);
                if (certificateData == null)
                    return null;
                X509Certificate x509Certificate ;
                try
                {
                    x509Certificate = new X509Certificate(certificateData);
                }
                catch (CryptographicException ex)
                {
                    throw new Exception("Error while creating cert from " + certificateData.Length + " bytes data", ex);
                }
                
                result = new OrganizationCertificateDescription(x509Certificate);
                HttpRuntime.Cache.Add(thumbprint, result, null, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(30), CacheItemPriority.Normal, null);
            }

            return result;
        }
    }
}