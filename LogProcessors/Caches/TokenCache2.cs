using System;
using System.Web;
using System.Web.Caching;
using LogProcessors.TokenHelper;

namespace LogProcessors.Caches
{
    internal static class TokenCache2
    {
        //static Cache _mainCache = new Cache();
        public static Token Get(string token)
        {
           
            Token result = (Token) HttpRuntime.Cache[token];
            
            if (result == null)
            {
                
                    result = TokenUtilities.FromString(token, true);
              
                if (result != null)
                    //_mainCache.Add(token, result, CacheItemPriority.Normal, null, _slidingTimeExpiration);
                    HttpRuntime.Cache.Add(token, result, null, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(30), CacheItemPriority.Normal, null);
            }
            return result;
        }
    }
}