using DisCache.Protos;
using DisCacheClient.Extensions.Caching.gRpc;
using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace DisCacheClient.Extensions.Caching
{
    public static class DisCacheServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Dis caching services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">An <see cref="Action"/> to configure the provided
        /// <see cref="DisCacheOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddDisCacheCache(this IServiceCollection services, Action<DisCacheOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.Configure(setupAction);

            //services.AddSingleton<GrpcRegister>();

            services.AddOptions();

            services.AddSingleton<MemoryDistributedCache>();
            services.Add(ServiceDescriptor.Singleton<IDistributedCache, DisCacheClient>());

            return services;
        }
    }
}
