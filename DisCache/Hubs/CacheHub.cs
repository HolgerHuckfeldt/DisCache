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
    public class CacheHub : Hub
    {
        private readonly ILogger<CacheHub> _logger;
        private readonly StamonService _statisticManager;
        private readonly CacheCore _cacheCore;


        // Verwaltung der Muster und Client-Abonnements
        private static readonly ConcurrentDictionary<string, string> Patterns = new();
        private static readonly ConcurrentDictionary<string, HashSet<string>> ClientPatterns = new();

        public CacheHub(
            CacheCore cacheCore,
            StamonService statisticManager,
            ILogger<CacheHub> logger)
        {
            _cacheCore = cacheCore;
            _logger = logger;
            _statisticManager = statisticManager;
        }

        public static ConcurrentDictionary<string, string> KeyPatterns => Patterns;

        /// <summary>
        /// Fügt einen Eintrag zum Cache hinzu.
        /// </summary>
        public async Task<SignalRResponse> SendData(string key, byte[] data, CacheOptions cacheOptions)
        {
            try
            {
                await _cacheCore.SetAsync(key, data, GetOptions(cacheOptions));
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
                var returnData = await _cacheCore.GetAsync(key);
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
                await _cacheCore.RemoveAsync(key);
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
                await _cacheCore.RefreshAsync(key);

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
        public async Task<bool> SubscribeToPattern(string pattern)
        {
            var connectionId = Context.ConnectionId;

            Patterns.TryAdd(pattern, pattern);

            var patterns = ClientPatterns.GetOrAdd(connectionId, _ => new HashSet<string>());
            patterns.Add(pattern);

            await Groups.AddToGroupAsync(connectionId, pattern);

            return true;
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

        public override Task OnConnectedAsync()
        {
            // SubscriberId könnte z. B. die ConnectionId sein
            var subscriberId = Context.ConnectionId;

            // Handler registrieren
            //_eventBroker.SubscribeToUpdates(subscriberId, async (action, key) =>
            //{
            //    // Filtern Sie bei Bedarf nach Muster. Falls nötig, kann der Broker bereits gefiltert anliefern.
            //    // Hier z. B. Clients, die bereits Muster registriert haben:
            //    if (IsMatch(subscriberId, key))
            //    {
            //        await Clients.Caller.SendAsync("Update", action, key);
            //    }
            //});

            //_eventBroker.SubscribeToDeletions(subscriberId, async (key) =>
            //{
            //    if (IsMatch(subscriberId, key))
            //    {
            //        await Clients.Caller.SendAsync("Update", "Delete", key);
            //    }
            //});

            return base.OnConnectedAsync();
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

        private static bool IsPatternInUseByOtherClients(string pattern)
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

        private static bool IsMatch(string key, string pattern)
        {
            // Einfaches Präfix-Matching
            if (pattern.EndsWith('*'))
            {
                var prefix = pattern.TrimEnd('*');
                return key.StartsWith(prefix);
            }
            else
            {
                return key == pattern;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private DistributedCacheEntryOptions? GetOptions(CacheOptions options)
        {
            DistributedCacheEntryOptions distribuedOptions = null;

            if (options != null)
            {
                distribuedOptions = new();
                if (options.AbsoluteExpiration.HasValue)
                {
                    distribuedOptions.AbsoluteExpiration = options.AbsoluteExpiration;
                }

                if (options.SlidingExpiration.HasValue)
                {
                    distribuedOptions.SlidingExpiration = TimeSpan.FromMilliseconds(options.SlidingExpiration.Value);
                }
                else
                {
                    distribuedOptions.SlidingExpiration = null;
                }

                if (options.AbsoluteExpirationRelativeToNow.HasValue)
                {
                    distribuedOptions.AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMilliseconds(options.AbsoluteExpirationRelativeToNow.Value);
                }
                else
                {
                    options.SlidingExpiration = 0;
                }
            }

            return distribuedOptions;
        }

    }
}
