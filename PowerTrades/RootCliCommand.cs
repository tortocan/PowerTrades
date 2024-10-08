using DotMake.CommandLine;
using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace PowerTrades
{
    // Create a simple class like this to define your root command:
    [CliCommand(Description = "A root cli command")]
    internal class RootCliCommand
    {
        private readonly ILogger<RootCliCommand> logger;

        public RootCliCommand(ILogger<RootCliCommand> logger)
        {
            this.logger = logger;
        }
        internal const string DirOptionName = "-d";
        [CliOption(Description = "The folder path for storing the CSV file", Required = true, ValidationRules = CliValidationRules.ExistingDirectory, Name = DirOptionName, Aliases = new string[] { "--directory" })]
        public DirectoryInfo Directory { get; set; }

        internal const string IntervalOptionName = "-i";
        [CliOption(Description = "The extract interval to generate the CSV file", Name = IntervalOptionName, Aliases = new string[] { "--interval" })]
        public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);

        public void Run(CliContext context)
        {
            logger.LogInformation($@"Handler for '{GetType().FullName}' is run:");
            Console.WriteLine($@"Value for {nameof(Directory)} property is '{Directory}'");
            Console.WriteLine($@"Value for {nameof(Interval)} property is '{Interval}'");
            Console.WriteLine();
        }
    }
}