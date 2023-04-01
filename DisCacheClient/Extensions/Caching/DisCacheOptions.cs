using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace DisCacheClient.Extensions.Caching
{
    public class DisCacheOptions : IOptions<DisCacheOptions>
    {
        /// <summary>
        /// The configuration used to connect to Redis.
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// Use the Grpc-Endpoint
        /// </summary>
        public CacheType CacheType { get; set; }

        public bool UseFallback { get; set; } = true;

        public string Username { get; set; }

        public string Password { get; set; }

        DisCacheOptions IOptions<DisCacheOptions>.Value
        {
            get { return this; }
        }
    }
}
