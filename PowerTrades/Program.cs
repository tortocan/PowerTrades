using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("PowerTrades.Tests")]

namespace PowerTrades
{
    internal static class Program
    {
        private static IConfigurationRoot configuration;

        // ----------------------------------------------------
        // My configuration part of the application
        // ----------------------------------------------------
        internal static IHostBuilder CreateDefaultBuilder(string[]? args = null)
        {
            var builder = Host.CreateDefaultBuilder(args ?? Array.Empty<string>())

            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                // Add more config files if needed
                var basePath = Directory.GetCurrentDirectory();
                config.SetBasePath(basePath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment}.json", true, true)
                .AddEnvironmentVariables();
                configuration = config.Build();
            }
            )
            .ConfigureServices((hostingContext, services) =>
            {
                // The "Application" service is the application
                // itself. Add all other dependencies hereafter
                services.AddSingleton(configuration);
                services.AddOptions();
                services.AddOptions<PowerTradesOptions>().BindConfiguration(PowerTradesOptions.Name);
                services.AddSingleton<Application>();
            })
            .ConfigureLogging((hostingContext, logging) =>
              // Add more logging if needed
              logging.AddConsole()
            );

            return builder;

        }

        // Run the application

        internal static async Task<int> Main(string[] args)
        {
            var application = CreateDefaultBuilder(args).Build().Services.GetRequiredService<Application>();
            return await application.ExecuteAsync(args);
        }
    }
}