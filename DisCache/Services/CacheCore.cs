using DisCache.BackgroundTaskQueue;
using DisCache.Mediator;
using DisCache.Model;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Stamon.Services;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DisCache.Services
{
    public class CacheCore
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheCore> _logger;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly StamonService _statisticManager;
        private readonly KeyService _keyService;
        private readonly IMediator _mediator;

        public CacheCore(
            IMediator mediator,
            KeyService keyService,
            StamonService statisticManager,
            IBackgroundTaskQueue taskQueue,
            IDistributedCache distributedCache,
            IMemoryCache memoryCache,
            ILogger<CacheCore> logger)
        {
            _mediator = mediator;
            _keyService = keyService;
            _statisticManager = statisticManager;
            _taskQueue = taskQueue;
            _logger = logger;
            _distributedCache = distributedCache;
            _memoryCache = memoryCache;
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            var sw = Stopwatch.StartNew();
            try
            {

                _logger.LogInformation("Get {Key}", key);

                await _statisticManager.StatisticEvent("Request");

                if (_keyService.HasAction(key) == KeyAction.Ignore)
                {
                    LogMiss(key, sw.ElapsedMilliseconds);
                    return null;
                }

                byte[] data = TryGetFromMemoryCache(key, token);
                if (data != null)
                {
                    LogHit(key, sw.ElapsedMilliseconds);
                    await RefreshAsync(key, token);
                    return data;
                }

                data = await _distributedCache.GetAsync(key, token);
                if (data == null)
                {
                    LogMiss(key, sw.ElapsedMilliseconds);
                    return null;
                }

                LogHit(key, sw.ElapsedMilliseconds);

                if (Configuration.UseMemoryCacheAccelerator)
                {
                    SetToMemoryCache(key, data, token);
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAsync for key {Key}", key);
                throw;
            }
            finally
            {
                sw.Stop();
            }
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            try
            {
                await _statisticManager.StatisticEvent("Request");

                // Write data to the distributed cache
                await _distributedCache.SetAsync(key, value, options, token);
                await _statisticManager.StatisticEvent("Insert");

                if (Configuration.UseMemoryCacheAccelerator)
                {
                    SetToMemoryCache(key, value, token);
                }

                // Notify clients about changes
                if (DisCacheConfiguration.UseEvents)
                {
                    await _mediator.Publish(new KeyUpdatedNotification("Update", key), token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SetAsync for key {Key}", key);
                throw;
            }
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            try
            {
                await _statisticManager.StatisticEvent("Request");

                if (Configuration.UseMemoryCacheAccelerator)
                {
                    _memoryCache.Get(key); // Refresh in-memory cache
                }

                await _distributedCache.RefreshAsync(key, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RefreshAsync for key {Key}", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            try
            {
                _logger.LogDebug("Remove {Key}", key);

                await _statisticManager.StatisticEvent("Request");
                await _statisticManager.StatisticEvent("Delete");

                if (Configuration.UseMemoryCacheAccelerator)
                {
                    _memoryCache.Remove(key);
                }

                await _distributedCache.RemoveAsync(key, token);

                // Notify clients about the deletion
                if (DisCacheConfiguration.UseEvents)
                {
                    await _mediator.Publish(new KeyDeletedNotification(key), token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RemoveAsync for key {Key}", key);
            }
        }

        private byte[] TryGetFromMemoryCache(string key, CancellationToken token)
        {
            if (!Configuration.UseMemoryCacheAccelerator) return null;

            var data = _memoryCache.Get(key) as byte[];
            if (data != null && Configuration.UseQueuedRefresh)
            {
                _taskQueue.QueueBackgroundWorkItem(tk => RefreshAsync(key, tk));
            }
            return data;
        }

        private void SetToMemoryCache(string key, byte[] value, CancellationToken token)
        {
            var memoryCacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Configuration.UseMemoryCacheAcceleratorTime)
            };

            _memoryCache.Set(key, value, memoryCacheOptions);
        }

        private void LogHit(string key, long elapsedMilliseconds)
        {
            _logger.LogDebug("Hub: Get {Key} - Hit. {ElapsedMilliseconds} milliseconds.", key, elapsedMilliseconds);
        }

        private void LogMiss(string key, long elapsedMilliseconds)
        {
            _logger.LogDebug("Hub: Get {Key} - Miss. {ElapsedMilliseconds} milliseconds.", key, elapsedMilliseconds);
        }

        private static DistributedCacheEntryOptions GetDistributedCacheOptions(CacheOptions? options)
        {
            if (options == null)
                return null;

            var distributedCacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = options.AbsoluteExpiration,
                SlidingExpiration = options.SlidingExpiration.HasValue ? TimeSpan.FromMilliseconds(options.SlidingExpiration.Value) : null,
                AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow.HasValue ? TimeSpan.FromMilliseconds(options.AbsoluteExpirationRelativeToNow.Value) : null
            };

            return distributedCacheOptions;
        }
    }
}
