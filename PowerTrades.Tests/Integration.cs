using Bogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PowerTrades.Builders;
using System.Text.Json;

namespace PowerTrades.Tests
{
    [TestClass]
    public class Integration
    {
        [TestMethod]
        public async Task Given_App_WhenExceuteHelp_Resturns_ConsoleStdOut()
        {
            var args = new string[] { "-?" };
            var sut = TestHelper.GetRequiredService<Application>();

            var result = TestHelper.CapturedStdOut(async () => await sut.ExecuteAsync(args));
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("help"));
        }

        //The folder path for storing the CSV file can be supplied on the command line
        [TestMethod]
        public async Task Given_App_WhenExceute_WithDOption_Accepts_DirectoryArg()
        {
            var args = new string[] { "-d", AppDomain.CurrentDomain.BaseDirectory };
            var sut = TestHelper.GetRequiredService<Application>();

            var result = TestHelper.CapturedStdOut(async () =>
            {
                var result = await sut.ExecuteAsync(args);
                Assert.IsNotNull(result);
                Assert.IsTrue(result == 0);
            });
            Assert.IsTrue(result.Contains(AppDomain.CurrentDomain.BaseDirectory));
        }

        //The folder path for storing the CSV file can be read from configuration
        [TestMethod]
        public async Task Given_App_WhenExceute_WithEnvVar_Accepts_DirectoryArg()
        {
            Environment.SetEnvironmentVariable("PowerTrades__WorkingDirectory", AppDomain.CurrentDomain.BaseDirectory);
            var args = Array.Empty<string>();
            var sut = TestHelper.GetRequiredService<Application>();

            var result = TestHelper.CapturedStdOut(async () =>
            {
                var result = await sut.ExecuteAsync(args);
                Assert.IsNotNull(result);
                Assert.IsTrue(result == 0);
            });
            Assert.IsTrue(result.Contains(AppDomain.CurrentDomain.BaseDirectory));
        }

