using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PowerService;
using PowerTrades.Builders;
using PowerTrades.Reports;
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
              
                AddForecastServices(services);
                services.AddSingleton<Application>();

            })
            .ConfigureLogging((hostingContext, logging) =>
              // Add more logging if needed
              logging.AddSimpleConsole(options =>
              {
                  options.IncludeScopes = true;
                  options.SingleLine = true;
                  options.UseUtcTimestamp = true;
                  options.TimestampFormat = "MM/dd HH:mm:ss ";
              })
            );

            return builder;

        }

        internal static void AddForecastServices(IServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(Program.configuration);
            services.AddOptions();
            services.AddOptions<PowerTradesOptions>().BindConfiguration(PowerTradesOptions.Name);
            services.AddTransient<PowerTradeCsvBuilder>();
            services.AddTransient<IPowerService, PowerService.PowerService>();
            services.AddTransient<ForecastPowerReport>();
        }

        // Run the application

        internal static async Task<int> Main(string[] args)
        {
            var application = CreateDefaultBuilder(args).Build().Services.GetRequiredService<Application>();
            return await application.ExecuteAsync(args);
        }
    }
}