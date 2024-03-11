using DisCache.Database;
using DisCache.DistributedInMemory.Cache;
using DisCache.gRpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using DisCache.Hubs;
using DisCache.Services;
using EasyJwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NeoSmart.Caching.Sqlite;
using Persistance.Memory.Services;
using Persistance.Sqlite;
using Serilog;
using System;
using Stamon.Model;
using Stamon.Services;
using Stamon.Types.Interfaces;

namespace DisCache
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        private bool UseAuthorisation { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            bool useJwt = Configuration.GetValue<bool>("DisC:UseJwt");
            bool useRest = Configuration.GetValue<bool>("DisC:UseRest");
            bool useGrpc = Configuration.GetValue<bool>("DisC:UsegRpc");
            bool useSignalR = Configuration.GetValue<bool>("DisC:UseSignalR");
            bool useEvents = Configuration.GetValue<bool>("DisC:Events");
            UseAuthorisation = Configuration.GetValue<bool>("DisC:UseAuthorisation");
            DisCache.Configuration.DbPath = Configuration.GetValue<string>("DisC:DbPath");

            DisCacheConfiguration.UseEvents = useEvents;

            bool useInMemory = Configuration.GetValue<bool>("CacheBackend:UseInMemory");
            DisCache.Configuration.UseMemoryCacheAccelerator = Configuration.GetValue<bool>("CacheBackend:UseInMemoryAccelerator");
            DisCache.Configuration.UseMemoryCacheAcceleratorTime = Configuration.GetValue<int>("CacheBackend:UseInMemoryAcceleratorTime");

            bool useSqlite = Configuration.GetValue<bool>("CacheBackend:UseSqlite");
            bool useRedis = Configuration.GetValue<bool>("CacheBackend:UseRedis");

            string sqlitePath = Configuration.GetValue<string>("CacheBackend.UseSqlite:Path");
            string statPath = Configuration.GetValue<string>("Monitor:EF");

            RuntimeInformation.Startup = DateTime.Now;
            RuntimeInformation.Version = "1.0 PR";
            RuntimeInformation.Name = Configuration.GetValue<string>("DisC:Name");

            IConfigurationSection sec = Configuration.GetSection("Monitor");

            var test = Configuration.GetValue<string>("Monitor:Name");

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });

            services.AddMemoryCache();

            if (useInMemory)
            {
                RuntimeInformation.Backend = "Memory";
                RuntimeInformation.BackendInformation = "MemoryDistributedCache";
                services.AddSingleton<MemoryDistributedCache>();
            }

            bool usingSqlite = false;
            bool usingRedis = false;

            if (useSqlite)
            {
                RuntimeInformation.Backend = "SQLite";
                RuntimeInformation.BackendInformation = $"Storage path: {sqlitePath}";
                usingSqlite = true;
                services.AddSqliteCache(options =>
                {
                    options.CachePath = sqlitePath;
                });
            }

            if (useRedis)
            {
                RuntimeInformation.Backend = "Redis";
                RuntimeInformation.BackendInformation = $"{Configuration.GetValue<string>("Redis:Server")}:{Configuration.GetValue<int>("Redis:Port")}";
                usingRedis = true;
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration =
                        $"{Configuration.GetValue<string>("Redis:Server")}:{Configuration.GetValue<int>("Redis:Port")}";
                });
            }

            if (!((usingRedis || usingSqlite) && useInMemory))
            {
                RuntimeInformation.Backend = "Memory";
                RuntimeInformation.BackendInformation = "Microsoft DistributedMemoryCache";
                services.AddDistributedMemoryCache();
            }

            IConfigurationSection monitorSec = Configuration.GetSection("Monitor");
            services.Configure<StarisConfig>(monitorSec);

            services.AddSingleton<StamonService>();

            string statSqitePath = Configuration.GetValue<string>("Monitor:EF");

            services.AddSqliteProvider(options =>
            {
                options.CachePath = statSqitePath;
            });

            services.AddSingleton<IStamonMemoryPersistance, PersistanceService>();

            services.AddSingleton<CacheCore>();

            services.AddSingleton<KeyService>();

            if (UseAuthorisation)
            {
                services.AddEasyJwt(Configuration);
                services.AddSingleton<IOptions<EasyJwt.JwtConfig>>();
            }

            if (useRest)
            {
                services.AddControllers();

                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DisCache", Version = "v1" });
                });
            }

            if (useGrpc)
                services.AddGrpc();

            if (useSignalR)
                services.AddSignalR(
                    o =>
                    {
                        o.EnableDetailedErrors = true;
                        o.MaximumReceiveMessageSize = 10240000;
                        o.StreamBufferCapacity = 1000;
                        o.MaximumParallelInvocationsPerClient = 200;
                    }).AddMessagePackProtocol();

            try
            {
                using var db = new DiscContext();
                db.Database.Migrate();
            }
            catch (Exception /*ex*/)
            {
                
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            bool useSwagger = Configuration.GetValue<bool>("DisC:Swagger");
            string swaggerEndpoint = Configuration.GetValue<string>("DisC:SwaggerEndpoint");


            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DisCache v1"));
            }
            else
            {
                if (useSwagger)
                {
                    app.UseDeveloperExceptionPage();
                    app.UseSwagger();
                    app.UseSwaggerUI(c => c.SwaggerEndpoint(swaggerEndpoint, "DisCache v1"));
                }
            }
            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors("CorsPolicy");

            app.UseSerilogRequestLogging();

            app.UseAuthorization();
            if (UseAuthorisation)
                app.UseAuthentication();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<DisCacheService>();
                endpoints.MapControllers();
                endpoints.MapHub<CacheHub>("/CacheHub");
            });
        }
    }
}
