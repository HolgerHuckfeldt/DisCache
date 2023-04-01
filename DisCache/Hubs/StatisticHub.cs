using DisCache.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Stamon.Services;

namespace DisCache.Hubs
{
    public class StatisticHub : Hub
    {
        private readonly IDistributedCache _service;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheHub> _logger;
        private readonly StamonService _statisticManager;
        private readonly KeyService _keyService;

        public StatisticHub(
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


    }
}
