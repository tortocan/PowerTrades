using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
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
                BuildLogger(logging)
            );

            return builder;
        }

        private static ILoggingBuilder BuildLogger(ILoggingBuilder logging)
        {
            return logging.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.UseUtcTimestamp = true;
                options.TimestampFormat = "MM/dd HH:mm:ss ";
            });
        }

        internal static void AddForecastServices(IServiceCollection services)
        {
            services.AddSingleton(configuration);
            services.AddOptions();
            services.AddOptions<PowerTradesOptions>().BindConfiguration(PowerTradesOptions.Name);
            services.AddTransient<PowerTradeCsvBuilder>();
            services.AddTransient<IPowerService, PowerService.PowerService>();
            services.AddTransient<ForecastPowerReport>();
            services.AddSingleton<TimedHostedService>();
        }

        // Run the application

        internal static int Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(x => BuildLogger(x));
            var logger = loggerFactory.CreateLogger(nameof(Program));
            try
            {
                Policy.Handle<Exception>()
                 .Retry(1, (e, i) =>
                 {
                     logger.LogError(e, $"Retry ({i}) An exception has occured: {e.Message}");
                 }).Execute(() =>
                 {
                     var application = CreateDefaultBuilder(args).Build().Services.GetRequiredService<Application>();
                     using var cancellationTokenSource = new CancellationTokenSource();
                     Console.CancelKeyPress += (s, e) =>
                     {
                         logger.LogCritical("Canceling...");
                         cancellationTokenSource.Cancel();
                         e.Cancel = true;
                     };

                     var result = 0;
                     var results = application.ExecuteAsService(args, cancellationTokenSource.Token);
                     foreach (var item in results)
                     {
                         logger.LogWarning($"Yield results {results.Count()}");
                         if (result == 0)
                         {
                             result = item;
                         }
                     }

                     return result;
                 });
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, ex.Message);
                throw;
            }

            return 1;
        }
    }
}