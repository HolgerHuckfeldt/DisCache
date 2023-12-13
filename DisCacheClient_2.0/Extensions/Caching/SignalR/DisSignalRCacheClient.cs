using DisCacheClient.Exceptions;
using DisCacheClient.Extensions.Caching.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

#pragma warning disable CS8603 // Mögliche Nullverweisrückgabe.
namespace DisCacheClient.Extensions.Caching.SignalR
{
    public delegate void DisCUpdateHandler(object sender, string key);
    public delegate void DisCDeleteHandler(object sender, string key);

    public class DisSignalRCacheClient : IDistributedCache, IDiscCacheClient
    {
        private readonly HubConnection _connection;
        private bool _connected;
        //private bool _initialized = false;
        private bool _firstCall = true;

        private readonly Timer _nextTryTimer;

        public event DisCUpdateHandler DisCUpdate;
        public event DisCDeleteHandler DisCDelete;

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

        public byte[] Get(string key)
        {
            if (!_connected)
            {
                var t = Connect();
                t.Wait();
            }

            if (_connection.InvokeCoreAsync(
                    "Get",
                    typeof(SignalRResponse),
                    new object[] { key }
                ).Result is SignalRResponse r)
            {
                return r.Data;
            }


            return null;

        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            if (!_connected)
                await Connect();

            var r = await _connection.InvokeCoreAsync(
                "Get",
                typeof(SignalRResponse),
                new object[]
                {
                    key
                }
            ) as SignalRResponse;


            return r?.Data;
        }

        public async Task<byte[]> PingAsync(CancellationToken token = default)
        {
            if (!_connected)
                await Connect();

            var r = await _connection.InvokeCoreAsync(
                "Ping",
                typeof(SignalRResponse),
                new object[]
                {
                    "Ping"
                }
            ) as SignalRResponse;


            return r?.Data;
        }

        public void Refresh(string key)
        {
            if (!_connected)
            {
                var t = Connect();
                t.Wait();
            }

            var r = _connection.InvokeCoreAsync(
                "Refresh",
                typeof(SignalRResponse),
                args: new object[]
                {
                    key
                }
            ).Result;

        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            if (!_connected)
                await Connect();

            var r = await _connection.InvokeCoreAsync(
                "Refresh",
                typeof(SignalRResponse),
                new object[]
                {
                    key
                }
            );
        }

        public void Remove(string key)
        {
            if (!_connected)
            {
                var t = Connect();
                t.Wait();
            }
            var r = _connection.InvokeCoreAsync(
                "Remove",
                typeof(SignalRResponse),
                new object[]
                {
                    key
                }
            ).Result;
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            if (!_connected)
                await Connect();

            var r = await _connection.InvokeCoreAsync(
                "Remove",
                typeof(SignalRResponse),
                new object[]
                {
                    key
                },
                token
            );
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
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

            var result = _connection.InvokeCoreAsync(
                "SendData",
                typeof(SignalRResponse),
                new object[]
                {
                    key,
                    value,
                    cacheOptions
                }
            );

            var response = result.Result;
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
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
                new object[]
                {
                    key,
                    value,
                    cacheOptions
                },
                token);

        }
        private async Task Connect()
        {

            _connection.On<string, string>("Update", (command, key) =>
            {
                switch (command)
                {
                    case "Update":
                        DisCUpdate?.Invoke(this, key);
                        break;
                    case "Delete":
                        DisCDelete?.Invoke(this, key);
                        break;
                }
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

        private Task ConnectionOnReconnected(string arg)
        {
            return Task.CompletedTask;
        }

        private Task ConnectionOnReconnecting(Exception arg)
        {
            return Task.CompletedTask;
        }

        private Task ConnectionOnClosed(Exception arg)
        {
            return Task.CompletedTask;
        }
    }
}
