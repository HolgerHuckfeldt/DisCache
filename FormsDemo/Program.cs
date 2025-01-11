using DisCacheClient.Extensions.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace FormsDemo
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Configure Serilog as the global logging provider.
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console() // Log output to console (can be changed to file, etc.)
                .CreateLogger();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Create a service collection and configure services.
            var services = new ServiceCollection();
            ConfigureServices(services);

            // Build the service provider.
            using var serviceProvider = services.BuildServiceProvider();

            // Use the DI container to create an instance of the MainForm.
            var mainForm = serviceProvider.GetRequiredService<Form1>();
            Application.Run(mainForm);
        }

        private static void ConfigureServices(IServiceCollection services)
        {

            services.AddSingleton<ILogger>(Log.Logger); // Register the logger with DI container.

            services.AddSingleton<Form1>();

            services.AddDisCacheCache(
                options => new DisCacheOptions
                {
                    Configuration = "https://localhost:5001/",
                    CacheType = CacheType.gRpc,
                    UseFallback = false
                }
            );


        }
    }
}
