using DisCache.BackgroundTaskQueue;
using DisCache.Mediator;
using DisCache.Model;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stamon.Services;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DisCache.Services
{

    public class CacheCore
    {
        private readonly IDistributedCache _service;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheCore> _logger;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly StamonService _statisticManager;
        private readonly KeyService _keyService;
        private readonly IMediator _mediator; // MediatR

        public CacheCore(
            IMediator mediator,
            KeyService keyService,
            StamonService statisticManager,
            IBackgroundTaskQueue taskQueue,
            IDistributedCache service,
            IMemoryCache memoryCache,
            ILogger<CacheCore> logger)
        {
            _mediator = mediator;
            _keyService = keyService;
            _statisticManager = statisticManager;
            _taskQueue = taskQueue;
            _logger = logger;
            _service = service;
            _memoryCache = memoryCache;
        }


        public async Task<byte[]> GetAsync(
            string key,
            CancellationToken token = new CancellationToken())
        {
            try
            {
                Stopwatch sw = new();
                sw.Start();

                _logger.LogInformation("Get {Key}", key);

                await _statisticManager.StatisticEvent("Request");

                if (_keyService.HasAction(key) == KeyAction.Ignore)
                {
                    await _statisticManager.StatisticEvent("Miss");
                    return null;
                }

                byte[] returnData = null;

                if (Configuration.UseMemoryCacheAccelerator)
                {
                    returnData = _memoryCache.Get(key) as byte[];

                    if (returnData != null)
                    {
                        if (Configuration.UseQueuedRefresh)
                        {
                            // Aufgabe in die Warteschlange stellen
                            _taskQueue.QueueBackgroundWorkItem(async tk =>
                            {
                                try
                                {
                                    await _service.RefreshAsync(key, token);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error refreshing cache key {Key}", key);
                                }
                            });
                        }
                        else
                        {
                            await _service.RefreshAsync(key, token);
                        }
                    }
                }

                if (returnData == null)
                {
                    returnData = await _service.GetAsync(key, token);

                    if (returnData != null && Configuration.UseMemoryCacheAccelerator)
                    {
                        _memoryCache.Set(key, returnData,
                            new MemoryCacheEntryOptions()
                            {
                                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Configuration.UseMemoryCacheAcceleratorTime)
                            });
                    }
                }

                await _statisticManager.StatisticEvent(returnData != null ? "Hit" : "Miss");

                sw.Stop();
                _logger.LogDebug(returnData != null
                    ? $"Hub: Get {key} - Hit. {sw.ElapsedMilliseconds} milliseconds."
                    : $"Hub: Get {key} - Miss. {sw.ElapsedMilliseconds} milliseconds.");

                return returnData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Get");
                throw;
            }
        }

        public async Task SetAsync(
            string key,
            byte[] value,
            CacheOptions options,
            CancellationToken token = new())
        {
            await SetAsync(key, value, GetDistributedCacheOptions(options), token);
        }

        public async Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = new())
        {
            try
            {
                await _statisticManager.StatisticEvent("Request");

                await _statisticManager.StatisticEvent("Insert");

                // Daten in den verteilten Cache schreiben
                await _service.SetAsync(key, value, options, token);

                if (Configuration.UseMemoryCacheAccelerator)
                {
                    _memoryCache.Set(key, value,
                        new MemoryCacheEntryOptions()
                        {
                            AbsoluteExpiration = options.AbsoluteExpiration,
                            AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow,
                            SlidingExpiration = options.SlidingExpiration
                        });
                }

                // Benachrichtige Clients über Änderungen
                if (DisCacheConfiguration.UseEvents)
                {
                    await _mediator.Publish(new KeyUpdatedNotification("Update", key), token);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendData");
               throw;
            }
        }

        public async Task RefreshAsync(
            string key,
            CancellationToken token = new CancellationToken())
        {
            try
            {
                await _statisticManager.StatisticEvent("Request");

                if (Configuration.UseMemoryCacheAccelerator)
                {
                    _memoryCache.Get(key);
                }

                await _service.RefreshAsync(key, token);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Refresh");
            }
        
        }


        public async Task RemoveAsync(
            string key,
            CancellationToken token = new CancellationToken())
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

                await _service.RemoveAsync(key, token);

                // Benachrichtige Clients über die Löschung
                if (DisCacheConfiguration.UseEvents)
                {
                    await _mediator.Publish(new KeyDeletedNotification(key), token);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Remove");
            }
        }

        private static DistributedCacheEntryOptions GetDistributedCacheOptions(CacheOptions? o)
        {
            if(o == default)
                return null;

            var options = new DistributedCacheEntryOptions { AbsoluteExpiration = o.AbsoluteExpiration };

            if (o.SlidingExpiration.HasValue)
            {
                options.SlidingExpiration = TimeSpan.FromMilliseconds(o.SlidingExpiration.Value);
            }
            else
            {
                options.SlidingExpiration = null;
            }

            if (o.AbsoluteExpirationRelativeToNow.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(o.AbsoluteExpirationRelativeToNow.Value);
            }
            else
            {
                options.SlidingExpiration = null;
            }

            return options;
        }
    }
}