        //The folder path for storing the CSV file can be read from configuration file
        [TestMethod]
        public async Task Given_App_WhenExceute_WithConfigVar_Accepts_DirectoryArg()
        {
            var expected = JsonSerializer.Deserialize<TestHelper.PowerTradesConfig>(File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}appsettings.Testing.json")).Value.WorkingDirectory;
            var args = Array.Empty<string>();
            var sut = TestHelper.GetTestBuilder().ConfigureAppConfiguration((c, x) => { x.SetBasePath(AppDomain.CurrentDomain.BaseDirectory).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Testing.json"); }).Build().Services.GetRequiredService<Application>();
            var result = TestHelper.CapturedStd(async () =>
            {
                var result = await sut.ExecuteAsync(args);
                Assert.IsNotNull(result);
                Assert.IsTrue(result == 0);
            });
            Assert.IsTrue(result.Contains(expected), $"Expected to contain {expected} but got {result}");
        }

        //CSV filename must follow the convention PowerPosition_YYYYMMDD_YYYYMMDDHHMM.csv where
        //YYYYMMDD is year/month/day, e.g. 20141220 for 20 Dec 2014 and HHMM is 24hr time hour and minutes
        //e.g. 1837. The first date refers to the day of the volumes (the day-ahead) while the second datetime re-
        //fers to the timestamp of the extraction in UTC.
        //The folder path for storing the CSV file can be read from a configuration file
        [TestMethod]
        public async Task Given_FileConvention_WithValidParams_Returns_FileName()
        {
            var extractionDateUtc = new DateTime(2014, 12, 20, 18, 37, 0, DateTimeKind.Utc);
            var volumeDate = new DateOnly(2014, 12, 20);
            var expected = $"PowerPosition_20141220_201412201837.csv";

            var result = new FileConventionBuilder(volumeDate, extractionDateUtc);
            Assert.AreEqual(expected, result.Build());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task Given_FileConvention_WithInvalidUTCDate_Fails()
        {
            var extractionDateUtc = new DateTime(2014, 12, 20, 18, 37, 0, DateTimeKind.Local);
            var volumeDate = new DateOnly(2014, 12, 20);
            var expected = $"PowerPosition_20141220_201412201837.csv";

            var result = new FileConventionBuilder(volumeDate, extractionDateUtc);
            Assert.AreEqual(expected, result.Build());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task Given_FileConvention_WithInvalidVolumeDate_Fails()
        {
            var extractionDateUtc = new DateTime(2014, 12, 20, 18, 37, 0, DateTimeKind.Utc);
            var volumeDate = DateOnly.MinValue;
            var expected = $"PowerPosition_20141220_201412201837.csv";

            var result = new FileConventionBuilder(volumeDate, extractionDateUtc);
            Assert.AreEqual(expected, result.Build());
        }

        //It has two columns Datetime and Volume
        [TestMethod]
        public void Given_PowerTradeCsv_WhenBuild_Has2Columns()
        {
            var volumeDate = new DateOnly(2023, 10, 10);
            var extractionDate = new DateTime(2023, 10, 10, 0, 0, 0, DateTimeKind.Utc);
            var records = new Faker<PowerTradeCsvModel>()
                .StrictMode(true)
                .RuleFor(x => x.Volume, x => x.Random.Float(-20, 150))
                .RuleFor(x => x.VolumeDate, x => x.Date.FutureOffset(1).DateTime)
                .Generate(24);
            var fileConvention = new FileConventionBuilder(volumeDate, extractionDate);
            var result = new PowerTradeCsvBuilder(records)
                .WithFilename(fileConvention)
                .Build();
            Assert.AreEqual(2, result.HeaderRecord?.Length);
            Assert.AreEqual("Datetime", result.HeaderRecord?.First());
            Assert.AreEqual("Volume", result.HeaderRecord?.Last());
        }


        //Semicolon is the separator
        //The first row is the header.
        //The point is the decimal separator.
        //The Datetime format should follow ISO_8601
        [TestMethod]
        public void Given_PowerTradeCsv_WhenBuild_CheckFormat()
        {
            var volumeDate = new DateOnly(2023, 10, 10);
            var extractionDate = new DateTime(2023, 10, 10, 0, 0, 0, DateTimeKind.Utc);
            var fileConvention = new FileConventionBuilder(volumeDate, extractionDate);
            var path = fileConvention.Build();
            if (!File.Exists(path))
            {
                Given_PowerTradeCsv_WhenBuild_Has2Columns();
            }
            var result = File.ReadAllLines(path);
            var firstRow = result[1];
            var rowParts = firstRow.Split(',');
            var date = rowParts[0];
            var volume = rowParts[1];
            Assert.AreEqual("Datetime,Volume", result.First());
            Assert.IsTrue(date.EndsWith("Z"));
            Assert.IsTrue(volume.Contains('.') && !volume.Contains(','));
        }

        //The actual interval X is passed on the command line 
        [TestMethod]
        public void Given_App_WithIOption_Accepts_IntervalArg()
        {
            var expected = TimeSpan.FromMinutes(1).ToString();
            var args = new string[] { "-d", AppDomain.CurrentDomain.BaseDirectory, "-i", expected };
            var sut = TestHelper.GetRequiredService<Application>();

            var result = TestHelper.CapturedStdOut(async () =>
            {
                var result = await sut.ExecuteAsync(args);
                Assert.IsNotNull(result);
                Assert.IsTrue(result == 0);
            });
            Assert.IsTrue(result.Contains(expected), $"Expected contains {expected} but got {result}");
        }

        //The actual interval X configured in the configuration file
        [TestMethod]
        public void Given_App_WithInterval_Accepts_IntervalArg()
        {
            var expected = JsonSerializer.Deserialize<TestHelper.PowerTradesConfig>(File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}appsettings.Testing.json")).Value.ExtractInterval.ToString();
            var args = new string[] { "-d", AppDomain.CurrentDomain.BaseDirectory, "-i", expected };
            var sut = TestHelper.GetRequiredService<Application>();

            var result = TestHelper.CapturedStdOut(async () =>
            {
                var result = await sut.ExecuteAsync(args);
                Assert.IsNotNull(result);
                Assert.IsTrue(result == 0);
            });
            Assert.AreEqual("00:01:00", expected);
            Assert.IsTrue(result.Contains(expected), $"Expected contains {expected} but got {result}");
        }
    }
}