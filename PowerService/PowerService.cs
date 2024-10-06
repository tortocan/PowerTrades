namespace PowerService
{
    public enum PowerServiceMode
    {
        Normal,
        Test,
        Error
    }

    public class PowerService : IPowerService
    {
        public const string SERVICE_MODE_ENV_VAR_NAME = "SERVICE_MODE";

        private readonly Random _random = new Random();

        private readonly PowerServiceMode _mode;

        public PowerService()
        {
            _mode = PowerServiceMode.Normal;
            var envMode = Environment.GetEnvironmentVariable(SERVICE_MODE_ENV_VAR_NAME);
            if (!string.IsNullOrWhiteSpace(envMode))
            {
                _mode = Enum.Parse<PowerServiceMode>(envMode);
            }
        }

        /// <summary>
        /// It reports the forecast of the total energy volume per hour required by Axpo for the next day.
        /// </summary>
        /// <param name="date">The argument date refers to the reference date of the trades thus, you will need to request the date of the following day if you want to get the power positions of the day-ahead</param>
        /// <returns>An array of  <see cref="PowerTrade"/>`s. with the UTC time <see cref="PowerTrade.Create(DateTime, int)"/></returns>
        public IEnumerable<PowerTrade> GetTrades(DateTime date, TimeZoneInfo timeZoneInfo)
        {
            if (_mode == PowerServiceMode.Normal | _mode == PowerServiceMode.Error)
            {
                CheckThrowError();
            }
            Thread.Sleep(GetDelay());
            return GetTradesImpl(date, timeZoneInfo);
        }

        /// <summary>
        /// It reports the forecast of the total energy volume per hour required by Axpo for the next day.
        /// </summary>
        /// <param name="date">The argument date refers to the reference date of the trades thus, you will need to request the date of the following day if you want to get the power positions of the day-ahead</param>
        /// <returns>An array of  <see cref="PowerTrade"/>`s. with the UTC time <see cref="PowerTrade.Create(DateTime, int)"/></returns>
        public async Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime date, TimeZoneInfo timeZoneInfo)
        {
            CheckThrowError();
            await Task.Delay(GetDelay());
            return GetTradesImpl(date, timeZoneInfo);
        }

        private void CheckThrowError()
        {
            if (_mode == PowerServiceMode.Error || _random.Next(10) == 9)
            {
                throw new PowerServiceException("Error retrieving power volumes");
            }
        }

        private TimeSpan GetDelay()
        {
            double seconds = _random.NextDouble() * 5.0;
            return TimeSpan.FromSeconds(seconds);
        }

        private IEnumerable<PowerTrade> GetTradesImpl(DateTime date, TimeZoneInfo timeZoneInfo)
        {
            DateTime utcStartTime = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc).Date.AddHours(-1.0); //DateTimeKind.Unspecified defaults to UTC
            DateTime utcEndTime = utcStartTime.AddDays(1.0);
            int numberOfPeriods = (int)utcEndTime.Subtract(utcStartTime).TotalHours;
            int numberOfTrades = ((_mode == PowerServiceMode.Test) ? 2 : _random.Next(1, 20));
            var dateToUtc = date.Kind == DateTimeKind.Utc ? date : TimeZoneInfo.ConvertTimeFromUtc(date, timeZoneInfo).ToUniversalTime();
            PowerTrade[] trades = (from _ in Enumerable.Range(0, numberOfTrades)
                                   select PowerTrade.Create(dateToUtc, numberOfPeriods)).ToArray();
            int period = 0;
            DateTime time = utcStartTime;
            while (time < utcEndTime)
            {
                PowerTrade[] array = trades;
                foreach (PowerTrade trade in array)
                {
                    double volume = ((_mode == PowerServiceMode.Test) ? ((double)(period + 1)) : (_random.NextDouble() * 1000.0));
                    trade.Periods[period].SetVolume(volume);
                }
                period++;
                time = time.AddHours(1.0);
            }
            return trades;
        }
    }
}