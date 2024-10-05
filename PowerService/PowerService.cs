using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerService
{
    public class PowerService : IPowerService
    {
        private static readonly TimeZoneInfo GmtTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

        private readonly Random _random = new Random();

        private readonly string _mode;

        public PowerService()
        {
            _mode = Environment.GetEnvironmentVariable("SERVICE_MODE") ?? "Normal";
        }
        /// <summary>
        /// It reports the forecast of the total energy volume per hour required by Axpo for the next day.
        /// </summary>
        /// <param name="date">The argument date refers to the reference date of the trades thus, you will need to request the date of the following day if you want to get the power positions of the day-ahead</param>
        /// <returns>An array of  <see cref="PowerTrade"/>`s.</returns>
        public IEnumerable<PowerTrade> GetTrades(DateTime date)
        {
            if (_mode == "Normal" | _mode == "Error")
            {
                CheckThrowError();
            }
            Thread.Sleep(GetDelay());
            return GetTradesImpl(date);
        }

        /// <summary>
        /// It reports the forecast of the total energy volume per hour required by Axpo for the next day.
        /// </summary>
        /// <param name="date">The argument date refers to the reference date of the trades thus, you will need to request the date of the following day if you want to get the power positions of the day-ahead</param>
        /// <returns>An array of  <see cref="PowerTrade"/>`s.</returns>
        public async Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime date)
        {
            CheckThrowError();
            await Task.Delay(GetDelay());
            return GetTradesImpl(date);
        }

        private void CheckThrowError()
        {
            if (_mode == "Error" || _random.Next(10) == 9)
            {
                throw new PowerServiceException("Error retrieving power volumes");
            }
        }

        private TimeSpan GetDelay()
        {
            double seconds = _random.NextDouble() * 5.0;
            return TimeSpan.FromSeconds(seconds);
        }

    
        private IEnumerable<PowerTrade> GetTradesImpl(DateTime date)
        {
            DateTime localStartTime = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified).Date.AddHours(-1.0);
            DateTime localEndTime = localStartTime.AddDays(1.0);
            DateTime utcStartTime = TimeZoneInfo.ConvertTimeToUtc(localStartTime, GmtTimeZoneInfo);
            DateTime utcEndTime = TimeZoneInfo.ConvertTimeToUtc(localEndTime, GmtTimeZoneInfo);
            int numberOfPeriods = (int)utcEndTime.Subtract(utcStartTime).TotalHours;
            int numberOfTrades = ((_mode == "Test") ? 2 : _random.Next(1, 20));
            PowerTrade[] trades = (from _ in Enumerable.Range(0, numberOfTrades)
                                   select PowerTrade.Create(date, numberOfPeriods)).ToArray();
            int period = 0;
            DateTime time = utcStartTime;
            while (time < utcEndTime)
            {
                PowerTrade[] array = trades;
                foreach (PowerTrade trade in array)
                {
                    double volume = ((_mode == "Test") ? ((double)(period + 1)) : (_random.NextDouble() * 1000.0));
                    trade.Periods[period].SetVolume(volume);
                }
                period++;
                time = time.AddHours(1.0);
            }
            return trades;
        }
    }

}
