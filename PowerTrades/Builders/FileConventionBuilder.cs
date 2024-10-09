namespace PowerTrades.Builders
{
    public class FileConventionBuilder
    {
        private DateOnly volumeDate;

        public FileConventionBuilder(DateOnly volumeDate)
        {
            ArgumentOutOfRangeException.ThrowIfEqual(volumeDate, DateOnly.MinValue);
            ArgumentNullException.ThrowIfNull(volumeDate, nameof(volumeDate));

            VolumeDate = volumeDate;
        }

        public DateTime ExtractionDateUtc { get; internal set; }
        public DateOnly VolumeDate { get => volumeDate; private set => volumeDate = value; }

        public string Build()
        {
            if (ExtractionDateUtc == DateTime.MinValue)
            {
                ExtractionDateUtc = DateTime.UtcNow;
            }
            return $"PowerPosition_{volumeDate:yyyyMMdd}_{ExtractionDateUtc:yyyyMMddHHmm}.csv";
        }
    }
}
