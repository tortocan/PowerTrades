namespace PowerService
{
    public interface IPowerService
    {
        IEnumerable<PowerTrade> GetTrades(DateTime date, TimeZoneInfo timeZoneInfo);

        Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime date, TimeZoneInfo timeZoneInfo);
    }
}
