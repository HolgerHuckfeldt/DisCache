using DisCacheClient.Extensions.Caching.SignalR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DisCacheClient.Extensions.Caching.Interfaces
{
    internal interface IDiscCacheClient
    {
        event DisCUpdateHandler DisCUpdate;
        event DisCDeleteHandler DisCDelete;

        void Subscribe(string pattern);

        void Unsubscribe(string pattern);

        Task SubscribeAsync(string pattern);

        Task UnsubscribeAsync(string pattern);

        public bool IsReady();
    }

    //internal interface ISubscribeDiscCacheClient : IDiscCacheClient
    //{

    //}
}
