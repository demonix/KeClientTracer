using System;
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
                result = new OrganizationCertificateDescription(new X509Certificate(certificateData));
                HttpRuntime.Cache.Add(thumbprint, result, null, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(30), CacheItemPriority.Normal, null);
            }

            return result;
        }
    }
}