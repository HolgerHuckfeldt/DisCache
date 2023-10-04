using DisCacheClient.Exceptions;
using DisCacheClient.Extensions.Caching.Interfaces;
using System.Web;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DisCacheClient.Extensions.Caching.REST
{
    public class DisRestCacheClient : IDistributedCache, IDiscCacheClient
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl;
        private string _jtoken = String.Empty;

        public DisRestCacheClient(string baseUrl)
        {
            _client = new HttpClient();
            _baseUrl = baseUrl;

            if (!_baseUrl.EndsWith("/"))
                _baseUrl += "/";
        }

        public bool IsReady()
        {
            return Ping();
        }

        public byte[] Get(string key)
        {
            try
            {
                var result = _client.GetAsync($"{_baseUrl}api/Cache/{key}").Result;

                if (result.IsSuccessStatusCode)
                {
                    var cachedValue = result.Content.ReadAsByteArrayAsync().Result;
                    return cachedValue;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new DiscException("Error getting item. See inner exception for details.", ex);
            }
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            try
            {

                var result = await _client.GetAsync($"{_baseUrl}api/Cache/{key}", token);
                if (result.IsSuccessStatusCode)
                {
                    return await result.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    if (result.StatusCode == HttpStatusCode.NotFound)
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DiscException("Error getting item. See inner exception for details.", ex);
            }
            return null;
        }

        public void Refresh(string key)
        {
            var result = _client.PutAsync($"{_baseUrl}api/cache/{key}", new StringContent("")).Result;
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {

            await _client.DeleteAsync($"{_baseUrl}api/cache/{key}", token);

        }

        public void Remove(string key)
        {

            var result = _client.DeleteAsync($"{_baseUrl}api/cache/{key}").Result;

        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {

            await _client.DeleteAsync($"{_baseUrl}api/cache/{key}", token);

        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {

            var cacheOptions = new CacheOptions()
            {
                AbsoluteExpiration = options.AbsoluteExpiration?.DateTime ?? null,
                AbsoluteExpirationRelativeToNow =
                    options.AbsoluteExpirationRelativeToNow?.TotalMilliseconds ?? null,
                SlidingExpiration = options.SlidingExpiration?.TotalMilliseconds ?? null
            };

            var optionsString = HttpUtility.UrlEncode(System.Text.Json.JsonSerializer.Serialize(cacheOptions));

            var multiForm = new MultipartFormDataContent
                {
                    { new StringContent("CustomField1"), optionsString }
                };

            multiForm.Add(new StreamContent(new MemoryStream(value)), "file", key);

            var response = _client.PostAsync($"{_baseUrl}api/Cache", multiForm).Result;

            if (response.IsSuccessStatusCode)
            {
                var responseString = response.Content.ReadAsStringAsync().Result;
            }

        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {

            var cacheOptions = new CacheOptions()
            {
                AbsoluteExpiration = options.AbsoluteExpiration?.DateTime ?? null,
                AbsoluteExpirationRelativeToNow =
                    options.AbsoluteExpirationRelativeToNow?.TotalMilliseconds ?? null,
                SlidingExpiration = options.SlidingExpiration?.TotalMilliseconds ?? null
            };

            var optionsString = HttpUtility.UrlEncode(System.Text.Json.JsonSerializer.Serialize(cacheOptions));

            var multiForm = new MultipartFormDataContent
                {
                    { new StringContent("CustomField1"), optionsString }
                };

            multiForm.Add(new StreamContent(new MemoryStream(value)), "file", key);

            var response = await _client.PostAsync($"{_baseUrl}api/Cache", multiForm, token);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
            }

        }

        private string Authorise(string user, string password)
        {
            return string.Empty;
        }

        private bool Ping()
        {
            var result = _client.GetAsync($"{_baseUrl}api/cache/Ping").Result;
            return result.IsSuccessStatusCode;
        }
    }
}
