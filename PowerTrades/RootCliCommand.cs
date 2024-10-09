using DotMake.CommandLine;
using Microsoft.Extensions.Logging;
using PowerTrades.Builders;
using PowerTrades.Reports;
using System.CommandLine;
using System.Globalization;

namespace PowerTrades
{
    // Create a simple class like this to define your root command:
    [CliCommand(Description = "A root cli command")]
    internal class RootCliCommand
    {
        private readonly ILogger<RootCliCommand> logger;
        private readonly ForecastPowerReport forecastPowerReport;
        private DateTime extractDateUtc;

        public RootCliCommand(ILogger<RootCliCommand> logger, ForecastPowerReport forecastPowerReport)
        {
            this.logger = logger;
            this.forecastPowerReport = forecastPowerReport;

            if (string.IsNullOrWhiteSpace(TimeZoneId))
            {
                TimeZoneInfo.TryConvertWindowsIdToIanaId(TimeZoneInfo.Local.Id, RegionInfo.CurrentRegion.TwoLetterISORegionName, out string? ianaId);
                TimeZoneId = ianaId;
            }
        }

        internal const string WorkingDirOptionName = "-d";

        [CliOption(Description = "The folder path for storing the CSV file", Required = true, ValidationRules = CliValidationRules.ExistingDirectory, Name = WorkingDirOptionName, Aliases = new string[] { "--directory" })]
        public DirectoryInfo WorkingDirectory { get; set; }

        internal const string IntervalOptionName = "-i";

        [CliOption(Description = "The extract interval to generate the CSV file", Name = IntervalOptionName, Aliases = new string[] { "--interval" })]
        public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);

        internal const string ExtractDateUtcOptionName = "-e";

        [CliOption(Description = "The extract date time as UTC format to generate the CSV file name", Name = ExtractDateUtcOptionName, Aliases = new string[] { "--extract-date-utc" })]
        public DateTime ExtractDateUtc { get => extractDateUtc; set => extractDateUtc = new DateTime(value.ToUniversalTime().Ticks, DateTimeKind.Utc); }

        internal const string ExtractDateTimeZoneInfoOptionName = "-t";

        [CliOption(Description = "The extract date IANA time zone info format for ex: Europe/Madrid, Defaults to Local Time Zone", Name = ExtractDateTimeZoneInfoOptionName, Required = false, Aliases = new string[] { "--extract-date-tz" })]
        public string TimeZoneId { get; set; }

        public void Run(CliContext context)
        {
            logger.LogInformation($@"Handler for '{GetType().FullName}' is run:");
            try
            {
                Console.WriteLine($@"Value for {nameof(WorkingDirectory)} property is '{WorkingDirectory}'");
                Console.WriteLine($@"Value for {nameof(Interval)} property is '{Interval}'");
                FileConventionBuilder? fileConventionBuilder = null;
                var tz = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
                var utcDate = DateTime.UtcNow;
                var runDay = TimeZoneInfo.ConvertTimeFromUtc(utcDate, tz);
                var nextDay = TimeZoneInfo.ConvertTimeFromUtc(utcDate.AddDays(1), tz);

                if (tz.SupportsDaylightSavingTime && tz.IsAmbiguousTime(runDay))
                {
                    throw new ApplicationException("Ambiguous Time run day!");
                }

                if (tz.SupportsDaylightSavingTime && tz.IsAmbiguousTime(nextDay))
                {
                    throw new ApplicationException("Ambiguous Time next day!");
                }

                if (ExtractDateUtc != DateTime.MinValue)
                {
                    Console.WriteLine($@"Value for {nameof(ExtractDateUtc)} property is '{ExtractDateUtc.ToString("o")}'");
                    fileConventionBuilder = new FileConventionBuilder(new DateOnly(nextDay.Year, nextDay.Month, nextDay.Day)) { ExtractionDateUtc = ExtractDateUtc };
                }
                Console.WriteLine();

                logger.LogInformation($"Start generating report now {runDay} for next day {nextDay} with timezone {tz.DisplayName}");
                forecastPowerReport
                    .Generate(nextDay, tz, WorkingDirectory.ToString(), fileConventionBuilder);
                logger.LogInformation($"Finish generating report now {runDay} for next day {nextDay}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                logger.LogInformation($@"Handler for '{GetType().FullName}' is finished:");
            }
        }
    }
}