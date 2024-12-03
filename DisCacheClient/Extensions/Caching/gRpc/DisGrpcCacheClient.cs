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
            private readonly DisCache.Protos. _client;
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
                    var response = _client.Get(new GetRequest { Key = key });

                    if (response.Success)
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
                    var response = await _client.GetAsync(new GetRequest { Key = key }, cancellationToken: token);

                    if (response.Success)
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
                    var cacheOptions = new CacheOptions
                    {
                        AbsoluteExpiration = options.AbsoluteExpiration?.ToUnixTimeMilliseconds() ?? 0,
                        SlidingExpiration = options.SlidingExpiration?.TotalMilliseconds ?? 0,
                        AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow?.TotalMilliseconds ?? 0
                    };

                    var request = new SendDataRequest
                    {
                        Key = key,
                        Data = Google.Protobuf.ByteString.CopyFrom(value),
                        CacheOptions = cacheOptions
                    };

                    var response = _client.SendData(request);

                    if (!response.Success)
                    {
                        _logger.LogWarning($"Set fehlgeschlagen: {response.Message}");
                    }
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
                    var cacheOptions = new CacheOptions
                    {
                        AbsoluteExpiration = options.AbsoluteExpiration?.ToUnixTimeMilliseconds() ?? 0,
                        SlidingExpiration = options.SlidingExpiration?.TotalMilliseconds ?? 0,
                        AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow?.TotalMilliseconds ?? 0
                    };

                    var request = new SendDataRequest
                    {
                        Key = key,
                        Data = Google.Protobuf.ByteString.CopyFrom(value),
                        CacheOptions = cacheOptions
                    };

                    var response = await _client.SendDataAsync(request, cancellationToken: token);

                    if (!response.Success)
                    {
                        _logger.LogWarning($"SetAsync fehlgeschlagen: {response.Message}");
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
                    var response = _client.Refresh(new RefreshRequest { Key = key });

                    if (!response.Success)
                    {
                        _logger.LogWarning($"Refresh fehlgeschlagen: {response.Message}");
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
                    var response = await _client.RefreshAsync(new RefreshRequest { Key = key }, cancellationToken: token);

                    if (!response.Success)
                    {
                        _logger.LogWarning($"RefreshAsync fehlgeschlagen: {response.Message}");
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
                    var response = _client.Remove(new RemoveRequest { Key = key });

                    if (!response.Success)
                    {
                        _logger.LogWarning($"Remove fehlgeschlagen: {response.Message}");
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
                    var response = await _client.RemoveAsync(new RemoveRequest { Key = key }, cancellationToken: token);

                    if (!response.Success)
                    {
                        _logger.LogWarning($"RemoveAsync fehlgeschlagen: {response.Message}");
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

