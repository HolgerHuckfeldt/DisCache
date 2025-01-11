using DisCacheClient.Local;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using WebApiDemo.Model;

namespace WebApiDemo.Cache
{
    public class CacheService
    {
        private readonly LocalCacheItem<TestClass> _testCache;

        public CacheService(IDistributedCache cache, IMemoryCache memCache)
        {
            _testCache = new LocalCacheItem<TestClass>(memCache, cache, new LocalCacheOptions
            {
                UseCache = true,
                SecondsLocal = 5,
                SecondsRemote = 16000,
                Subscribe = "_test*"
            });
        }

        private readonly string _testKey = "_testKey";

        public async Task<TestClass?> GetDocumentAsync(int id)
        {
            string cacheKey = $"{_testKey}_{id}";

            return await _testCache.GetAsync(cacheKey);
        }

        public async Task AddAsync(TestClass? s)
        {
            if (s == default)
            {
                return;
            }

            string cacheKey = $"{_testKey}_{s.Id}";
            await _testCache.AddAsync(cacheKey, s);
        }

    }
}
