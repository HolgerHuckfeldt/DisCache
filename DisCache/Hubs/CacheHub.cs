using DisCache.BackgroundTaskQueue;
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
using System.Diagnostics;
using System.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DisCache.Hubs
{
    /// <summary>
    /// SignalR's implementation of the cache server
    /// </summary>
    //public class CacheHub : Hub
    //{
    //    private readonly IDistributedCache _service;
    //    private readonly IMemoryCache _memoryCache;
    //    private readonly ILogger<CacheHub> _logger;
    //    private readonly StamonService _statisticManager;
    //    private readonly KeyService _keyService;
    //    private readonly IBackgroundTaskQueue _taskQueue;

    //    // Verwaltung der Muster und Client-Abonnements
    //    private static readonly ConcurrentDictionary<string, string> Patterns = new ConcurrentDictionary<string, string>();
    //    private static readonly ConcurrentDictionary<string, HashSet<string>> ClientPatterns = new ConcurrentDictionary<string, HashSet<string>>();


    //    public CacheHub(
    //        IBackgroundTaskQueue taskQueue,
    //        StamonService statisticManager,
    //        IDistributedCache service,
    //        KeyService keyService,
    //        IMemoryCache memoryCache,
    //        ILogger<CacheHub> logger)
    //    {
    //        _taskQueue = taskQueue;
    //        _service = service;
    //        _memoryCache = memoryCache;
    //        _logger = logger;
    //        _keyService = keyService;
    //        _statisticManager = statisticManager;
    //    }

    //    /// <summary>
    //    /// Add a item to the cache.
    //    /// </summary>
    //    /// <param name="key">Key of the cached item</param>
    //    /// <param name="data">Data to be cached</param>
    //    /// <param name="cacheOptions">Cache-Options</param>
    //    /// <returns></returns>
    //    public async Task<SignalRResponse> SendData(string key, byte[] data, CacheOptions cacheOptions)
    //    {
    //        try
    //        {

    //            await _statisticManager.StatisticEvent("Request");
    //            var options = new DistributedCacheEntryOptions();

    //            if (cacheOptions != null)
    //            {
    //                options.AbsoluteExpiration = cacheOptions.AbsoluteExpiration;

    //                if (cacheOptions.SlidingExpiration.HasValue)
    //                {
    //                    options.SlidingExpiration = TimeSpan.FromMilliseconds(cacheOptions.SlidingExpiration.Value);
    //                }
    //                else
    //                {
    //                    options.SlidingExpiration = null;
    //                }

    //                if (cacheOptions.AbsoluteExpirationRelativeToNow.HasValue)
    //                {
    //                    options.AbsoluteExpirationRelativeToNow =
    //                        TimeSpan.FromMilliseconds(cacheOptions.AbsoluteExpirationRelativeToNow.Value);
    //                }
    //                else
    //                {
    //                    options.SlidingExpiration = null;
    //                }
    //            }

    //            await _service.SetAsync(key, data, options);

    //            await _statisticManager.StatisticEvent("Insert");

    //            if (Configuration.UseMemoryCacheAccelerator)
    //            {
    //                _memoryCache.Set(key, data,
    //                    new MemoryCacheEntryOptions()
    //                    {
    //                        AbsoluteExpiration = options.AbsoluteExpiration,
    //                        SlidingExpiration = TimeSpan.FromSeconds(Configuration.UseMemoryCacheAcceleratorTime)
    //                    });
    //            }

    //            if (DisCacheConfiguration.UseEvents)
    //                await Clients.Others.SendAsync("Update", "Update", key);

    //            return new SignalRResponse() { Data = null, Success = true };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error");
    //            return new SignalRResponse() { Data = null, Success = false, Message = ex.Message };
    //        }

    //    }

    //    /// <summary>Returns a Pong
    //    /// </summary>
    //    /// <returns>Pong</returns>
    //    public async Task<SignalRResponse> Ping()
    //    {
    //        try
    //        {
    //            //var tt = Context.Features.Get<IHttpTransportFeature>()!.TransportType;
    //            //_logger.LogInformation($"Transport={tt}");
    //            _logger.LogInformation($"Ping");

    //            await _statisticManager.StatisticEvent("Request");

    //            return new SignalRResponse() { Data = Encoding.UTF8.GetBytes("Pong"), Success = true };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error");
    //            return new SignalRResponse() { Data = null, Success = false, Message = ex.Message };
    //        }
    //    }

    //    /// <summary>
    //    /// Get cache by key.
    //    /// </summary>
    //    /// <param name="key">Key of the cached item</param>
    //    /// <returns></returns>
    //    public async Task<SignalRResponse> Get(string key)
    //    {
    //        try
    //        {
    //            Stopwatch sw = new Stopwatch();

    //            _logger.LogInformation("Get {Key}", key);

    //            await _statisticManager.StatisticEvent("Request");

    //            if (_keyService.HasAction(key) == KeyAction.Ignore)
    //            {
    //                await _statisticManager.StatisticEvent("Miss");
    //                return new SignalRResponse() { Data = null, Success = true };
    //            }

    //            byte[] returnData = null;

    //            if (Configuration.UseMemoryCacheAccelerator)
    //            {
    //                // Wenn der InMemory-Accelerator aktiviert ist, erst mal darein schauen.
    //                returnData = _memoryCache.Get(key) as byte[];
    //            }

    //            if (returnData != null)
    //            {
    //                if (Configuration.UseQueuedRefresh)
    //                {
    //                    // Aufgabe in die Warteschlange stellen
    //                    _taskQueue.QueueBackgroundWorkItem(async token =>
    //                    {
    //                        try
    //                        {
    //                            await _service.RefreshAsync(key);
    //                        }
    //                        catch (Exception ex)
    //                        {
    //                            _logger.LogError(ex, "Error refreshing cache key {Key}", key);
    //                        }
    //                    });
    //                }
    //                else
    //                {
    //                    await _service.RefreshAsync(key);
    //                }
    //            }

    //            returnData ??= await _service.GetAsync(key);

    //            await _statisticManager.StatisticEvent(returnData != null
    //                ? "Hit"
    //                : "Miss");

    //            sw.Stop();
    //            if (returnData != null)
    //            {
    //                _logger.LogDebug($"Hub: Get {key} - Hit. {sw.ElapsedMilliseconds} milliseconds.");
    //            }
    //            else
    //            {
    //                _logger.LogDebug($"Hub: Get {key} - Fail. {sw.ElapsedMilliseconds} milliseconds.");
    //            }

    //            return new SignalRResponse() { Data = returnData, Success = true };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Hub: Error");
    //            return new SignalRResponse() { Data = null, Success = false, Message = ex.Message };
    //        }
    //    }

    //    /// <summary>
    //    /// Remove item from cache by key.
    //    /// </summary>
    //    /// <param name="key">Key of the cached item</param>
    //    /// <returns></returns>
    //    public async Task<SignalRResponse> Remove(string key)
    //    {
    //        try
    //        {
    //            //var tt = Context.Features.Get<IHttpTransportFeature>()!.TransportType;
    //            //_logger.LogInformation($"Transport={tt}");
    //            _logger.LogDebug("Remove {Key}", key);

    //            await _statisticManager.StatisticEvent("Request");
    //            await _statisticManager.StatisticEvent("Delete");

    //            if (Configuration.UseMemoryCacheAccelerator)
    //            {
    //                // Wenn der InMemory-Accelerator aktiviert ist, erst mal da rein schauen.
    //                _memoryCache.Remove(key);
    //            }

    //            await _service.RemoveAsync(key);

    //            if (DisCacheConfiguration.UseEvents)
    //                await Clients.All.SendAsync("Update", "Delete", key);

    //            return new SignalRResponse() { Data = null, Success = true };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error");
    //            return new SignalRResponse() { Data = null, Success = false, Message = ex.Message };
    //        }
    //    }

    //    /// <summary>
    //    /// Refresh item in cache by key.
    //    /// </summary>
    //    /// <param name="key">Key of the cached item</param>
    //    /// <returns></returns>
    //    public async Task<SignalRResponse> Refresh(string key)
    //    {
    //        try
    //        {
    //            await _statisticManager.StatisticEvent("Request");
    //            //_statisticManager.StatisticEvent(StatisticsType.);

    //            if (Configuration.UseMemoryCacheAccelerator)
    //            {
    //                // Wenn der InMemory-Accelerator aktiviert ist, erst mal darein schauen.
    //                _memoryCache.Get(key);
    //            }

    //            await _service.RefreshAsync(key);

    //            return new SignalRResponse() { Data = null, Success = true };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error");
    //            return new SignalRResponse() { Data = null, Success = false, Message = ex.Message };
    //        }
    //    }


    //}

    public class CacheHub : Hub
    {
        private readonly IDistributedCache _service;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheHub> _logger;
        private readonly StamonService _statisticManager;
        private readonly KeyService _keyService;
        private readonly IBackgroundTaskQueue _taskQueue;

        // Verwaltung der Muster und Client-Abonnements
        private static readonly ConcurrentDictionary<string, string> Patterns = new();
        private static readonly ConcurrentDictionary<string, HashSet<string>> ClientPatterns = new();

        public CacheHub(
            IBackgroundTaskQueue taskQueue,
            StamonService statisticManager,
            IDistributedCache service,
            KeyService keyService,
            IMemoryCache memoryCache,
            ILogger<CacheHub> logger)
        {
            _taskQueue = taskQueue;
            _service = service;
            _memoryCache = memoryCache;
            _logger = logger;
            _keyService = keyService;
            _statisticManager = statisticManager;
        }

        /// <summary>
        /// Fügt einen Eintrag zum Cache hinzu.
        /// </summary>
        public async Task<SignalRResponse> SendData(string key, byte[] data, CacheOptions cacheOptions)
        {
            try
            {
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

                // Daten in den verteilten Cache schreiben
                await _service.SetAsync(key, data, options);

                if (Configuration.UseMemoryCacheAccelerator)
                {
                    _memoryCache.Set(key, data,
                        new MemoryCacheEntryOptions()
                        {
                            AbsoluteExpiration = options.AbsoluteExpiration,
                            AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow,
                            SlidingExpiration = options.SlidingExpiration
                        });
                }

                // Benachrichtige Clients über Änderungen
                if (DisCacheConfiguration.UseEvents)
                    await NotifyClientsAsync("Update", key);

                return new SignalRResponse() { Data = null, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendData");
                return new SignalRResponse() { Data = null, Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Holt einen Cache-Eintrag anhand des Schlüssels.
        /// </summary>
        public async Task<SignalRResponse> Get(string key)
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
                    return new SignalRResponse() { Data = null, Success = true };
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
                            _taskQueue.QueueBackgroundWorkItem(async token =>
                            {
                                try
                                {
                                    await _service.RefreshAsync(key);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error refreshing cache key {Key}", key);
                                }
                            });
                        }
                        else
                        {
                            await _service.RefreshAsync(key);
                        }
                    }
                }

                if (returnData == null)
                {
                    returnData = await _service.GetAsync(key);

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

                return new SignalRResponse() { Data = returnData, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Get");
                return new SignalRResponse() { Data = null, Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Entfernt einen Eintrag aus dem Cache.
        /// </summary>
        public async Task<SignalRResponse> Remove(string key)
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

                await _service.RemoveAsync(key);

                // Benachrichtige Clients über die Löschung
                if (DisCacheConfiguration.UseEvents)
                    await NotifyClientsAsync("Delete", key);

                return new SignalRResponse() { Data = null, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Remove");
                return new SignalRResponse() { Data = null, Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Aktualisiert einen Cache-Eintrag anhand des Schlüssels.
        /// </summary>
        public async Task<SignalRResponse> Refresh(string key)
        {
            try
            {
                await _statisticManager.StatisticEvent("Request");

                if (Configuration.UseMemoryCacheAccelerator)
                {
                    _memoryCache.Get(key);
                }

                await _service.RefreshAsync(key);

                return new SignalRResponse() { Data = null, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Refresh");
                return new SignalRResponse() { Data = null, Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Sendet einen Ping und erhält einen Pong zurück.
        /// </summary>
        public async Task<SignalRResponse> Ping()
        {
            try
            {
                _logger.LogInformation("Ping");

                await _statisticManager.StatisticEvent("Request");

                return new SignalRResponse() { Data = Encoding.UTF8.GetBytes("Pong"), Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Ping");
                return new SignalRResponse() { Data = null, Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Abonniert Benachrichtigungen für ein bestimmtes Schlüssel-Muster.
        /// </summary>
        public async Task SubscribeToPattern(string pattern)
        {
            var connectionId = Context.ConnectionId;

            Patterns.TryAdd(pattern, pattern);

            var patterns = ClientPatterns.GetOrAdd(connectionId, _ => new HashSet<string>());
            patterns.Add(pattern);

            await Groups.AddToGroupAsync(connectionId, pattern);
        }

        /// <summary>
        /// Kündigt das Abonnement für ein bestimmtes Schlüssel-Muster.
        /// </summary>
        public async Task UnsubscribeFromPattern(string pattern)
        {
            var connectionId = Context.ConnectionId;

            if (ClientPatterns.TryGetValue(connectionId, out var patterns))
            {
                patterns.Remove(pattern);

                if (patterns.Count == 0)
                {
                    ClientPatterns.TryRemove(connectionId, out _);
                }
            }

            await Groups.RemoveFromGroupAsync(connectionId, pattern);
        }

        /// <summary>
        /// Wird aufgerufen, wenn ein Client die Verbindung trennt.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
            await RemoveClientFromPatterns(Context.ConnectionId);
        }

        private async Task RemoveClientFromPatterns(string connectionId)
        {
            if (ClientPatterns.TryRemove(connectionId, out var patterns))
            {
                foreach (var pattern in patterns)
                {
                    await Groups.RemoveFromGroupAsync(connectionId, pattern);

                    if (!IsPatternInUseByOtherClients(pattern))
                    {
                        Patterns.TryRemove(pattern, out _);
                    }
                }
            }
        }

        private bool IsPatternInUseByOtherClients(string pattern)
        {
            return ClientPatterns.Values.Any(p => p.Contains(pattern));
        }

        private async Task NotifyClientsAsync(string action, string key)
        {
            var matchingPatterns = Patterns.Keys.Where(pattern => IsMatch(key, pattern));

            foreach (var pattern in matchingPatterns)
            {
                await Clients.Group(pattern).SendAsync("Update", action, key);
            }
        }

        private bool IsMatch(string key, string pattern)
        {
            // Einfaches Präfix-Matching
            if (pattern.EndsWith("*"))
            {
                var prefix = pattern.TrimEnd('*');
                return key.StartsWith(prefix);
            }
            else
            {
                return key == pattern;
            }
        }
    }
}
