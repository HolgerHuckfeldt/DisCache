using System;
using Microsoft.Extensions.DependencyInjection;
using Persistance.LiteDb.Services;
using Stamon.Types.Interfaces;

namespace Persistance.LiteDb
{
    public static class LiteDbProviderServiceCollectionExtensions
    {
        public static IServiceCollection AddLiteDbProvider(this IServiceCollection services,
            Action<LiteDbOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddOptions();
            services.AddSingleton<IStamonDatabasePersistance, LiteDbService>();
            services.Configure(setupAction);

            return services;
        }

        public static IServiceCollection AddLiteDbProvider(this IServiceCollection services,
            string databasePath)
        {
            return AddLiteDbProvider(services, options => options.DatabasePath = databasePath);
        }
    }
}
