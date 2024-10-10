namespace PowerService.Tests
{
    [TestClass]
    public class Integration
    {
        /// <summary>
        /// Net system dose not store TZ information with dates this needs to be stored in a persistance place or UTC dates to be used.
        /// As a result of that the ToLocalTime and ToUniversalTime can cause bugs even if best practices are applied more info can be found here <see href="https://learn.microsoft.com/en-us/previous-versions/dotnet/articles/ms973825(v=msdn.10)"/> about best parctices.
        /// <para>
        /// As an alternative <see href="https://nodatime.org/"> NodaTime library </see> can be used, <see href="https://www.c-sharpcorner.com/article/nodatime-vs-system-datetime-in-net/"> this</see> article compares the pros and cons
        /// </para>
        /// </summary>
        [TestMethod]
        public void Given_TwoSimilar_Timezones_When_ToFunctionsAreCalled_CanFailIfNotHandledPropely()
        {
            var utcTime = new DateTime(2024, 10, 6, 0, 0, 0, DateTimeKind.Utc);
            TimeZoneInfo.TryConvertIanaIdToWindowsId("Europe/Bucharest", out string? bucharestTimezoneId);
            TimeZoneInfo.TryConvertIanaIdToWindowsId("Europe/Madrid", out string? madridTimezoneId);
            TimeZoneInfo.TryConvertIanaIdToWindowsId("Europe/Istanbul", out string? istabulTimezoneId);
            TimeZoneInfo.TryConvertIanaIdToWindowsId("UTC", out string? utdTimezonId);

            var utcTz = TimeZoneInfo.FindSystemTimeZoneById(utdTimezonId);
            var istabulTz = TimeZoneInfo.FindSystemTimeZoneById(istabulTimezoneId);
            var madridTz = TimeZoneInfo.FindSystemTimeZoneById(madridTimezoneId);
            var bucharestTz = TimeZoneInfo.FindSystemTimeZoneById(bucharestTimezoneId);

            var localDate = TimeZoneInfo.ConvertTimeFromUtc(utcTime, madridTz);

            var istabulDate = TimeZoneInfo.ConvertTimeFromUtc(utcTime, istabulTz);
            var bucharestDate = TimeZoneInfo.ConvertTimeFromUtc(utcTime, bucharestTz);

            Assert.AreEqual(utcTime, utcTime.ToUniversalTime());
            Assert.AreEqual(utcTime, localDate.ToUniversalTime());//This will asume the system TZ

            Assert.AreNotEqual(localDate, istabulDate);
            Assert.AreNotEqual(localDate, bucharestDate);

            Assert.AreEqual(TimeSpan.FromHours(1), (istabulDate - localDate));
            Assert.AreEqual(TimeSpan.FromHours(1), (bucharestDate - localDate));
            Assert.AreEqual(bucharestDate, istabulDate);

            Assert.IsTrue(localDate.IsDaylightSavingTime());
            Assert.IsTrue(istabulDate.IsDaylightSavingTime());
            Assert.IsTrue(bucharestDate.IsDaylightSavingTime());

            Assert.AreNotEqual(localDate, bucharestDate.ToLocalTime());//This "should" be equal
            Assert.AreNotEqual(utcTime, bucharestDate.ToUniversalTime());//This "should" be equal
            Assert.AreNotEqual(localDate, istabulDate.ToLocalTime());//This "should" be equal
            Assert.AreNotEqual(utcTime, istabulDate.ToUniversalTime());//This "should" be equal
            Assert.AreNotEqual(localDate.ToUniversalTime(), istabulDate.ToUniversalTime());//This "should" be equal
            Assert.AreNotEqual(TimeZoneInfo.ConvertTime(localDate, utcTz), TimeZoneInfo.ConvertTime(istabulDate, utcTz));//This "should be equal
            Assert.AreNotEqual(bucharestDate, TimeZoneInfo.ConvertTime(istabulDate, istabulTz));//This "should be equal

            Assert.AreEqual(localDate, TimeZoneInfo.ConvertTimeToUtc(bucharestDate, bucharestTz).ToLocalTime());//Correct
            Assert.AreEqual(utcTime, TimeZoneInfo.ConvertTimeToUtc(bucharestDate, bucharestTz));//Correct

            Assert.AreEqual(localDate, TimeZoneInfo.ConvertTimeToUtc(istabulDate, istabulTz).ToLocalTime());//Correct
            Assert.AreEqual(utcTime, TimeZoneInfo.ConvertTimeToUtc(istabulDate, istabulTz));//Correct

            Assert.AreEqual(TimeZoneInfo.ConvertTime(bucharestDate, bucharestTz), TimeZoneInfo.ConvertTime(istabulDate, istabulTz));//Correct
            Assert.AreEqual(TimeZoneInfo.ConvertTime(bucharestDate, bucharestTz).ToUniversalTime(), TimeZoneInfo.ConvertTime(istabulDate, istabulTz).ToUniversalTime());//Correct
        }

        [TestMethod]
        public void Given_LocalDate_When_GetTrades_Returns_24PeriodsPerTrade_For_NextDay()
        {
            Environment.SetEnvironmentVariable(PowerService.SERVICE_MODE_ENV_VAR_NAME, PowerServiceMode.Test.ToString());
            var sut = new PowerService();

            var today = new DateTime(2023, 07, 01, 0, 0, 0, DateTimeKind.Local);
            var nextDay = today.AddDays(1);
            var dayHours = (nextDay - today).TotalHours;
            var result = sut.GetTrades(nextDay, TimeZoneInfo.Local);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Any(), "No trades returned");
            foreach (var trade in result)
            {
                Assert.IsNotNull(trade.Periods, "Trade has no pedriods");
                Assert.IsTrue(trade.Periods.Length == dayHours, $"The report should generate an hourly volume current periods {trade.Periods.Length}");
                Assert.AreEqual(nextDay.ToUniversalTime(), trade.Date, "The report should be for the next day");
            }
        }

        [TestMethod]
        public void Given_Date_When_GetTrades_Returns_24PeriodsPerTrade_For_NextDay()
        {
            Environment.SetEnvironmentVariable(PowerService.SERVICE_MODE_ENV_VAR_NAME, PowerServiceMode.Test.ToString());
            var sut = new PowerService();

            var utcTime = new DateTime(2024, 10, 6, 0, 0, 0, DateTimeKind.Utc);
            TimeZoneInfo.TryConvertIanaIdToWindowsId("Europe/Istanbul", out string? istabulTimezoneId);
            TimeZoneInfo.TryConvertIanaIdToWindowsId("UTC", out string? utdTimezonId);

            var utcTz = TimeZoneInfo.FindSystemTimeZoneById(utdTimezonId);
            var istabulTz = TimeZoneInfo.FindSystemTimeZoneById(istabulTimezoneId);

            var istabulDate = TimeZoneInfo.ConvertTimeFromUtc(utcTime, istabulTz);

            var today = istabulDate;
            var nextDay = today.AddDays(1);
            var dayHours = (nextDay - today).TotalHours;
            var result = sut.GetTrades(nextDay, istabulTz);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Any(), "No trades returned");
            foreach (var trade in result)
            {
                Assert.IsNotNull(trade.Periods, "Trade has no pedriods");
                Assert.IsTrue(trade.Periods.Length == dayHours, $"The report should generate an hourly volume current periods {trade.Periods.Length}");
                Assert.AreEqual(TimeZoneInfo.ConvertTime(nextDay, istabulTz).ToUniversalTime(), trade.Date, "The report should be for the next day");
            }
        }

        [TestMethod]
        public async Task Given_Date_When_GetTradesAsync_Returns_Results()
        {
            Environment.SetEnvironmentVariable(PowerService.SERVICE_MODE_ENV_VAR_NAME, PowerServiceMode.Test.ToString());
            var sut = new PowerService();

            var utcTime = new DateTime(2024, 10, 6, 0, 0, 0, DateTimeKind.Utc);
            TimeZoneInfo.TryConvertIanaIdToWindowsId("Europe/Istanbul", out string? istabulTimezoneId);
            TimeZoneInfo.TryConvertIanaIdToWindowsId("UTC", out string? utdTimezonId);

            var utcTz = TimeZoneInfo.FindSystemTimeZoneById(utdTimezonId);
            var istabulTz = TimeZoneInfo.FindSystemTimeZoneById(istabulTimezoneId);

            var istabulDate = TimeZoneInfo.ConvertTimeFromUtc(utcTime, istabulTz);

            var today = istabulDate;
            var nextDay = today.AddDays(1);
            var dayHours = (nextDay - today).TotalHours;
            var result = await sut.GetTradesAsync(nextDay, istabulTz);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Given_UTCDate_When_GetTrades_Returns_24PeriodsPerTrade_For_NextDay()
        {
            Environment.SetEnvironmentVariable(PowerService.SERVICE_MODE_ENV_VAR_NAME, PowerServiceMode.Test.ToString());
            var sut = new PowerService();

            var utcTime = new DateTime(2024, 10, 6, 0, 0, 0, DateTimeKind.Utc);
            TimeZoneInfo.TryConvertIanaIdToWindowsId("UTC", out string? utdTimezonId);

            var utcTz = TimeZoneInfo.FindSystemTimeZoneById(utdTimezonId);

            var today = utcTime;
            var nextDay = today.AddDays(1);
            var dayHours = (nextDay - today).TotalHours;
            var result = sut.GetTrades(nextDay, utcTz);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Any(), "No trades returned");
            foreach (var trade in result)
            {
                Assert.IsNotNull(trade.Periods, "Trade has no pedriods");
                Assert.IsTrue(trade.Periods.Length == dayHours, $"The report should generate an hourly volume current periods {trade.Periods.Length}");
                Assert.AreEqual(nextDay, trade.Date, "The report should be for the next day");
            }
        }

        [TestMethod]
        public void Given_Date_When_GetTrades_WithTestConfig_Returns2Results()
        {
            Environment.SetEnvironmentVariable(PowerService.SERVICE_MODE_ENV_VAR_NAME, PowerServiceMode.Test.ToString());
            var sut = new PowerService();
            var result = sut.GetTrades(DateTime.UtcNow, TimeZoneInfo.Utc);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Any(), "No trades returned");
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod]
        [ExpectedException(typeof(PowerServiceException))]
        public void Given_Date_When_GetTrades_WithErrorConfig_ThrowsException()
        {
            Environment.SetEnvironmentVariable(PowerService.SERVICE_MODE_ENV_VAR_NAME, PowerServiceMode.Error.ToString());
            var sut = new PowerService();
            var result = sut.GetTrades(DateTime.UtcNow, TimeZoneInfo.Utc);

            Assert.IsNull(result);
        }
    }
}