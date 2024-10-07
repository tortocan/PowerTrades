namespace PowerTrades
{
    public class FileConventionBuilder
    {
        private DateOnly volumeDate;
        private DateTime extractionDateUtc;

        public FileConventionBuilder(DateOnly volumeDate, DateTime extractionDateUtc)
        {
            ArgumentOutOfRangeException.ThrowIfEqual(volumeDate, DateOnly.MinValue);
            ArgumentNullException.ThrowIfNull(volumeDate, nameof(volumeDate));
            ArgumentOutOfRangeException.ThrowIfEqual(extractionDateUtc, DateTime.MinValue);
            ArgumentNullException.ThrowIfNull(extractionDateUtc, nameof(extractionDateUtc));

            extractionDateUtc = TimeZoneInfo.ConvertTimeFromUtc(extractionDateUtc,TimeZoneInfo.Utc);
            this.VolumeDate = volumeDate;
            this.ExtractionDateUtc = extractionDateUtc;
        }

        public DateTime ExtractionDateUtc { get => extractionDateUtc; private set => extractionDateUtc = value; }
        public DateOnly VolumeDate { get => volumeDate; private set => volumeDate = value; }

        public string? Build()
        {
            return $"PowerPosition_{volumeDate:yyyyMMdd}_{ExtractionDateUtc:yyyyMMddHHmm}.csv";
        }
    }
}
