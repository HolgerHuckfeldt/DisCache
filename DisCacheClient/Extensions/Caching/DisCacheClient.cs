using DisCache.Protos;
using DisCacheClient.Extensions.Caching.Interfaces;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using DisCacheClient.Extensions.Caching.REST;
using DisCacheClient.Extensions.Caching.SignalR;
using Google.Protobuf;
using Grpc.Net.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace DisCacheClient.Extensions.Caching
{

    public enum CacheType
    {
        REST,
        gRpc,
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
            //DisCacher.DisCacherClient grpcClient,
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
                case CacheType.REST:
                    _distributedCache = new DisRestCacheClient(_options.Configuration);
                    break;
                case CacheType.gRpc:
                    _distributedCache = new gRpc.DisGrpcCacheClient(_options.Configuration);
                    break;
                case CacheType.SignalR:
                    _distributedCache = new DisSignalRCacheClient(_options.Configuration);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        private readonly DisCacheOptions _options;

        public byte[] Get(string key)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation($"DisC: Getting Key {key}.");
                if (((IDiscCacheClient)_distributedCache).IsReady())
                {
                    return _distributedCache.Get(key);
                }

                if (_options.UseFallback)
                {
                    return _distributedFallbackCache.Get(key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                sw.Stop();
                var tt = sw.ElapsedMilliseconds;
                
            }
            return null;
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            try
            {
                _logger.LogInformation($"DisC: Getting Key {key} async.");
                if (((IDiscCacheClient)_distributedCache).IsReady())
                {
                    return await _distributedCache.GetAsync(key, token);
                }

                if (_options.UseFallback)
                {
                    return await _distributedFallbackCache.GetAsync(key, token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return null;
        }

        public void Refresh(string key)
        {
            try
            {
                _logger.LogInformation($"DisC: Refresh Key {key}.");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            try
            {
                _logger.LogInformation($"DisC: Refresh Key {key} async.");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

        }

        public void Remove(string key)
        {
            try
            {
                _logger.LogInformation($"DisC: Remove Key {key}.");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            try
            {
                _logger.LogInformation($"DisC: Remove Key {key} async.");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            try
            {
                _logger.LogInformation($"DisC: Set Key {key}.");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            try
            {
                _logger.LogInformation($"DisC: Set Key {key} async.");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
