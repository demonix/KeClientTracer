using System;
using System.Security.Cryptography.X509Certificates;
using System.Web.Caching;
using LogProcessors.AuthRegistratorHelper;
using LogProcessors.CertificateHelper;

namespace LogProcessors.Caches
{
    /*public static class CertificateCache
    {
        private static Cache _mainCache = new Cache(new NullBackingStore(), new CachingInstrumentationProvider("certCache", false, false, "default"));
        static SlidingTime _slidingTimeExpiration = new SlidingTime(TimeSpan.FromMinutes(30));
        static AuthRegistratorClient arClient = new AuthRegistratorClient();
        public static OrganizationCertificateDescription Get(string thumbprint)
        {
            OrganizationCertificateDescription result = (OrganizationCertificateDescription)_mainCache.GetData(thumbprint);
            if (result == null)
            {
                Console.Out.WriteLine("th {0} not in cache", thumbprint);
                try
                {
                    result = new OrganizationCertificateDescription(new X509Certificate(arClient.GetCertificate(thumbprint)));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                if (result != null)
                    _mainCache.Add(thumbprint, result, CacheItemPriority.Normal, null, _slidingTimeExpiration);


            }
            else
            { Console.Out.WriteLine("th {0} in cache", thumbprint); }
            return result;
        }
        //private static Cache _mainCache = new Cache(new NullBackingStore(),new CachingInstrumentationProvider("mainCache",false,false,"logger"));
    }*/
}