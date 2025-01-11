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
using CacheOptions = DisCache.Model.CacheOptions;

namespace DisCache.gRpc
{
    //public class GprsCacheService : DisCacher.DisCacherBase, IDisposable
    //{
    //    private readonly IDistributedCache _cacheService;
    //    private readonly IMemoryCache _memoryCache;
    //    private readonly ILogger<GprsCacheService> _logger;
    //    private readonly StamonService _statisticManager;
    //    private readonly KeyService _keyService;
    //    private readonly IBackgroundTaskQueue _taskQueue;
    //    private readonly CacheCore _cacheCore;

    //    // Verwaltung der Muster und Client-Abonnements
    //    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<IServerStreamWriter<UpdateNotification>, bool>> PatternSubscriptions = new();

    //    public GprsCacheService(
    //        CacheCore cacheCore,
    //        IDistributedCache cacheService,
    //        IMemoryCache memoryCache,
    //        ILogger<GprsCacheService> logger,
    //        StamonService statisticManager,
    //        KeyService keyService,
    //        IBackgroundTaskQueue taskQueue)
    //    {
    //        _cacheCore = cacheCore;
    //        _cacheService = cacheService;
    //        _memoryCache = memoryCache;
    //        _logger = logger;
    //        _statisticManager = statisticManager;
    //        _keyService = keyService;
    //        _taskQueue = taskQueue;
    //    }

    //    public override async Task<SetCacheResponse> Set(SetCacheRequest request, ServerCallContext context)
    //    {
    //        try
    //        {
    //            var options = new DistributedCacheEntryOptions();

    //            if (request.Options != null)
    //            {
    //                if (request.Options.AbsoluteExpiration != 0)
    //                {
    //                    options.AbsoluteExpiration =
    //                        DateTimeOffset.FromUnixTimeMilliseconds(request.Options.AbsoluteExpiration);
    //                }

    //                if (request.Options.SlidingExpiration != 0)
    //                {
    //                    options.SlidingExpiration = TimeSpan.FromMilliseconds(request.Options.SlidingExpiration);
    //                }
    //                else
    //                {
    //                    options.SlidingExpiration = null;
    //                }

    //                if (request.Options.AbsoluteExpirationRelativeToNow != 0)
    //                {
    //                    options.AbsoluteExpirationRelativeToNow =
    //                        TimeSpan.FromMilliseconds(request.Options.AbsoluteExpirationRelativeToNow);
    //                }
    //                else
    //                {
    //                    options.SlidingExpiration = null;
    //                }
    //            }

    //            await _cacheService.SetAsync(request.Key, request.Data.ToByteArray(), options, CancellationToken.None);

    //            // Abonnenten benachrichtigen
    //            await NotifySubscribersAsync("Update", request.Key);


    //            return new SetCacheResponse { Result = true };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error");
    //            return new SetCacheResponse { Result = false };
    //        }
    //    }

    //    public override Task<GetCacheResponse> Get(GetCacheRequest request, ServerCallContext context)
    //    {
    //        try
    //        {
    //            if (_keyService.HasAction(request.Key) == KeyAction.Ignore)
    //            {
    //                //await _statisticManager.StatisticEvent(StatisticsType.MissesPerTime);
    //                return Task.FromResult(new GetCacheResponse() { Data = ByteString.Empty, Found = false });
    //            }

    //            var data = _cacheService.Get(request.Key);

    //            if (data != null)
    //            {
    //                var result = new GetCacheResponse() { Data = ByteString.CopyFrom(data), Found = true };
    //                return Task.FromResult(result);
    //            }

    //            return Task.FromResult(new GetCacheResponse() { Data = ByteString.Empty, Found = false });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error");
    //            return Task.FromResult(new GetCacheResponse() { Data = ByteString.Empty, Found = false });
    //        }
    //    }


    //    public override Task<SetCacheResponse> Remove(GetCacheRequest request, ServerCallContext context)
    //    {
    //        try
    //        {
    //            _cacheService.Remove(request.Key);
    //            return Task.FromResult(new SetCacheResponse() { Result = true });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error");
    //            return Task.FromResult(new SetCacheResponse { Result = false });
    //        }
    //    }

    //    public override Task<SetCacheResponse> Refresh(GetCacheRequest request, ServerCallContext context)
    //    {
    //        try
    //        {
    //            _cacheService.Refresh(request.Key);
    //            return Task.FromResult(new SetCacheResponse() { Result = true });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error");
    //            return Task.FromResult(new SetCacheResponse { Result = false });
    //        }
    //    }

    //    public override Task<GetCacheResponse> Ping(GetCacheRequest request, ServerCallContext context)
    //    {
    //        var result = new GetCacheResponse()
    //        {
    //            Data = ByteString.CopyFrom(Encoding.UTF8.GetBytes("Pong")),
    //            Found = true,
    //        };
    //        return Task.FromResult(result);
    //    }


