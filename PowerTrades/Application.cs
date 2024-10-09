using DotMake.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("PowerTrades.Tests")]

namespace PowerTrades
{
    // ----------------------------------------------------
    // This is the business logic of my application.
    // It's here that I define what the application will do
    // and which parameters it requires
    // ----------------------------------------------------
    public class Application
    {
        private readonly ILogger<Application> logger;
        private readonly IOptions<PowerTradesOptions> options;
        private readonly TimedHostedService timedHostedService;
        private readonly IHostingEnvironment hostingEnvironment;
        private readonly CliSettings settings;
        public Application(ILogger<Application> logger, IOptions<PowerTradesOptions> options, TimedHostedService timedHostedService, IHostingEnvironment hostingEnvironment)
        {
            this.logger = logger;
            this.options = options;
            this.timedHostedService = timedHostedService;
            this.hostingEnvironment = hostingEnvironment;
            settings = new CliSettings()
            {
                EnableEnvironmentVariablesDirective = true
            };
        }

        public async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
        {
            var result = 1;
            try
            {
                using var scope = logger.BeginScope(hostingEnvironment.EnvironmentName);
                logger.LogInformation($"Recevied arguments: {string.Join(" ", args)}");
                Cli.Ext.ConfigureServices(services =>
                {
                    services.AddLogging(logging => logging.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.UseUtcTimestamp = true;
                        options.TimestampFormat = "MM/dd HH:mm:ss ";
                    }));
                    Program.AddForecastServices(services);
                });


                if (!string.IsNullOrWhiteSpace(options.Value.WorkingDirectory))
                {
                    var cmd = Cli.Parse<RootCliCommand>(args, settings);

                    if (cmd.Tokens?.Any(x => x.Value == RootCliCommand.WorkingDirOptionName) == false)
                    {
                        args = [RootCliCommand.WorkingDirOptionName, options.Value.WorkingDirectory];
                    }
                }


                logger.LogInformation($"options.Value.WorkingDirectory: {options.Value.WorkingDirectory}");
                result = await Cli.RunAsync<RootCliCommand>(args, settings, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            return result;
        }

        public IEnumerable<int> ExecuteAsService(string[] args, CancellationToken cancellationToken = default)
        {
            yield return ExecuteAsync(args, cancellationToken).GetAwaiter().GetResult();
            var serviceTask = async () => await timedHostedService.WithInterval(options.Value.ExtractInterval)
                  .WithJob(async () => await ExecuteAsync(args, cancellationToken))
                 .StartAsync(cancellationToken);
            serviceTask();
            var sw = Stopwatch.StartNew();
            while (!cancellationToken.IsCancellationRequested)
            {
                Thread.Sleep(options.Value.ExtractInterval / 2);
                yield return 0;
            }
            yield return -1;
        }
    }
}