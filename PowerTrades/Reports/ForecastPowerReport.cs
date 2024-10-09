using Microsoft.Extensions.Logging;
using PowerService;
using PowerTrades.Builders;
using System.Collections.ObjectModel;

namespace PowerTrades.Reports
{
    public class ForecastPowerReport
    {
        private readonly ILogger<ForecastPowerReport> logger;
        private readonly IPowerService powerService;
        private readonly PowerTradeCsvBuilder powerTradeCsvBuilder;

        public ForecastPowerReport(ILogger<ForecastPowerReport> logger, IPowerService powerService, PowerTradeCsvBuilder powerTradeCsvBuilder)
        {
            this.logger = logger;
            this.powerService = powerService;
            this.powerTradeCsvBuilder = powerTradeCsvBuilder;
        }

        public IReadOnlyCollection<PowerTradeCsvModel> Records { get; set; } = [];

        public ForecastPowerReport Generate(DateTime date, TimeZoneInfo timeZoneInfo,string workingDirectory = "./", FileConventionBuilder? fileConventionBuilder = null)
        {
            //date = new DateTime(date.Year, date.Month, date.Day,date.Hour,date.Minute,date.Second,date.Kind);
            logger.LogInformation("Report started.");
            IDisposable? loggerScope = null;
            try
            {
                var trades = this.powerService.GetTrades(date, timeZoneInfo);
                if (trades?.Any() == true)
                {
                    var results = new Dictionary<int, PowerTradeCsvModel>();
                    logger.LogInformation($"Report has total trades {trades.Count()}");
                    foreach (var trade in trades)
                    {
                        loggerScope = logger.BeginScope(trade.TradeId);
                        var periodGroups = trade.Periods.GroupBy(x => x.Period);
                        logger.LogDebug($"Working on trade periods found {periodGroups?.Count()}");
                        foreach (var periodGroup in periodGroups)
                        {
                            var volume = periodGroup.Sum(x => x.Volume);

                            if (!results.ContainsKey(periodGroup.Key))
                            {
                                var periodDate = TimeZoneInfo.ConvertTime(new DateTime(trade.Date.Ticks, trade.Date.Kind), timeZoneInfo);
                                periodDate = periodDate.AddHours(periodGroup.Key - 1);

                                results.Add(periodGroup.Key, new PowerTradeCsvModel() { VolumeDate = periodDate });
                            }
                            var result = results[periodGroup.Key];
                            logger.LogDebug($"Sum volume {volume} to current {result.Volume}");
                            result.Volume += volume;
                        }
                        loggerScope?.Dispose();
                    }
                    Records = new ReadOnlyCollection<PowerTradeCsvModel>(results.Values?.OrderBy(x => x.VolumeDate).ToList());

                    logger.LogInformation("Start buiding the CSV file.");

                    fileConventionBuilder ??= new FileConventionBuilder(new DateOnly(date.Year, date.Month, date.Day));
                    var builder = powerTradeCsvBuilder.WithWorkingDirectory(workingDirectory).WithFilename(fileConventionBuilder);
                    builder.Build(Records);
                    logger.LogInformation("Finish buiding the CSV file.");
                }
                else
                {
                    logger.LogWarning("Report has no trades.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                loggerScope?.Dispose();
            }
            finally
            {
                logger.LogInformation($"Report finish with results {Records.Count}.");
            }
            return this;
        }
    }
}