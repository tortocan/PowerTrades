namespace PowerService
{
    public class PowerTrade
    {
        public string TradeId { get; }

        public DateTime Date { get; }

        public PowerPeriod[] Periods { get; }

        private PowerTrade(DateTime date, PowerPeriod[] periods)
        {
            TradeId = Guid.NewGuid().ToString();
            Date = date.ToUniversalTime();
            Periods = periods;
        }

        public static PowerTrade Create(DateTime date, int numberOfPeriods)
        {
            PowerPeriod[] periods = (from period in Enumerable.Range(1, numberOfPeriods)
                                     select new PowerPeriod(period)).ToArray();
            return new PowerTrade(date, periods);
        }
    }
}