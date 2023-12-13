using DisCache.Hubs;
using DisCache.Model;
using System;
using System.Threading.Tasks;
using DisCache.Protos;
using DisCache.Services;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text;

namespace DisCache.gRpc
{
    public class DisCacheService : DisCacher.DisCacherBase
    {
        private readonly ILogger<DisCacheService> _logger;
        private readonly IDistributedCache _cacheService;
        private readonly KeyService _keyService;

        public DisCacheService(
            IDistributedCache cacheService,
            KeyService keyService,
            ILogger<DisCacheService> logger)
        {
            _logger = logger;
            _keyService = keyService;
            _cacheService = cacheService;
        }

        public override Task<SetCacheResponse> Set(SetCacheRequest request, ServerCallContext context)
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

                _cacheService.Set(request.Key, request.Data.ToByteArray(), options);
                return Task.FromResult(new SetCacheResponse { Result = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return Task.FromResult(new SetCacheResponse { Result = false });
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
                Data = ByteString.CopyFrom(Encoding.UTF8.GetBytes("Pong")), Found = true,
            };
            return Task.FromResult(result);
        }
    }
}
