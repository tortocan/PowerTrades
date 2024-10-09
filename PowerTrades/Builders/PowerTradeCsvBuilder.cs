using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Linq.Expressions;

namespace PowerTrades.Builders
{
    public class PowerTradeCsvBuilder
    {
        private string fileName;
        private string workingDirectory;
        private readonly ILogger<PowerTradeCsvBuilder> logger;

        public PowerTradeCsvBuilder(ILogger<PowerTradeCsvBuilder> logger)
        {
            this.logger = logger;
        }

        private class PowerTradeCsvClassMap : ClassMap<PowerTradeCsvModel>
        {
            public PowerTradeCsvClassMap()
            {
                Map(x => x.VolumeDate).Ignore();
                var dateTimeMap = Map(x => x.VolumeDateUtc).Name("Datetime").NameIndex(0);
                dateTimeMap.TypeConverterOption.Format("o");
                //dateTimeMap.TypeConverterOption.Format("yyyy-MM-ddTHH:mm:ssZ");
                dateTimeMap.TypeConverterOption.CultureInfo(CultureInfo.InvariantCulture);
                var volumeMap = Map(x => x.Volume).Name("Volume").NameIndex(1);
            }

            Expression<Func<T>> ToExpression<T>(Func<T> call)
            {
                MethodCallExpression methodCall = call.Target == null
                    ? Expression.Call(call.Method)
                    : Expression.Call(Expression.Constant(call.Target), call.Method);

                return Expression.Lambda<Func<T>>(methodCall);
            }
        }

        public PowerTradeCsvBuilder WithWorkingDirectory(string workingDirectory) {
            this.workingDirectory = workingDirectory;
            return this;
        }

        public CsvWriter Build(IEnumerable<PowerTradeCsvModel> records)
        {
            logger.LogInformation($"Start building CSV file");
            try
            {
                var culture = CultureInfo.InvariantCulture;

                var config = new CsvConfiguration(culture)
                {
                    NewLine = Environment.NewLine,
                    Delimiter = ","
                };
                logger.LogInformation($"WorkingDirectory is ({workingDirectory})");
                var path = Path.Combine(workingDirectory, fileName);
                using var writer = new StreamWriter(fileName);
                using var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<PowerTradeCsvClassMap>();
                csv.WriteRecords(records);
                logger.LogInformation($"CSV file was generated at {path}");
                return csv;

            }
            catch (Exception ex)
            {
                logger.LogError(ex,ex.Message);

                throw;
            }
            finally
            {
                logger.LogInformation($"Finish building CSV file");

            }
            
        }

        public PowerTradeCsvBuilder WithFilename(FileConventionBuilder fileConvention)
        {
            fileName = fileConvention.Build();
            return this;
        }
    }
}