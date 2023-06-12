using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DisCache.Services
{
    public class CacheCore : IDistributedCache
    {
        private readonly IDistributedCache _service;
        private readonly IMemoryCache _memoryCache;

        public CacheCore(IDistributedCache service, IMemoryCache memoryCache)
        {
            _service = service;
            _memoryCache = memoryCache;
        }

        public byte[] Get(string key)
        {
            byte[] returnData = null;

            if (Configuration.UseMemoryCacheAccelerator)
            {
                // Wenn der InMemory-Accelerator aktiviert ist, erst mal darein schauen.
                returnData = _memoryCache.Get(key) as byte[];
            }

            returnData ??= _service.Get(key);

            return returnData;
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = new CancellationToken())
        {
            byte[] returnData = null;

            if (Configuration.UseMemoryCacheAccelerator)
            {
                // Wenn der InMemory-Accelerator aktiviert ist, erst mal darein schauen.
                returnData = _memoryCache.Get(key) as byte[];
            }

            returnData ??= await _service.GetAsync(key, token);

            return returnData;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {

            if (Configuration.UseMemoryCacheAccelerator)
            {
                _memoryCache.Set(key, value,
                    new MemoryCacheEntryOptions()
                    {
                        AbsoluteExpiration = options.AbsoluteExpiration,
                        SlidingExpiration = options.SlidingExpiration
                    });
            }

            _service.Set(key, value, options);
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
            CancellationToken token = new CancellationToken())
        {
            if (Configuration.UseMemoryCacheAccelerator)
            {
                _memoryCache.Set(key, value,
                    new MemoryCacheEntryOptions()
                    {
                        AbsoluteExpiration = options.AbsoluteExpiration,
                        SlidingExpiration = TimeSpan.FromSeconds(Configuration.UseMemoryCacheAcceleratorTime)
                    });
            }

            await _service.SetAsync(key, value, options, token);
        }

        public void Refresh(string key)
        {
            if (Configuration.UseMemoryCacheAccelerator)
            {
                _memoryCache.TryGetValue(key, out var v);
            }

            _service.Refresh(key);
        }

        public async Task RefreshAsync(string key, CancellationToken token = new CancellationToken())
        {
            if (Configuration.UseMemoryCacheAccelerator)
            {
                _memoryCache.TryGetValue(key, out var v);
            }

            await _service.RefreshAsync(key, token);
        }

        public void Remove(string key)
        {
            if (Configuration.UseMemoryCacheAccelerator)
            {
                _memoryCache.Remove(key);
            }

            _service.Remove(key);
        }

        public async Task RemoveAsync(string key, CancellationToken token = new CancellationToken())
        {
            if (Configuration.UseMemoryCacheAccelerator)
            {
                _memoryCache.Remove(key);
            }

            await _service.RemoveAsync(key, token);
        }
    }
}
