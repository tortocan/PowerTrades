using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace PowerTrades.Builders
{
    public class PowerTradeCsvBuilder
    {
        private List<PowerTradeCsvModel> records;
        private string fileName;

        public PowerTradeCsvBuilder(List<PowerTradeCsvModel> records)
        {
            this.records = records;
        }

        private class PowerTradeCsvClassMap : ClassMap<PowerTradeCsvModel>
        {
            public PowerTradeCsvClassMap()
            {
                var dateTimeMap = Map(x => x.VolumeDate).Name("Datetime").NameIndex(0);
                dateTimeMap.TypeConverterOption.Format("yyyy-MM-ddTHH:mm:ssZ");
                dateTimeMap.TypeConverterOption.CultureInfo(CultureInfo.InvariantCulture);
                var volumeMap = Map(x => x.Volume).Name("Volume").NameIndex(0);
            }
        }

        public CsvWriter Build()
        {
            var culture = CultureInfo.InvariantCulture;
        
            var config = new CsvConfiguration(culture)
            {
                NewLine = Environment.NewLine,
                Delimiter = ","
            };

            using var writer = new StreamWriter(fileName);
            using var csv = new CsvWriter(writer, config);
            csv.Context.RegisterClassMap<PowerTradeCsvClassMap>();
            csv.WriteRecords(records);
            return csv;
        }

        public PowerTradeCsvBuilder WithFilename(FileConventionBuilder fileConvention)
        {
            fileName = fileConvention.Build();
            return this;
        }
    }
}