using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace DisCacheClient.Extensions.Caching.SignalR
{
    public class DisSignalRCacheClientHubConnection
    {
        public HubConnection Connection { get; set; }
    }
}
