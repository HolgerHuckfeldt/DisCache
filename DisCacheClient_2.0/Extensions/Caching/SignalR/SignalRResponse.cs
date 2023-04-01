using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DisCacheClient.Extensions.Caching.SignalR
{
    public class SignalRResponse
    {
        public bool Success { get; set; }
        public byte[] Data { get; set; }
        public string Message { get; set; }
    }
}
