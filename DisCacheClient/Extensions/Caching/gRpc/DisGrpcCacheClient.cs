using DisCache.Protos;
using DisCacheClient.Extensions.Caching.Interfaces;
using DisCacheClient.Extensions.Caching.SignalR;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace DisCacheClient.Extensions.Caching.gRpc
{
    namespace DisCacheClient.Extensions.Caching.gRpc
    {
        public class DisGrpcCacheClient : IDistributedCache, IDiscCacheClient, IDisposable
        {
            private readonly DisCacher.DisCacherClient _client;
            private readonly ILogger<DisGrpcCacheClient> _logger;
            private readonly GrpcChannel _channel;

            public event DisCUpdateHandler? DisCUpdate;
            public event DisCDeleteHandler? DisCDelete;

            private bool _isReady = false;
            private AsyncDuplexStreamingCall<SubscriptionRequest, UpdateNotification>? _subscribeCall;
            private Task? _readTask;
            private CancellationTokenSource _cts = new CancellationTokenSource();

            // Um mehrere Abonnements pro Client zu unterstützen, halten wir ein lokales Verzeichnis der abonnierten Muster.
            private ConcurrentDictionary<string, bool> _patterns = new ConcurrentDictionary<string, bool>();

            public DisGrpcCacheClient(string configuration)
            {
                _logger = null;

                _channel = GrpcChannel.ForAddress(configuration);
                _client = new DisCacher.DisCacherClient(_channel);

                // Starten Sie die Verbindung und den Streaming-Call
                StartSubscription();
            }

            private void StartSubscription()
            {
                _subscribeCall = _client.SubscribeToPattern(cancellationToken: _cts.Token);
                _readTask = Task.Run(async () => await ReadUpdatesAsync(_cts.Token));
                _isReady = true;
            }

            private async Task ReadUpdatesAsync(CancellationToken token)
            {
                try
                {
                    if (_subscribeCall != null)
                    {
                        while (await _subscribeCall.ResponseStream?.MoveNext(token))
                        {
                            var notification = _subscribeCall.ResponseStream.Current;
                            _logger?.LogInformation("Empfangen: Aktion={Action}, Key={Key}", notification.Action, notification.Key);

                            switch (notification.Action.ToLowerInvariant())
                            {
                                case "update":
                                    DisCUpdate?.Invoke(this, notification.Key);
                                    break;
                                case "delete":
                                    DisCDelete?.Invoke(this, notification.Key);
                                    break;
                            }
                        }
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    _logger?.LogInformation(ex, "Abonnement wurde abgebrochen.");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Fehler beim Empfangen von Benachrichtigungen.");
                }
                finally
                {
                    _isReady = false;
                }
            }

            public void Subscribe(string pattern)
            {
                if (!_isReady || _subscribeCall == null)
                {
                    _logger?.LogWarning("Client ist noch nicht bereit.");
                    return;
                }

                if (!_patterns.ContainsKey(pattern))
                {
                    _patterns[pattern] = true;
                    SendSubscriptionRequest(pattern, true).GetAwaiter().GetResult();
                }
            }

            public async Task SubscribeAsync(string pattern)
            {
                if (!_isReady || _subscribeCall == null)
                {
                    _logger.LogWarning("Client ist noch nicht bereit.");
                    return;
                }

                if (!_patterns.ContainsKey(pattern))
                {
                    _patterns[pattern] = true;
                    await SendSubscriptionRequest(pattern, true);
                }
            }

            public void Unsubscribe(string pattern)
            {
                if (!_isReady || _subscribeCall == null)
                {
                    _logger?.LogWarning("Client ist noch nicht bereit.");
                    return;
                }

                if (_patterns.ContainsKey(pattern))
                {
                    _patterns.TryRemove(pattern, out _);
                    SendSubscriptionRequest(pattern, false).GetAwaiter().GetResult();
                }
            }

            public async Task UnsubscribeAsync(string pattern)
            {
                if (!_isReady || _subscribeCall == null)
                {
                    _logger?.LogWarning("Client ist noch nicht bereit.");
                    return;
                }

                if (_patterns.ContainsKey(pattern))
                {
                    _patterns.TryRemove(pattern, out _);
                    await SendSubscriptionRequest(pattern, false);
                }
            }

            private async Task SendSubscriptionRequest(string pattern, bool subscribe)
            {
                try
                {
                    await _subscribeCall!.RequestStream.WriteAsync(new SubscriptionRequest
                    {
                        Pattern = pattern,
                        Subscribe = subscribe
                    });
                    _logger?.LogInformation($"{(subscribe ? "Abonniere" : "Deabonniere")} Muster: {pattern}");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Fehler beim Senden des SubscriptionRequests für Muster: {Pattern}", pattern);
                }
            }

            public bool IsReady()
            {
                return _isReady;
            }

            public void Dispose()
            {
                _cts.Cancel();
                _cts.Dispose();

                // Completen des RequestStreams, um den Server zu informieren, dass keine weiteren Subscriptions kommen.
                if (_subscribeCall != null)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            await _subscribeCall.RequestStream.CompleteAsync();
                        }
                        catch { /* Ignorieren wir evtl. Fehler beim Dispose */ }
                    }).GetAwaiter().GetResult();
                }

                _channel.Dispose();
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
                        _logger?.LogWarning("Get fehlgeschlagen: {Message}", response.Message);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Fehler bei Get");
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
                        _logger?.LogWarning("GetAsync fehlgeschlagen: {Message}", response.Message);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Fehler bei GetAsync");
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
                        _logger.LogWarning("SetAsync fehlgeschlagen: {Response}", response);
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
                        _logger.LogWarning("Refresh fehlgeschlagen");
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
                        _logger.LogWarning("RefreshAsync fehlgeschlagen");
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
                        _logger.LogWarning("Remove fehlgeschlagen");
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
                        _logger.LogWarning("RemoveAsync fehlgeschlagen");
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