    //    public override async Task SubscribeToPattern(IAsyncStreamReader<SubscriptionRequest> requestStream, IServerStreamWriter<UpdateNotification> responseStream, ServerCallContext context)
    //    {
    //        var subscriptions = new ConcurrentDictionary<string, bool>();

    //        // Verarbeitung der eingehenden Abonnementanfragen
    //        var readTask = Task.Run(async () =>
    //        {
    //            try
    //            {
    //                while (await requestStream.MoveNext())
    //                {
    //                    var request = requestStream.Current;
    //                    if (request.Subscribe)
    //                    {
    //                        // Abonnieren
    //                        subscriptions[request.Pattern] = true;
    //                        var patternSubscribers = PatternSubscriptions.GetOrAdd(request.Pattern, _ => new ConcurrentDictionary<IServerStreamWriter<UpdateNotification>, bool>());
    //                        patternSubscribers[responseStream] = true;
    //                    }
    //                    else
    //                    {
    //                        // Deabonnieren
    //                        subscriptions.TryRemove(request.Pattern, out _);
    //                        if (PatternSubscriptions.TryGetValue(request.Pattern, out var patternSubscribers))
    //                        {
    //                            patternSubscribers.TryRemove(responseStream, out _);
    //                        }
    //                    }
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                _logger.LogError(ex, "Error in SubscribeToPattern readTask");
    //            }
    //        });

    //        // Halten Sie den Stream offen, bis der Client die Verbindung trennt oder der CancellationToken abgebrochen wird
    //        var cancellationTask = Task.Delay(Timeout.Infinite, context.CancellationToken);

    //        await Task.WhenAny(readTask, cancellationTask);

    //        // Bereinigen der Abonnements bei Trennung
    //        foreach (var pattern in subscriptions.Keys)
    //        {
    //            if (PatternSubscriptions.TryGetValue(pattern, out var patternSubscribers))
    //            {
    //                patternSubscribers.TryRemove(responseStream, out _);
    //            }
    //        }
    //    }


    //    private async Task NotifySubscribersAsync(string action, string key)
    //    {
    //        var matchingPatterns = PatternSubscriptions.Keys.Where(pattern => IsMatch(key, pattern)).ToList();

    //        foreach (var pattern in matchingPatterns)
    //        {
    //            if (PatternSubscriptions.TryGetValue(pattern, out var subscribers))
    //            {
    //                var notification = new UpdateNotification { Action = action, Key = key };
    //                foreach (var subscriber in subscribers.Keys)
    //                {
    //                    try
    //                    {
    //                        await subscriber.WriteAsync(notification);
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        _logger.LogError(ex, "Error notifying subscriber");
    //                        // Optional: Entfernen Sie den fehlerhaften Subscriber
    //                        subscribers.TryRemove(subscriber, out _);
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    private static bool IsMatch(string key, string pattern)
    //    {
    //        // Einfaches Präfix-Matching
    //        if (pattern.EndsWith('*'))
    //        {
    //            var prefix = pattern.TrimEnd('*');
    //            return key.StartsWith(prefix);
    //        }

    //        return key == pattern;

    //    }

