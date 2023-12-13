using DisCacheClient.Exceptions;
using DisCacheClient.Extensions.Caching.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;

namespace DisCacheClient.Local
{

    public class LocalCacheOptions
    {
        public bool UseCache { get; set; }
        public int SecondsLocal { get; set; }
        public int SecondsRemote { get; set; }
    }


    public class LocalCacheItem<T> : IDisposable
    {
        //private ILogger<LocalCacheItem<T>> _logger;
        private readonly LocalCacheOptions _options;
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;

        public LocalCacheItem(

            IMemoryCache memoryCache,
            IDistributedCache distributedCache,
            LocalCacheOptions options)
        {
            //_logger = logger;
            _options = options;
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;

            if (distributedCache is Extensions.Caching.DisCacheClient cc)
            {
                cc.DisCDelete += DistributedCacheOnDelete;
                cc.DisCUpdate += DistributedCacheOnDelete;
            }

        }

        private void DistributedCacheOnDelete(object sender, string key)
        {
            _memoryCache?.Remove(key);
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
                            // It's json in memory cache. Deserialose.
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

                        if (_options.UseCache && _options.SecondsLocal > 0)
                        {
                            // I put the json in local cache. 
                            _memoryCache.Set(cacheKey, val, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(_options.SecondsLocal)));
                        }

                        return disRes;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DiscException("Error getting cached item. Inner exception for details.", ex);
            }

            return default(T);
        }

        public async Task<T> GetAsync(string key)
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
                    var val = await _distributedCache.GetStringAsync(cacheKey);

                    if (!string.IsNullOrEmpty(val))
                    {
                        var disRes = JsonConvert.DeserializeObject<T>(val);

                        if (_options.UseCache && _options.SecondsLocal > 0)
                        {
                            _memoryCache.Set(cacheKey, val, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(_options.SecondsLocal)));
                        }

                        return disRes;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DiscException("Error getting cached item. Look in inner exception for details.", ex);
            }

            return default(T);
        }

        public void Add(string key, T item)
        {
            if (!_options.UseCache)
                return;

            if (_options.SecondsLocal == 0 && _options.SecondsRemote == 0)
                return;

            if (_options.SecondsLocal > 0)
            {
                var toCache =
                    JsonConvert.DeserializeObject<T>(
                        JsonConvert.SerializeObject(item));
                _memoryCache.Set(key, toCache, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(_options.SecondsLocal)));
            }
            if (_options.SecondsRemote > 0)
            {
                _distributedCache.SetString(key, JsonConvert.SerializeObject(item),
                    new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpirationRelativeToNow =
                            TimeSpan.FromSeconds(_options.SecondsRemote)
                    }
                );
            }
        }

        public async Task AddAsync(string key, T item, CancellationToken ct = default)
        {
            if (!_options.UseCache)
                return;

            if (_options.SecondsLocal == 0 && _options.SecondsRemote == 0)
                return;

            if (_options.SecondsLocal > 0)
            {
                var toCache =
                    JsonConvert.DeserializeObject<T>(
                        JsonConvert.SerializeObject(item));
                _memoryCache.Set(key, toCache, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(_options.SecondsLocal)));
            }
            if (_options.SecondsRemote > 0)
            {
                await _distributedCache.SetStringAsync(key, JsonConvert.SerializeObject(item),
                    new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpirationRelativeToNow =
                            TimeSpan.FromSeconds(_options.SecondsRemote)
                    },
                    ct
                );
            }
        }

        public void Invalidate(string key)
        {

            if (!_options.UseCache)
            {
                return;
            }

            if (_options.SecondsLocal > 0)
            {
                _memoryCache.Remove(key);
            }

            if (_options.SecondsRemote > 0)
            {
                _distributedCache.Remove(key);
            }
        }

        public async Task InvalidateAsync(string key)
        {

            if (!_options.UseCache)
            {
                return;
            }

            if (_options.SecondsLocal > 0)
            {
                _memoryCache.Remove(key);
            }

            if (_options.SecondsRemote > 0)
            {
                await _distributedCache.RemoveAsync(key);
            }
        }

        public void Dispose()
        {
            if (_distributedCache is Extensions.Caching.DisCacheClient cc)
            {
                cc.DisCDelete -= DistributedCacheOnDelete;
                cc.DisCUpdate -= DistributedCacheOnDelete;
            }
        }
    }
}
