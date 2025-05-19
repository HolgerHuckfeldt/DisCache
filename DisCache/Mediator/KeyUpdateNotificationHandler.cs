using System.Linq;

namespace DisCache.Mediator
{
    using DisCache.Hubs;
    using MediatR;
    using Microsoft.AspNetCore.SignalR;
    using System.Threading.Tasks;
    using System.Threading;

    public class KeyUpdateNotificationHandler : INotificationHandler<KeyUpdatedNotification>
    {
        private readonly IHubContext<CacheHub> _hubContext;

        public KeyUpdateNotificationHandler(IHubContext<CacheHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task Handle(KeyUpdatedNotification notification, CancellationToken cancellationToken)
        {
            await NotifySignalRClientsAsync("Update", notification.Key);
        }

        private async Task NotifySignalRClientsAsync(string action, string key)
        {
            var matchingPatterns = CacheHub.KeyPatterns.Keys.Where(pattern => IsMatch(key, pattern));
            foreach (var pattern in matchingPatterns)
            {
                await _hubContext.Clients.Groups(pattern).SendAsync("Update", action, key);
            }
        }

        private static bool IsMatch(string key, string pattern)
        {
            if (pattern.EndsWith('*'))
            {
                var prefix = pattern.TrimEnd('*');
                return key.StartsWith(prefix);
            }

            return key == pattern;
        }
    }

    public class KeyDeleteNotificationHandler : INotificationHandler<KeyDeletedNotification>
    {
        private readonly IHubContext<CacheHub> _hubContext;

        public KeyDeleteNotificationHandler(IHubContext<CacheHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task Handle(KeyDeletedNotification notification, CancellationToken cancellationToken)
        {
            await NotifySignalRClientsAsync("Delete", notification.Key);
        }

        private async Task NotifySignalRClientsAsync(string action, string key)
        {
            var matchingPatterns = CacheHub.KeyPatterns.Keys.Where(pattern => IsMatch(key, pattern));
            foreach (var pattern in matchingPatterns)
            {
                await _hubContext.Clients.Groups(pattern).SendAsync("Update", action, key);
            }
        }

        private static bool IsMatch(string key, string pattern)
        {
            if (pattern.EndsWith('*'))
            {
                var prefix = pattern.TrimEnd('*');
                return key.StartsWith(prefix);
            }

            return key == pattern;
        }
    }
    
}
