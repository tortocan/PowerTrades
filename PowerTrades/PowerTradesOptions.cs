namespace PowerTrades
{
    public class PowerTradesOptions
    {
        public const string Name = "PowerTrades";
        public string? WorkingDirectory { get; set; }
        public TimeSpan ExtractInterval { get; set; }
    }
}