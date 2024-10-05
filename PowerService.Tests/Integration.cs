namespace PowerService.Tests
{
    [TestClass]
    public class Integration
    {


        [TestMethod]
        public void Given_Date_When_GetTrades_Returns_24PeriodsPerTrade_For_NextDay()
        {
            Environment.SetEnvironmentVariable("SERVICE_MODE", "Test");
            var sut = new PowerService();

            var today = DateTime.Parse("2023-07-01").Date.ToUniversalTime();
            var nextDay = today.AddDays(1);
            var result = sut.GetTrades(nextDay);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Any(),"No trades returned");
            foreach (var trade in result) {

                Assert.IsNotNull(trade.Periods,"Trade has no pedriods");
                Assert.AreEqual(24, trade.Periods.Length, "The report should generate an hourly aggregated volume");
                Assert.AreEqual(nextDay, trade.Date, "The report should be for the next day");
            }
        }


        [TestMethod]
        public void Given_Date_When_GetTrades_WithTestConfig_Returns2Results()
        {
            Environment.SetEnvironmentVariable("SERVICE_MODE", "Test");
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
            Environment.SetEnvironmentVariable("SERVICE_MODE", "Error");
            var sut = new PowerService();
            var result = sut.GetTrades(DateTime.UtcNow);

            Assert.IsNull(result);
        }
    }
}