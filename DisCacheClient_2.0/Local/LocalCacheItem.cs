using Microsoft.Extensions.Caching.Distributed;
using System;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace DisCacheClient.Local
{

    public class LocalCacheOptions
    {
        public bool UseCache { get; set; }
        public int SecondsLocal { get; set; }
        public int SecondsRemote { get; set; }
    }

    public class LocalCacheItem<T> 
    {
        //private ILogger<LocalCacheItem<T>> _logger;
        private LocalCacheOptions _options;
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;

        public LocalCacheItem (
           
            IMemoryCache memoryCache,
            IDistributedCache distributedCache,
            LocalCacheOptions options)
        {
            //_logger = logger;
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
            _options = options;
        }

        public T Get(string key)
        {

            if (!_options.UseCache)
                return default(T);

            if (_options.SecondsLocal == 0 && _options.SecondsRemote == 0)
                return default(T);

            try
            {
                string cacheKey = key;

                if (_options.SecondsLocal > 0)
                {
                    if (_memoryCache.TryGetValue<string>(cacheKey, out var cacheVal))
                    {
                        if (!string.IsNullOrEmpty(cacheVal))
                        {
                            return JsonConvert.DeserializeObject<T>(cacheVal);
                        }
                    }
                }

                if (_options.SecondsRemote > 0)
                {
                    var val = _distributedCache.GetString(cacheKey);

                    if (!string.IsNullOrEmpty(val))
                    {
                        var disRes = JsonConvert.DeserializeObject<T>(val);

                        if (_options.UseCache && _options.SecondsLocal>0)
                        {
                            _memoryCache.Set(cacheKey, disRes, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(_options.SecondsLocal)));
                        }

                        return disRes;
                    }
                }
            }
            catch (Exception)
            {
            }

            return default(T);
        }

        public void Add(string key, T mandant)
        {
            if (!_options.UseCache)
                return;

            if (_options.SecondsLocal == 0 && _options.SecondsRemote == 0)
                return;

            if (_options.SecondsLocal > 0)
            {
                var toCache =
                    JsonConvert.DeserializeObject<T>(
                        JsonConvert.SerializeObject(mandant));
                _memoryCache.Set(key, toCache, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(_options.SecondsLocal)));
            }
            if (_options.SecondsRemote > 0)
            {
                _distributedCache.SetString(key, JsonConvert.SerializeObject(mandant),
                    new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpirationRelativeToNow =
                            TimeSpan.FromSeconds(_options.SecondsRemote)
                    }
                );
            }
        }

        public void Invalidate(string key)
        {

            if (!_options.UseCache)
            {
                return; 
            }

            if(_options.SecondsLocal > 0)
            {
                _memoryCache.Remove(key);
            }

            if (_options.SecondsRemote > 0)
            {
                _distributedCache.Remove(key);
            }
        }
    }
}
