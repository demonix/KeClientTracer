namespace LogProcessors.Caches
{
    /*internal static class TokenCache
    {
        private static Cache _mainCache = new Cache(new NullBackingStore(), new CachingInstrumentationProvider("tokenCache", false, false, "default"));
        static SlidingTime _slidingTimeExpiration = new SlidingTime(TimeSpan.FromMinutes(30));

        public static Token Get(string token)
        {
            Token result = (Token)_mainCache.GetData(token);
            if (result == null)
            {
                try
                {
                    result = TokenUtilities.FromString(token, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                if (result != null)
                    _mainCache.Add(token, result, CacheItemPriority.Normal, null, _slidingTimeExpiration);


            }
            return result;
        }
    }*/
}