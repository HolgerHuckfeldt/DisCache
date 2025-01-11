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
            // Ähnlich wie CacheCoreOnDisCUpdate in Ihrem alten Code
            // Jetzt können Sie Musterprüfung oder direkt an Gruppen senden
            await NotifySignalRClientsAsync("Update", notification.Key);
        }

        private async Task NotifySignalRClientsAsync(string action, string key)
        {
            // Hier kommt die Logik aus NotifyClientsAsync (pattern matching)
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
            else
            {
                return key == pattern;
            }
        }
    }
}
