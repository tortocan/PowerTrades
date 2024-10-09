using Microsoft.Extensions.Logging;
using Polly;
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

        public ForecastPowerReport Generate(DateTime date, TimeZoneInfo timeZoneInfo, string workingDirectory = "./", FileConventionBuilder? fileConventionBuilder = null)
        {
            IDisposable? tradeScope = null;
            using var reportScope = logger.BeginScope($"Report Id: {Guid.NewGuid()}");
            logger.LogInformation("Report started.");
            var isSuccessFull = false;
            try
            {
                Policy.Handle<Exception>()
               .Retry(3, (e, i) =>
               {
                   logger.LogError(e,$"Retry ({i}) task, An excepion has occured: {e.Message}");
               })
               .Execute(() =>
                {
                    var trades = this.powerService.GetTrades(date, timeZoneInfo);
                    if (trades?.Any() == true)
                    {
                        var results = new Dictionary<int, PowerTradeCsvModel>();
                        logger.LogInformation($"Report has total trades {trades.Count()}");
                        foreach (var trade in trades)
                        {
                            tradeScope = logger.BeginScope($"Trade Id: {trade.TradeId}");
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
                            tradeScope?.Dispose();
                        }
                        Records = new ReadOnlyCollection<PowerTradeCsvModel>(results.Values?.OrderBy(x => x.VolumeDate).ToList());

                        logger.LogInformation("Start buiding the CSV file.");

                        fileConventionBuilder ??= new FileConventionBuilder(new DateOnly(date.Year, date.Month, date.Day));
                        var builder = powerTradeCsvBuilder.WithWorkingDirectory(workingDirectory).WithFilename(fileConventionBuilder);
                        builder.Build(Records);
                        logger.LogInformation("Finish buiding the CSV file.");
                        isSuccessFull = true;
                    }
                    else
                    {
                        logger.LogWarning("Report has no trades.");
                        throw new ArgumentNullException(nameof(trades));
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, ex.Message);
                tradeScope?.Dispose();
            }
            finally
            {
                if (isSuccessFull)
                {
                    logger.LogInformation($"Report finish with results {Records.Count}.");
                }
                else
                {
                    logger.LogCritical($"Report finish with results {Records.Count}.");
                }
            }
            return this;
        }
    }
}