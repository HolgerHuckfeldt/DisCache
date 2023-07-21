using DisCache.Protos;
using DisCacheClient.Extensions.Caching.Interfaces;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DisCacheClient.Extensions.Caching.gRpc
{
    public class DisGrpcCacheClient : IDistributedCache, IDiscCacheClient
    {

        private readonly DisCacher.DisCacherClient _grpClient;

        public DisGrpcCacheClient(string baseUrl)
        {
            var channel = GrpcChannel.ForAddress(baseUrl);
            _grpClient = new DisCacher.DisCacherClient(channel);
        }

        public bool IsReady()
        {
            return Ping();
        }

        public byte[] Get(string key)
        {
            var result = _grpClient.Get(new GetCacheRequest()
            {
                Key = key
            });
            return result?.Data.ToByteArray();
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            var grpcResult = await _grpClient.GetAsync(new GetCacheRequest()
            {
                Key = key
            });
            return grpcResult.Data.ToByteArray();
        }

        public async Task<bool> PingAsync(CancellationToken token = default)
        {
            try
            {
                var grpcResult = await _grpClient.PingAsync(new GetCacheRequest());

                return grpcResult.Found;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Ping(CancellationToken token = default)
        {
            try
            {
                var grpcResult = _grpClient.Ping(new GetCacheRequest());

                return grpcResult.Found;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Refresh(string key)
        {
            var grpcResult = _grpClient.Refresh(new GetCacheRequest()
            {
                Key = key
            });
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            var grpcResult = await _grpClient.RefreshAsync(new GetCacheRequest()
            {
                Key = key
            });

        }

        public void Remove(string key)
        {
            var grpcResult = _grpClient.Remove(new GetCacheRequest()
            {
                Key = key
            });
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            var grpcResult = await _grpClient.RemoveAsync(new GetCacheRequest()
            {
                Key = key
            });
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            var grpcResult = _grpClient.Set(new SetCacheRequest()
            {
                Key = key,
                Data = ByteString.CopyFrom(value),
                Options = MakeOptions(options)
            });
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            var grpcResult = await _grpClient.SetAsync(new SetCacheRequest()
            {
                Key = key,
                Data = ByteString.CopyFrom(value),
                Options = MakeOptions(options)
            });
        }

        private DisCache.Protos.CacheOptions MakeOptions(DistributedCacheEntryOptions options)
        {
            var disOptions = new DisCache.Protos.CacheOptions();

            if (options.AbsoluteExpiration.HasValue)
            {
                disOptions.AbsoluteExpiration = options.AbsoluteExpiration.Value.ToUnixTimeMilliseconds();
            }

            if (options.SlidingExpiration.HasValue)
            {
                disOptions.SlidingExpiration = options.SlidingExpiration.Value.TotalMilliseconds;
            }

            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                disOptions.SlidingExpiration = options.AbsoluteExpirationRelativeToNow.Value.TotalMilliseconds;
            }

            return disOptions;
        }


    }
}

