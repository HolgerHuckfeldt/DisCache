using DisCache.Model;
using DisCache.Services;
using Microsoft.AspNetCore.Http.Connections.Features;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Stamon.Services;
using System.Text;

namespace DisCache.Hubs
{
    /// <summary>
    /// SignalR implementation of the cache server
    /// </summary>
    public class CacheHub : Hub
    {
        private readonly IDistributedCache _service;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheHub> _logger;
        private readonly StamonService _statisticManager;
        private readonly KeyService _keyService;

        public CacheHub(
            StamonService statisticManager,
            IDistributedCache service,
            KeyService keyService,
            IMemoryCache memoryCache,
            ILogger<CacheHub> logger)
        {
            _service = service;
            _memoryCache = memoryCache;
            _logger = logger;
            _keyService = keyService;
            _statisticManager = statisticManager;
        }

        /// <summary>
        /// Add a item to the cache.
        /// </summary>
        /// <param name="key">Key of the cached item</param>
        /// <param name="data">Data to be cached</param>
        /// <param name="cacheOptions">Cache-Options</param>
        /// <returns></returns>
        public async Task<SignalRResponse> SendData(string key, byte[] data, CacheOptions cacheOptions)
        {
            try
            {

                //var tt = Context.Features.Get<IHttpTransportFeature>()!.TransportType;
                //_logger.LogInformation($"Transport={tt}");
                _logger.LogInformation("SendData {Key}", key);

                await _statisticManager.StatisticEvent("Request");
                var options = new DistributedCacheEntryOptions();


                if (cacheOptions != null)
                {
                    options.AbsoluteExpiration = cacheOptions.AbsoluteExpiration;

                    if (cacheOptions.SlidingExpiration.HasValue)
                    {
                        options.SlidingExpiration = TimeSpan.FromMilliseconds(cacheOptions.SlidingExpiration.Value);
                    }
                    else
                    {
                        options.SlidingExpiration = null;
                    }

                    if (cacheOptions.AbsoluteExpirationRelativeToNow.HasValue)
                    {
                        options.AbsoluteExpirationRelativeToNow =
                            TimeSpan.FromMilliseconds(cacheOptions.AbsoluteExpirationRelativeToNow.Value);
                    }
                    else
                    {
                        options.SlidingExpiration = null;
                    }
                }
                await _statisticManager.StatisticEvent("Insert");

                if (Configuration.UseMemoryCacheAccelerator)
                {
                    _memoryCache.Set(key, data,
                        new MemoryCacheEntryOptions()
                        {
                            AbsoluteExpiration = options.AbsoluteExpiration,
                            SlidingExpiration = TimeSpan.FromSeconds(Configuration.UseMemoryCacheAcceleratorTime)
                        });
                }

                return new SignalRResponse() { Data = null, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return new SignalRResponse() { Data = null, Success = false, Message = ex.Message };
            }

        }

        /// <summary>Returns a Pong
        /// </summary>
        /// <returns>Pong</returns>
        public async Task<SignalRResponse> Ping()
        {
            try
            {
                //var tt = Context.Features.Get<IHttpTransportFeature>()!.TransportType;
                //_logger.LogInformation($"Transport={tt}");
                _logger.LogInformation($"Ping");

                await _statisticManager.StatisticEvent("Request");

                return new SignalRResponse() { Data = Encoding.UTF8.GetBytes("Pong"), Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return new SignalRResponse() { Data = null, Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Get cache by key.
        /// </summary>
        /// <param name="key">Key of the cached item</param>
        /// <returns></returns>
        public async Task<SignalRResponse> Get(string key)
        {
            try
            {
                //var tt = Context.Features.Get<IHttpTransportFeature>()!.TransportType;
                //_logger.LogInformation($"Transport={tt}");

                _logger.LogInformation("Get {Key}", key);

                await _statisticManager.StatisticEvent("Request");

                if (_keyService.HasAction(key) == KeyAction.Ignore)
                {
                    await _statisticManager.StatisticEvent("Miss");
                    return new SignalRResponse() { Data = null, Success = true };
                }

                byte[] returnData = null;

                if (Configuration.UseMemoryCacheAccelerator)
                {
                    // Wenn der InMemory-Accelerator aktiviert ist, erst mal darein schauen.
                    returnData = _memoryCache.Get(key) as byte[];
                }

                returnData ??= await _service.GetAsync(key);

                await _statisticManager.StatisticEvent(returnData != null
                    ? "Hit"
                    : "Miss");

                if (returnData != null)
                {
                    _logger.LogDebug("Get {Key} - Hit", key);
                }
                else
                {
                    _logger.LogDebug("Get {Key} - Fail", key);
                }

                return new SignalRResponse() { Data = returnData, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return new SignalRResponse() { Data = null, Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Remove item from cache by key.
        /// </summary>
        /// <param name="key">Key of the cached item</param>
        /// <returns></returns>
        public async Task<SignalRResponse> Remove(string key)
        {
            try
            {
                //var tt = Context.Features.Get<IHttpTransportFeature>()!.TransportType;
                //_logger.LogInformation($"Transport={tt}");
                _logger.LogDebug("Remove {Key}", key);

                await _statisticManager.StatisticEvent("Request");
                await _statisticManager.StatisticEvent("Delete");

                if (Configuration.UseMemoryCacheAccelerator)
                {
                    // Wenn der InMemory-Accelerator aktiviert ist, erst mal da rein schauen.
                    _memoryCache.Remove(key);
                }

                await _service.RemoveAsync(key);
                return new SignalRResponse() { Data = null, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return new SignalRResponse() { Data = null, Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Refresh item in cache by key.
        /// </summary>
        /// <param name="key">Key of the cached item</param>
        /// <returns></returns>
        public async Task<SignalRResponse> Refresh(string key)
        {
            try
            {
                await _statisticManager.StatisticEvent("Request");
                //_statisticManager.StatisticEvent(StatisticsType.);

                if (Configuration.UseMemoryCacheAccelerator)
                {
                    // Wenn der InMemory-Accelerator aktiviert ist, erst mal darein schauen.
                    _memoryCache.Get(key);
                }

                await _service.RefreshAsync(key);

                return new SignalRResponse() { Data = null, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return new SignalRResponse() { Data = null, Success = false, Message = ex.Message };
            }
        }
    }
}
