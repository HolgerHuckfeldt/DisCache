using DisCache.gRpc;
using DisCache.Protos;
using MediatR;
using System.Linq;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace DisCache.Mediator
{

    public class KeyUpdateNotificationHandlerForGrpc : INotificationHandler<KeyUpdatedNotification>
    {
        private readonly ILogger<KeyUpdateNotificationHandlerForGrpc> _logger;

        public KeyUpdateNotificationHandlerForGrpc(ILogger<KeyUpdateNotificationHandlerForGrpc> logger)
        {
            _logger = logger;
        }

        public async Task Handle(KeyUpdatedNotification notification, CancellationToken cancellationToken)
        {
            await NotifySubscribersAsync(notification.Action, notification.Key);
        }

        private async Task NotifySubscribersAsync(string action, string key)
        {
            // Dies repliziert die Logik aus "NotifySubscribersAsync" in Ihrem ursprünglichen GprsCacheService.
            // matchingPatterns finden:
            var matchingPatterns = GprsCacheService.PatternSubscriptions.Keys.Where(pattern => IsMatch(key, pattern)).ToList();

            foreach (var pattern in matchingPatterns)
            {
                if (GprsCacheService.PatternSubscriptions.TryGetValue(pattern, out var subscribers))
                {
                    var updateNote = new UpdateNotification { Action = action, Key = key };
                    foreach (var subscriber in subscribers.Keys)
                    {
                        try
                        {
                            await subscriber.WriteAsync(updateNote);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error notifying gRPC subscriber");
                            subscribers.TryRemove(subscriber, out _);
                        }
                    }
                }
            }
        }

        private bool IsMatch(string key, string pattern)
        {
            if (pattern.EndsWith("*"))
            {
                var prefix = pattern.TrimEnd('*');
                return key.StartsWith(prefix);
            }
            return key == pattern;
        }
    }

    public class KeyDeleteNotificationHandlerForGrpc : INotificationHandler<KeyDeletedNotification>
    {
        private readonly ILogger<KeyDeleteNotificationHandlerForGrpc> _logger;

        public KeyDeleteNotificationHandlerForGrpc(ILogger<KeyDeleteNotificationHandlerForGrpc> logger)
        {
            _logger = logger;
        }

        public async Task Handle(KeyDeletedNotification notification, CancellationToken cancellationToken)
        {
            await NotifySubscribersAsync("Delete", notification.Key);
        }

        private async Task NotifySubscribersAsync(string action, string key)
        {
            var matchingPatterns = GprsCacheService.PatternSubscriptions.Keys.Where(pattern => IsMatch(key, pattern)).ToList();

            foreach (var pattern in matchingPatterns)
            {
                if (GprsCacheService.PatternSubscriptions.TryGetValue(pattern, out var subscribers))
                {
                    var deleteNote = new UpdateNotification { Action = action, Key = key };
                    foreach (var subscriber in subscribers.Keys)
                    {
                        try
                        {
                            await subscriber.WriteAsync(deleteNote);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error notifying gRPC subscriber");
                            subscribers.TryRemove(subscriber, out _);
                        }
                    }
                }
            }
        }

        private bool IsMatch(string key, string pattern)
        {
            if (pattern.EndsWith("*"))
            {
                var prefix = pattern.TrimEnd('*');
                return key.StartsWith(prefix);
            }
            return key == pattern;
        }
    }


}
