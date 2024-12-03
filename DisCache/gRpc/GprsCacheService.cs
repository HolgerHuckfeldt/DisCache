using DisCache.BackgroundTaskQueue;
using DisCache.Model;
using DisCache.Protos;
using DisCache.Services;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Stamon.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DisCache.gRpc
{
    public class GprsCacheService : DisCacher.DisCacherBase
    {
        private readonly IDistributedCache _cacheService;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<GprsCacheService> _logger;
        private readonly StamonService _statisticManager;
        private readonly KeyService _keyService;
        private readonly IBackgroundTaskQueue _taskQueue;

        // Verwaltung der Muster und Client-Abonnements
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<IServerStreamWriter<UpdateNotification>, bool>> PatternSubscriptions = new();

        public GprsCacheService(
            IDistributedCache cacheService,
            IMemoryCache memoryCache,
            ILogger<GprsCacheService> logger,
            StamonService statisticManager,
            KeyService keyService,
            IBackgroundTaskQueue taskQueue)
        {
            _cacheService = cacheService;
            _memoryCache = memoryCache;
            _logger = logger;
            _statisticManager = statisticManager;
            _keyService = keyService;
            _taskQueue = taskQueue;
        }

        public override async Task<SetCacheResponse> Set(SetCacheRequest request, ServerCallContext context)
        {
            try
            {
                var options = new DistributedCacheEntryOptions();

                if (request.Options != null)
                {
                    if (request.Options.AbsoluteExpiration != 0)
                    {
                        options.AbsoluteExpiration =
                            DateTimeOffset.FromUnixTimeMilliseconds(request.Options.AbsoluteExpiration);
                    }

                    if (request.Options.SlidingExpiration != 0)
                    {
                        options.SlidingExpiration = TimeSpan.FromMilliseconds(request.Options.SlidingExpiration);
                    }
                    else
                    {
                        options.SlidingExpiration = null;
                    }

                    if (request.Options.AbsoluteExpirationRelativeToNow != 0)
                    {
                        options.AbsoluteExpirationRelativeToNow =
                            TimeSpan.FromMilliseconds(request.Options.AbsoluteExpirationRelativeToNow);
                    }
                    else
                    {
                        options.SlidingExpiration = null;
                    }
                }

                // Abonnenten benachrichtigen
                await NotifySubscribersAsync("Update", request.Key);

                _cacheService.Set(request.Key, request.Data.ToByteArray(), options);
                return new SetCacheResponse { Result = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return new SetCacheResponse { Result = false };
            }
        }

        public override Task<GetCacheResponse> Get(GetCacheRequest request, ServerCallContext context)
        {
            try
            {
                if (_keyService.HasAction(request.Key) == KeyAction.Ignore)
                {
                    //await _statisticManager.StatisticEvent(StatisticsType.MissesPerTime);
                    return Task.FromResult(new GetCacheResponse() { Data = ByteString.Empty, Found = false });
                }

                var data = _cacheService.Get(request.Key);

                if (data != null)
                {
                    var result = new GetCacheResponse() { Data = ByteString.CopyFrom(data), Found = true };
                    return Task.FromResult(result);
                }

                return Task.FromResult(new GetCacheResponse() { Data = ByteString.Empty, Found = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return Task.FromResult(new GetCacheResponse() { Data = ByteString.Empty, Found = false });
            }
        }


        public override Task<SetCacheResponse> Remove(GetCacheRequest request, ServerCallContext context)
        {
            try
            {
                _cacheService.Remove(request.Key);
                return Task.FromResult(new SetCacheResponse() { Result = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return Task.FromResult(new SetCacheResponse { Result = false });
            }
        }

        public override Task<SetCacheResponse> Refresh(GetCacheRequest request, ServerCallContext context)
        {
            try
            {
                _cacheService.Refresh(request.Key);
                return Task.FromResult(new SetCacheResponse() { Result = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return Task.FromResult(new SetCacheResponse { Result = false });
            }
        }

        public override Task<GetCacheResponse> Ping(GetCacheRequest request, ServerCallContext context)
        {
            var result = new GetCacheResponse()
            {
                Data = ByteString.CopyFrom(Encoding.UTF8.GetBytes("Pong")),
                Found = true,
            };
            return Task.FromResult(result);
        }


        public override async Task SubscribeToPattern(IAsyncStreamReader<SubscriptionRequest> requestStream, IServerStreamWriter<UpdateNotification> responseStream, ServerCallContext context)
        {
            var subscriptions = new ConcurrentDictionary<string, bool>();

            // Verarbeitung der eingehenden Abonnementanfragen
            var readTask = Task.Run(async () =>
            {
                try
                {
                    while (await requestStream.MoveNext())
                    {
                        var request = requestStream.Current;
                        if (request.Subscribe)
                        {
                            // Abonnieren
                            subscriptions[request.Pattern] = true;
                            var patternSubscribers = PatternSubscriptions.GetOrAdd(request.Pattern, _ => new ConcurrentDictionary<IServerStreamWriter<UpdateNotification>, bool>());
                            patternSubscribers[responseStream] = true;
                        }
                        else
                        {
                            // Deabonnieren
                            subscriptions.TryRemove(request.Pattern, out _);
                            if (PatternSubscriptions.TryGetValue(request.Pattern, out var patternSubscribers))
                            {
                                patternSubscribers.TryRemove(responseStream, out _);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SubscribeToPattern readTask");
                }
            });

            // Halten Sie den Stream offen, bis der Client die Verbindung trennt oder der CancellationToken abgebrochen wird
            var cancellationTask = Task.Delay(Timeout.Infinite, context.CancellationToken);

            await Task.WhenAny(readTask, cancellationTask);

            // Bereinigen der Abonnements bei Trennung
            foreach (var pattern in subscriptions.Keys)
            {
                if (PatternSubscriptions.TryGetValue(pattern, out var patternSubscribers))
                {
                    patternSubscribers.TryRemove(responseStream, out _);
                }
            }
        }


        private async Task NotifySubscribersAsync(string action, string key)
        {
            var matchingPatterns = PatternSubscriptions.Keys.Where(pattern => IsMatch(key, pattern));

            foreach (var pattern in matchingPatterns)
            {
                if (PatternSubscriptions.TryGetValue(pattern, out var subscribers))
                {
                    var notification = new UpdateNotification { Action = action, Key = key };
                    foreach (var subscriber in subscribers.Keys)
                    {
                        try
                        {
                            await subscriber.WriteAsync(notification);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error notifying subscriber");
                            // Optional: Entfernen Sie den fehlerhaften Subscriber
                            subscribers.TryRemove(subscriber, out _);
                        }
                    }
                }
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
