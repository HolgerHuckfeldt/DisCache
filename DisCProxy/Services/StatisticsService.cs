using DisCProxy.Model;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DisCProxy.Services
{
    public class StatisticsService
    {
        private string _baseUri;
        private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };

        public StatisticsService(string _endpoint)
        {
            _baseUri= _endpoint;
        }

        public async Task<List<ActionPerTime>?> GetRequests()
        {
            using HttpClient client = new HttpClient();
            var request = await client.GetAsync($"{_baseUri}/api/Statistic/requests");
            if (request.IsSuccessStatusCode)
            {
                var result = await 
                    JsonSerializer.DeserializeAsync<List<ActionPerTime>>(await request.Content.ReadAsStreamAsync(), _jsonOptions);

                return result;
            }

            return new List<ActionPerTime>();
        }

        public async Task<List<ActionPerTime>?> GetRequests(string name)
        {
            using HttpClient client = new HttpClient();
            var request = await client.GetAsync($"{_baseUri}/api/Statistic/requests/{name}");
            if (request.IsSuccessStatusCode)
            {
                var result = await
                    JsonSerializer.DeserializeAsync<List<ActionPerTime>>(await request.Content.ReadAsStreamAsync(), _jsonOptions);

                return result;
            }

            return new List<ActionPerTime>();
        }

        public async Task<List<ActionPerTime>?> GetRequests(StatisticRequest request)
        {
            using HttpClient client = new HttpClient();

            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            HttpResponseMessage result = await client.PostAsync($"{_baseUri}/api/Statistic/requests", content);
            if (result.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<List<ActionPerTime>>(await result.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions(){PropertyNameCaseInsensitive = true});
            }

            return new List<ActionPerTime>();
        }

        public async Task<List<ActionPerTime>?> GetHits()
        {

            using HttpClient client = new HttpClient();
            var request = await client.GetAsync($"{_baseUri}/api/Statistic/hits");
            if (request.IsSuccessStatusCode)
            {
                var result = await
                    JsonSerializer.DeserializeAsync<List<ActionPerTime>>(await request.Content.ReadAsStreamAsync(), _jsonOptions);

                return result;
            }

            return new List<ActionPerTime>();
        }

        public async Task<List<ActionPerTime>?> GetHits(string name)
        {

            using HttpClient client = new HttpClient();
            var request = await client.GetAsync($"{_baseUri}/api/Statistic/hits/{name}");
            if (request.IsSuccessStatusCode)
            {
                var result = await
                    JsonSerializer.DeserializeAsync<List<ActionPerTime>>(await request.Content.ReadAsStreamAsync(), _jsonOptions);

                return result;
            }

            return new List<ActionPerTime>();
        }

        public async Task<List<ActionPerTime>?> GetMiss()
        {

            using HttpClient client = new HttpClient();
            var request = await client.GetAsync($"{_baseUri}/api/Statistic/misses");
            if (request.IsSuccessStatusCode)
            {
                var result = await
                    JsonSerializer.DeserializeAsync<List<ActionPerTime>>(await request.Content.ReadAsStreamAsync(), _jsonOptions);

                return result;
            }

            return new List<ActionPerTime>();
        }

        public async Task<List<ActionPerTime>?> GetMiss(string name)
        {

            using HttpClient client = new HttpClient();
            var request = await client.GetAsync($"{_baseUri}/api/Statistic/misses/{name}");
            if (request.IsSuccessStatusCode)
            {
                var result = await
                    JsonSerializer.DeserializeAsync<List<ActionPerTime>>(await request.Content.ReadAsStreamAsync(), _jsonOptions);

                return result;
            }

            return new List<ActionPerTime>();
        }

        public async Task<List<ActionPerTime>?> GetInserts()
        {

            using HttpClient client = new HttpClient();
            var request = await client.GetAsync($"{_baseUri}/api/Statistic/inserts");
            if (request.IsSuccessStatusCode)
            {
                var result = await
                    JsonSerializer.DeserializeAsync<List<ActionPerTime>>(await request.Content.ReadAsStreamAsync(), _jsonOptions);

                return result;
            }

            return new List<ActionPerTime>();
        }

        public async Task<List<ActionPerTime>?> GetInserts(string name)
        {

            using HttpClient client = new HttpClient();
            var request = await client.GetAsync($"{_baseUri}/api/Statistic/inserts/{name}");
            if (request.IsSuccessStatusCode)
            {
                var result = await
                    JsonSerializer.DeserializeAsync<List<ActionPerTime>>(await request.Content.ReadAsStreamAsync(), _jsonOptions);

                return result;
            }

            return new List<ActionPerTime>();
        }

        public async Task<SystemInformation?> GetSystemInformation()
        {

            using HttpClient client = new HttpClient();
            var request = await client.GetAsync($"{_baseUri}/api/Statistic/information");
            if (request.IsSuccessStatusCode)
            {
                var result = await
                    JsonSerializer.DeserializeAsync<SystemInformation>(await request.Content.ReadAsStreamAsync(), _jsonOptions);

                return result;
            }

            return new SystemInformation();
        }


    }
}
