using DisCache.Protos;
using DisCacheClient.Exceptions;
using DisCacheClient.Extensions.Caching.Interfaces;
using DisCacheClient.Extensions.Caching.SignalR;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DisCacheClient.Extensions.Caching.gRpc
{
    namespace DisCacheClient.Extensions.Caching.gRpc
    {
        public class DisGrpcCacheClient : IDistributedCache, IDiscCacheClient, IDisposable
        {
            private readonly DisCache.Protos.DisCacher.DisCacherClient _client;
            private readonly ILogger<DisGrpcCacheClient> _logger;
            private readonly GrpcChannel _channel;

            public event DisCUpdateHandler? DisCUpdate;
            public event DisCDeleteHandler? DisCDelete;

            private bool _isReady = false;

            public DisGrpcCacheClient(string configuration, ILogger<DisGrpcCacheClient> logger)
            {
                _logger = logger;

                // Erstellen Sie den gRPC-Channel
                _channel = GrpcChannel.ForAddress(configuration);
                //_client = new CacheService.CacheServiceClient(_channel);

                //var client = DisCacher.Descriptor.

                // Starten Sie die Verbindung und das Abonnement
                Task.Run(() => StartSubscription());
            }

            private async Task StartSubscription()
            {
                try
                {
                    var cts = new CancellationTokenSource();

                    using var call = _client.SubscribeToPattern(cancellationToken: cts.Token);

                    // Abonnieren Sie das gewünschte Muster, z. B. alle Schlüssel
                    await call.RequestStream.WriteAsync(new SubscriptionRequest
                    {
                        Pattern = "*",
                        Subscribe = true
                    });

                    _isReady = true;

                    // Empfangen von Benachrichtigungen
                    while (await call.ResponseStream.MoveNext(cts.Token))
                    {
                        var notification = call.ResponseStream.Current;

                        if (notification.Action == "Update")
                        {
                            DisCUpdate?.Invoke(this, notification.Key);
                        }
                        else if (notification.Action == "Delete")
                        {
                            DisCDelete?.Invoke(this, notification.Key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Empfangen von Benachrichtigungen.");
                    _isReady = false;
                }
            }

            public bool IsReady()
            {
                return _isReady;
            }

            public void Dispose()
            {
                _channel?.Dispose();
            }

            // Implementierung der IDistributedCache-Methoden

            public byte[]? Get(string key)
            {
                try
                {
                    var response = _client.Get(new GetCacheRequest() { Key = key });

                    if (response.Found)
                    {
                        return response.Data.ToByteArray();
                    }
                    else
                    {
                        _logger.LogWarning($"Get fehlgeschlagen: {response.Message}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler bei Get");
                    return null;
                }
            }

            public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
            {
                try
                {
                    var response = await _client.GetAsync(new GetCacheRequest { Key = key }, cancellationToken: token);

                    if (response.Found)
                    {
                        return response.Data.ToByteArray();
                    }
                    else
                    {
                        _logger.LogWarning($"GetAsync fehlgeschlagen: {response.Message}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler bei GetAsync");
                    return null;
                }
            }

            public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
            {
                try
                {
                    var cacheOptions = new DisCache.Protos.CacheOptions()
                    {
                        AbsoluteExpiration = options.AbsoluteExpiration?.ToUnixTimeMilliseconds() ?? 0,
                        SlidingExpiration = options.SlidingExpiration?.TotalMilliseconds ?? 0,
                        AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow?.TotalMilliseconds ?? 0
                    };

                    var request = new SetCacheRequest()
                    {
                        Key = key,
                        Data = Google.Protobuf.ByteString.CopyFrom(value),
                        Options = cacheOptions
                    };

                    var response = _client.SetAsync(request);

                    //if (!response.Res)
                    //{
                    //    _logger.LogWarning($"Set fehlgeschlagen: {response.Message}");
                    //}
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler bei Set");
                }
            }

            public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
            {
                try
                {
                    var cacheOptions = new DisCache.Protos.CacheOptions
                    {
                        AbsoluteExpiration = options.AbsoluteExpiration?.ToUnixTimeMilliseconds() ?? 0,
                        SlidingExpiration = options.SlidingExpiration?.TotalMilliseconds ?? 0,
                        AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow?.TotalMilliseconds ?? 0
                    };

                    var request = new SetCacheRequest()
                    {
                        Key = key,
                        Data = Google.Protobuf.ByteString.CopyFrom(value),
                        Options = cacheOptions
                    };

                    var response = await _client.SetAsync(request, cancellationToken: token);

                    if (!response.Result)
                    {
                        _logger.LogWarning($"SetAsync fehlgeschlagen: {response}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler bei SetAsync");
                }
            }

            public void Refresh(string key)
            {
                try
                {
                    var response = _client.Refresh(new GetCacheRequest() { Key = key });

                    if (!response.Result)
                    {
                        _logger.LogWarning($"Refresh fehlgeschlagen");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler bei Refresh");
                }
            }

            public async Task RefreshAsync(string key, CancellationToken token = default)
            {
                try
                {
                    var response = await _client.RefreshAsync(new GetCacheRequest { Key = key }, cancellationToken: token);

                    if (!response.Result)
                    {
                        _logger.LogWarning($"RefreshAsync fehlgeschlagen");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler bei RefreshAsync");
                }
            }

            public void Remove(string key)
            {
                try
                {
                    var response = _client.Remove(new GetCacheRequest() { Key = key });

                    if (!response.Result)
                    {
                        _logger.LogWarning($"Remove fehlgeschlagen");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler bei Remove");
                }
            }

            public async Task RemoveAsync(string key, CancellationToken token = default)
            {
                try
                {
                    var response = await _client.RemoveAsync(new GetCacheRequest { Key = key }, cancellationToken: token);

                    if (!response.Result)
                    {
                        _logger.LogWarning($"RemoveAsync fehlgeschlagen");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler bei RemoveAsync");
                }
            }
        }
    }
}

