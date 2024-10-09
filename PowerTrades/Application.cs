using DotMake.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        public Application(ILogger<Application> logger, IOptions<PowerTradesOptions> options)
        {
            this.logger = logger;
            this.options = options;
        }

        // This is where the arguments are defined
        public async Task<int> ExecuteAsync(string[] args)
        {

            try
            {

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

                var settings = new CliSettings()
                {
                    EnableEnvironmentVariablesDirective = true
                };
                if (!string.IsNullOrWhiteSpace(options.Value.WorkingDirectory))
                {

                    var cmd = Cli.Parse<RootCliCommand>(args, settings);
                    if (cmd.Tokens?.Any(x => x.Value == RootCliCommand.WorkingDirOptionName) == false)
                    {
                        args = [RootCliCommand.WorkingDirOptionName, options.Value.WorkingDirectory];
                    }
                }
                logger.LogInformation($"options.Value.WorkingDirectory: {options.Value.WorkingDirectory}");
                var result = await Cli.RunAsync<RootCliCommand>(args, settings);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }


            return -1;


        }


    }
}