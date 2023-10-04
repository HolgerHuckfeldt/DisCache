#nullable enable
using DisCacheClient.Exceptions;
using DisCacheClient.Extensions.Caching.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

#pragma warning disable CS8603 // Mögliche Nullverweisrückgabe.
namespace DisCacheClient.Extensions.Caching.SignalR
{

    public delegate void DisCUpdateHandler(object sender, string command, string key);

    public class DisSignalRCacheClient : IDistributedCache, IDiscCacheClient
    {

        private readonly HubConnection _connection;
        private bool _connected;
        //private bool _initialized = false;
        private bool _firstCall = true;

        private readonly Timer _nextTryTimer;

        public event DisCUpdateHandler? DisCUpdate;

        public DisSignalRCacheClient(string baseUrl)
        {
            if (!baseUrl.EndsWith("/"))
                baseUrl += "/";

            _nextTryTimer = new Timer();
            _nextTryTimer.Interval = TimeSpan.FromMinutes(2).TotalMilliseconds;
            _nextTryTimer.AutoReset = true;
            _nextTryTimer.Elapsed += NextTryTimerOnElapsed;

            var reconnectTimes = new List<TimeSpan>()
            {
                TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),TimeSpan.FromSeconds(5),TimeSpan.FromSeconds(5),TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),TimeSpan.FromSeconds(10),TimeSpan.FromSeconds(10),TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30),TimeSpan.FromSeconds(30),TimeSpan.FromSeconds(30),TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(60),TimeSpan.FromSeconds(60),TimeSpan.FromSeconds(60),TimeSpan.FromSeconds(60),
                TimeSpan.FromMinutes(5),TimeSpan.FromMinutes(5),TimeSpan.FromMinutes(5),TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(10),TimeSpan.FromMinutes(10),TimeSpan.FromMinutes(10),TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(10),TimeSpan.FromMinutes(10),TimeSpan.FromMinutes(10),TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(10),TimeSpan.FromMinutes(10),TimeSpan.FromMinutes(10),TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(10),TimeSpan.FromMinutes(10),TimeSpan.FromMinutes(10),TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(30),TimeSpan.FromMinutes(30),TimeSpan.FromMinutes(30),TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(30),TimeSpan.FromMinutes(30),TimeSpan.FromMinutes(30),TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(30),TimeSpan.FromMinutes(30),TimeSpan.FromMinutes(30),TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(60),TimeSpan.FromMinutes(60),TimeSpan.FromMinutes(60),TimeSpan.FromMinutes(60),
                TimeSpan.FromMinutes(60),TimeSpan.FromMinutes(60),TimeSpan.FromMinutes(60),TimeSpan.FromMinutes(60)
            };

            _connection = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}CacheHub")
                .AddMessagePackProtocol()
                .WithAutomaticReconnect(reconnectTimes.ToArray())
                .Build();
        }

        private async void NextTryTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            await Connect();
        }


        public bool IsReady()
        {
            if (_firstCall)
            {
                _firstCall = false;
                return true;
            }

            return _connection.State == HubConnectionState.Connected;
        }

        public byte[]? Get(string key)
        {
            try
            {
                if (!_connected)
                {
                    var t = Connect();
                    t.Wait();
                }

                if (_connection.InvokeCoreAsync(
                        "Get",
                        typeof(SignalRResponse),
                        new object?[] { key }
                    ).Result is SignalRResponse r)
                {
                    return r.Data;
                }
            }
            catch (Exception ex)
            {
                throw new DiscException("Error getting item. See inner exception for details.", ex);
            }

            return null;

        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            try
            {
                if (!_connected)
                    await Connect();

                var r = await _connection.InvokeCoreAsync(
                    "Get",
                    typeof(SignalRResponse),
                    new object?[] { key }
                ) as SignalRResponse;


                return r?.Data;
            }
            catch (Exception ex)
            {
                throw new DiscException("Error getting item. See inner exception for details.", ex);
            }
            
        }

        public async Task<byte[]> PingAsync(CancellationToken token = default)
        {
            try
            {
                if (!_connected)
                    await Connect();

                var r = await _connection.InvokeCoreAsync(
                    "Ping",
                    typeof(SignalRResponse),
                    new object?[] { "Ping" }
                ) as SignalRResponse;


                return r?.Data;
            }
            catch (Exception ex)
            {
                throw new DiscException("Error pinging server. See inner exception for details.", ex);
            }
        }

        public void Refresh(string key)
        {
            try
            {
                if (!_connected)
                {
                    var t = Connect();
                    t.Wait();
                }

                var r = _connection.InvokeCoreAsync(
                    "Refresh",
                    typeof(SignalRResponse),
                    args: new object?[] { key }
                ).Result;
            }
            catch (Exception ex)
            {
                throw new DiscException("Error refreshing item. See inner exception for details.", ex);
            }
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            try
            {

                if (!_connected)
                    await Connect();

                var r = await _connection.InvokeCoreAsync(
                    "Refresh",
                    typeof(SignalRResponse),
                    new object?[] { key }
                );
            }
            catch (Exception ex)
            {
                throw new DiscException("Error refreshing item. See inner exception for details.", ex);
            }
        }

        public void Remove(string key)
        {
            try
            {


                if (!_connected)
                {
                    var t = Connect();
                    t.Wait();
                }

                var r = _connection.InvokeCoreAsync(
                    "Remove",
                    typeof(SignalRResponse),
                    new object?[] { key }
                ).Result;
            }
            catch (Exception ex)
            {
                throw new DiscException("Error removing item. See inner exception for details.", ex);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken ct = default)
        {
            try
            {
                if (!_connected)
                    await Connect();

                var r = await _connection.InvokeCoreAsync(
                    "Remove",
                    typeof(SignalRResponse),
                    new object?[] { key },
                    ct
                );
            }
            catch (Exception ex)
            {
                throw new DiscException("Error removing item. See inner exception for details.", ex);
            }
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            try
            {

                if (!_connected)
                {
                    var t = Connect();
                    t.Wait();
                }

                var cacheOptions = new CacheOptions()
                {
                    AbsoluteExpiration = options.AbsoluteExpiration?.DateTime ?? null,
                    AbsoluteExpirationRelativeToNow =
                        options.AbsoluteExpirationRelativeToNow?.TotalMilliseconds ?? null,
                    SlidingExpiration = options.SlidingExpiration?.TotalMilliseconds ?? null
                };

                /*var result = */ _connection.InvokeCoreAsync(
                    "SendData",
                    typeof(SignalRResponse),
                    new object?[] { key, value, cacheOptions }
                );

                //var response = result.Result;
            }
            catch (Exception ex)
            {
                throw new DiscException("Error setting item. See inner exception for details.", ex);
            }
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            try
            {

                if (!_connected)
                    await Connect();

                var cacheOptions = new CacheOptions()
                {
                    AbsoluteExpiration = options.AbsoluteExpiration?.DateTime ?? null,
                    AbsoluteExpirationRelativeToNow =
                        options.AbsoluteExpirationRelativeToNow?.TotalMilliseconds ?? null,
                    SlidingExpiration = options.SlidingExpiration?.TotalMilliseconds ?? null
                };

                await _connection.InvokeCoreAsync(
                    "SendData",
                    typeof(SignalRResponse),
                    new object?[] { key, value, cacheOptions },
                    token);
            }
            catch (Exception ex)
            {
                throw new DiscException("Error setting item. See inner exception for details.", ex);
            }
        }


        private async Task Connect()
        {

            _connection.On<string, string>("Update", (command, key) =>
            {
                DisCUpdate?.Invoke(this, command, key);
            });

            //_connection.HandshakeTimeout = TimeSpan.FromSeconds(10);
            //_connection.ServerTimeout = TimeSpan.FromSeconds(5);
            _connection.Closed += ConnectionOnClosed;
            _connection.Reconnecting += ConnectionOnReconnecting;
            _connection.Reconnected += ConnectionOnReconnected;

            try
            {
                //_initialized = true;

                await _connection.StartAsync();
                _nextTryTimer.Stop();
                _connected = true;
            }
            catch (HttpRequestException /*rex*/)
            {
                // Network-Exception. Retry...
                _nextTryTimer.Start();
            }
            catch (Exception ex)
            {
                throw new DiscException("Error connecting to SignalR-Host. See inner exception for details.", ex);
            }
        }

        private Task ConnectionOnReconnected(string? arg)
        {
            return Task.CompletedTask;
        }

        private Task ConnectionOnReconnecting(Exception? arg)
        {
            return Task.CompletedTask;
        }

        private Task ConnectionOnClosed(Exception? arg)
        {
            return Task.CompletedTask;
        }
    }
}

#pragma warning restore CS8603 // Mögliche Nullverweisrückgabe.