    //    public void Dispose()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    public class GprsCacheService : DisCacher.DisCacherBase, IDisposable
    {
        private readonly IDistributedCache _cacheService;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<GprsCacheService> _logger;
        private readonly StamonService _statisticManager;
        private readonly KeyService _keyService;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly CacheCore _cacheCore;

        internal static readonly ConcurrentDictionary<string, ConcurrentDictionary<IServerStreamWriter<UpdateNotification>, bool>> PatternSubscriptions = new();
        
        // Verwaltung der Muster und Client-Abonnements
        //private static readonly ConcurrentDictionary<string, ConcurrentDictionary<IServerStreamWriter<UpdateNotification>, bool>> PatternSubscriptions = new();

        public GprsCacheService(
            CacheCore cacheCore,
            IDistributedCache cacheService,
            IMemoryCache memoryCache,
            ILogger<GprsCacheService> logger,
            StamonService statisticManager,
            KeyService keyService,
            IBackgroundTaskQueue taskQueue)
        {
            _cacheCore = cacheCore;
            _cacheService = cacheService;
            _memoryCache = memoryCache;
            _logger = logger;
            _statisticManager = statisticManager;
            _keyService = keyService;
            _taskQueue = taskQueue;

            //_cacheCore.DisCUpdate += CacheCoreOnDisCUpdate;
            //_cacheCore.DisCDelete += CacheCoreOnDisCDelete;
        }

        private async void CacheCoreOnDisCUpdate(object sender, string key)
        {
            try
            {
                await NotifySubscribersAsync("Update", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CacheCoreOnDisCUpdate");
            }
        }

        private async void CacheCoreOnDisCDelete(object sender, string key)
        {
            try
            {
                await NotifySubscribersAsync("Delete", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CacheCoreOnDisCUpdate");
            }
        }

        public override async Task<SetCacheResponse> Set(SetCacheRequest request, ServerCallContext context)
        {
            try
            {
                await _cacheCore.SetAsync(request.Key, request.Data.ToByteArray(),GetOptions(request.Options));
                return new SetCacheResponse { Result = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return new SetCacheResponse { Result = false };
            }
        }

        public override async Task<GetCacheResponse> Get(GetCacheRequest request, ServerCallContext context)
        {
            try
            {
                var data = await _cacheCore.GetAsync(request.Key);

                if (data != null)
                {
                    var result = new GetCacheResponse() { Data = ByteString.CopyFrom(data), Found = true };
                    return result;
                }

                return new GetCacheResponse() { Data = ByteString.Empty, Found = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return new GetCacheResponse() { Data = ByteString.Empty, Found = false };
            }
        }


        public override async Task<SetCacheResponse> Remove(GetCacheRequest request, ServerCallContext context)
        {
            try
            {
                await _cacheCore.RemoveAsync(request.Key);
                return new SetCacheResponse() { Result = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return new SetCacheResponse { Result = false };
            }
        }

        public override async Task<SetCacheResponse> Refresh(GetCacheRequest request, ServerCallContext context)
        {
            try
            {
                await _cacheCore.RefreshAsync(request.Key);
                return new SetCacheResponse() { Result = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return new SetCacheResponse { Result = false };
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


        //public override async Task SubscribeToPattern(IAsyncStreamReader<SubscriptionRequest> requestStream, IServerStreamWriter<UpdateNotification> responseStream, ServerCallContext context)
        //{
        //    var subscriptions = new ConcurrentDictionary<string, bool>();

        //    // Verarbeitung der eingehenden Abonnementanfragen
        //    var readTask = Task.Run(async () =>
        //    {
        //        try
        //        {
        //            while (await requestStream.MoveNext())
        //            {
        //                var request = requestStream.Current;
        //                if (request.Subscribe)
        //                {
        //                    // Abonnieren
        //                    subscriptions[request.Pattern] = true;
        //                    var patternSubscribers = PatternSubscriptions.GetOrAdd(request.Pattern, _ => new ConcurrentDictionary<IServerStreamWriter<UpdateNotification>, bool>());
        //                    patternSubscribers[responseStream] = true;
        //                }
        //                else
        //                {
        //                    // Deabonnieren
        //                    subscriptions.TryRemove(request.Pattern, out _);
        //                    if (PatternSubscriptions.TryGetValue(request.Pattern, out var patternSubscribers))
        //                    {
        //                        patternSubscribers.TryRemove(responseStream, out _);
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Error in SubscribeToPattern readTask");
        //        }
        //    });

        //    // Halten Sie den Stream offen, bis der Client die Verbindung trennt oder der CancellationToken abgebrochen wird
        //    var cancellationTask = Task.Delay(Timeout.Infinite, context.CancellationToken);

        //    await Task.WhenAny(readTask, cancellationTask);

        //    // Bereinigen der Abonnements bei Trennung
        //    foreach (var pattern in subscriptions.Keys)
        //    {
        //        if (PatternSubscriptions.TryGetValue(pattern, out var patternSubscribers))
        //        {
        //            patternSubscribers.TryRemove(responseStream, out _);
        //        }
        //    }
        //}


        public override async Task SubscribeToPattern(IAsyncStreamReader<SubscriptionRequest> requestStream, IServerStreamWriter<UpdateNotification> responseStream, ServerCallContext context)
        {
            var subscriptions = new ConcurrentDictionary<string, bool>();

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

            var cancellationTask = Task.Delay(Timeout.Infinite, context.CancellationToken);
            await Task.WhenAny(readTask, cancellationTask);

            // Bereinigen der Abonnements bei Verbindungstrennung
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
            var matchingPatterns = PatternSubscriptions.Keys.Where(pattern => IsMatch(key, pattern)).ToList();

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

        private static bool IsMatch(string key, string pattern)
        {
            // Einfaches Präfix-Matching
            if (pattern.EndsWith('*'))
            {
                var prefix = pattern.TrimEnd('*');
                return key.StartsWith(prefix);
            }

            return key == pattern;
        }

        public void Dispose()
        {

        }

        private DistributedCacheEntryOptions? GetOptions(DisCache.Protos.CacheOptions options)
        {
            DistributedCacheEntryOptions distribuedOptions = null; 

            if (options != null)
            {
                distribuedOptions = new();
                if (options.AbsoluteExpiration != 0)
                {
                    distribuedOptions.AbsoluteExpiration =
                        DateTimeOffset.FromUnixTimeMilliseconds(options.AbsoluteExpiration);
                }

                if (options.SlidingExpiration != 0)
                {
                    distribuedOptions.SlidingExpiration = TimeSpan.FromMilliseconds(options.SlidingExpiration);
                }
                else
                {
                    distribuedOptions.SlidingExpiration = null;
                }

                if (options.AbsoluteExpirationRelativeToNow != 0)
                {
                    distribuedOptions.AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMilliseconds(options.AbsoluteExpirationRelativeToNow);
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
