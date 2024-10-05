namespace PowerService.Tests
{
    [TestClass]
    public class Integration
    {


        [TestMethod]
        public void Given_LocalDate_When_GetTrades_Returns_24PeriodsPerTrade_For_NextDay()
        {
            Environment.SetEnvironmentVariable(PowerService.SERVICE_MODE_ENV_VAR_NAME, PowerServiceMode.Test.ToString());
            var sut = new PowerService();

            var today = new DateTime(2023,07,02,0,0,0,DateTimeKind.Local);
            var nextDay = today.AddDays(1);
            var result = sut.GetTrades(nextDay);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Any(),"No trades returned");
            foreach (var trade in result) {

                Assert.IsNotNull(trade.Periods,"Trade has no pedriods");
                Assert.AreEqual(24, trade.Periods.Length, "The report should generate an hourly aggregated volume");
                Assert.AreEqual(nextDay.ToUniversalTime(), trade.Date, "The report should be for the next day");
            }
        }


        [TestMethod]
        public void Given_InstanbulDate_When_GetTrades_Returns_24PeriodsPerTrade_For_NextDay()
        {
            Environment.SetEnvironmentVariable(PowerService.SERVICE_MODE_ENV_VAR_NAME, PowerServiceMode.Test.ToString());
            var sut = new PowerService();
            //IANA Timezone to windows
            TimeZoneInfo.TryConvertIanaIdToWindowsId("Europe/Istanbul", out string? timeZondId);

            var localDate = new DateTime(2023, 07, 02, 0, 0, 0, DateTimeKind.Local);

            var instabulDate = TimeZoneInfo.ConvertTime(localDate, TimeZoneInfo.FindSystemTimeZoneById(timeZondId));
            var today = TimeZoneInfo.ConvertTime(instabulDate, TimeZoneInfo.Local);
            var nextDay = today.AddDays(1);
            var result = sut.GetTrades(nextDay);

            Assert.AreNotEqual(localDate,instabulDate);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Any(), "No trades returned");
            foreach (var trade in result)
            {

                Assert.IsNotNull(trade.Periods, "Trade has no pedriods");
                Assert.AreEqual(24, trade.Periods.Length, "The report should generate an hourly aggregated volume");
                Assert.AreEqual(nextDay.ToUniversalTime(), trade.Date, "The report should be for the next day");
            }
        }


        [TestMethod]
        public void Given_Date_When_GetTrades_WithTestConfig_Returns2Results()
        {
            Environment.SetEnvironmentVariable(PowerService.SERVICE_MODE_ENV_VAR_NAME, PowerServiceMode.Test.ToString());
            var sut = new PowerService();
            var result = sut.GetTrades(DateTime.UtcNow);

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
            var result = sut.GetTrades(DateTime.UtcNow);

            Assert.IsNull(result);
        }
    }
}