using DisCacheClient.Extensions.Caching.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using DisCacheClient.Extensions.Caching.REST;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DisCacheClient.Extensions.Caching
{

    public enum CacheType
    {
        Rest,
        GRpc,
        SignalR
    }

    public class DisCacheClient : IDistributedCache
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IDistributedCache _distributedFallbackCache;
        private readonly ILogger<DisCacheClient> _logger;

        public DisCacheClient(
            MemoryDistributedCache memoryCache,
            IOptions<DisCacheOptions> optionsAccessor,
            ILogger<DisCacheClient> logger)
        {
            _logger = logger;
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _options = optionsAccessor.Value;

            _distributedFallbackCache = memoryCache;

            switch (_options.CacheType)
            {
                case CacheType.Rest:
                    _distributedCache = new DisRestCacheClient(_options.Configuration);
                    break;
                case CacheType.GRpc:
                    _distributedCache = new gRpc.DisGrpcCacheClient(_options.Configuration);
                    break;
                case CacheType.SignalR:
                    _distributedCache = new SignalR.DisSignalRCacheClient(_options.Configuration);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        private readonly DisCacheOptions _options;

        public byte[] Get(string key)
        {
            if (((IDiscCacheClient)_distributedCache).IsReady())
            {
                return _distributedCache.Get(key);
            }

            if (_options.UseFallback)
            {
                return _distributedFallbackCache.Get(key);
            }

            return null;
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            if (((IDiscCacheClient)_distributedCache).IsReady())
            {
                return await _distributedCache.GetAsync(key, token);
            }

            if (_options.UseFallback)
            {
                return await _distributedFallbackCache.GetAsync(key, token);
            }

            return null;
        }

        public void Refresh(string key)
        {
            if (((IDiscCacheClient)_distributedCache).IsReady())
            {
                _distributedCache.Refresh(key);
            }
            else
            {
                if (_options.UseFallback)
                {
                    _distributedFallbackCache.Refresh(key);
                }
            }

        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            if (((IDiscCacheClient)_distributedCache).IsReady())
            {
                await _distributedCache.RefreshAsync(key, token);
            }
            else
            {
                if (_options.UseFallback)
                {
                    await _distributedFallbackCache.RefreshAsync(key, token);
                }
            }


        }

        public void Remove(string key)
        {
            if (((IDiscCacheClient)_distributedCache).IsReady())
            {
                _distributedCache.Remove(key);
            }
            else
            {
                if (_options.UseFallback)
                {
                    _distributedFallbackCache.Remove(key);
                }
            }
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            if (((IDiscCacheClient)_distributedCache).IsReady())
            {
                await _distributedCache.RemoveAsync(key, token);
            }
            else
            {
                if (_options.UseFallback)
                {
                    await _distributedFallbackCache.RemoveAsync(key, token);
                }
            }
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (((IDiscCacheClient)_distributedCache).IsReady())
            {
                _distributedCache.Set(key, value, options);
            }
            else
            {
                if (_options.UseFallback)
                {
                    _distributedFallbackCache.Set(key, value, options);
                }
            }

        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            if (((IDiscCacheClient)_distributedCache).IsReady())
            {
                await _distributedCache.SetAsync(key, value, options, token);
            }
            else
            {
                if (_options.UseFallback)
                {
                    await _distributedFallbackCache.SetAsync(key, value, options, token);
                }
            }
        }
    }
}
