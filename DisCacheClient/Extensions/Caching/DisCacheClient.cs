#nullable enable
using DisCacheClient.Exceptions;
using DisCacheClient.Extensions.Caching.gRpc.DisCacheClient.Extensions.Caching.gRpc;
using DisCacheClient.Extensions.Caching.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using DisCacheClient.Extensions.Caching.REST;
using DisCacheClient.Extensions.Caching.SignalR;
using Microsoft.Extensions.Caching.Distributed;
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

    public class DisCacheClient : IDistributedCache, IDiscCacheClient, IDisposable
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IDistributedCache _distributedFallbackCache;
        private readonly ILogger<DisCacheClient> _logger;

        public event DisCUpdateHandler? DisCUpdate;
        public event DisCDeleteHandler? DisCDelete;

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
                    var grpcClient = new DisGrpcCacheClient(_options.Configuration);
                    grpcClient.Subscribe("_test*");
                    grpcClient.DisCDelete += Client_DisCDelete;
                    grpcClient.DisCUpdate += Client_DisCUpdate;
                    _distributedCache = grpcClient;
                    break;
                case CacheType.SignalR:
                    var client = new DisSignalRCacheClient(_options.Configuration);

                    client.DisCDelete += Client_DisCDelete;
                    client.DisCUpdate += Client_DisCUpdate;

                    _distributedCache = client;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        private readonly DisCacheOptions _options;

        private void Client_DisCDelete(object sender, string key)
        {
            DisCDelete?.Invoke(sender, key);
        }

        private void Client_DisCUpdate(object sender, string key)
        {
            DisCUpdate?.Invoke(sender, key);
        }

        public byte[]? Get(string key)
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
            catch (DiscException ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                sw.Stop();
                //var tt = sw.ElapsedMilliseconds;

            }
            return null;
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
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
            catch (DiscException ex)
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
            catch (DiscException ex)
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
            catch (DiscException ex)
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
            catch (DiscException ex)
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
            catch (DiscException ex)
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
            catch (DiscException ex)
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
            catch (DiscException ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }


        public void Dispose()
        {
            if (_distributedCache is DisSignalRCacheClient client)
            {
                client.DisCDelete -= Client_DisCDelete;
                client.DisCUpdate -= Client_DisCUpdate;
            }
        }

        public void Subscribe(string pattern)
        {
            if (_distributedCache is IDiscCacheClient cc)
            {
                cc.Subscribe(pattern);
            }
        }

        public void Unsubscribe(string pattern)
        {
            if (_distributedCache is IDiscCacheClient cc)
            {
                cc.Unsubscribe(pattern);
            }
        }

        public async Task SubscribeAsync(string pattern)
        {
            if (_distributedCache is IDiscCacheClient cc)
            {
                await cc.SubscribeAsync(pattern);
            }
        }

        public async Task UnsubscribeAsync(string pattern)
        {
            if (_distributedCache is IDiscCacheClient cc)
            {
                await cc.UnsubscribeAsync(pattern);
            }
        }

        public bool IsReady()
        {
            if (_distributedCache is IDiscCacheClient cc)
            {
                return cc.IsReady();
            }

            return true;
        }
    }
}
