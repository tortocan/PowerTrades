using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;

namespace PowerTrades.Tests
{
    public static class TestHelper
    {

        public class PowerTradesConfig
        {
            [JsonPropertyName(PowerTradesOptions.Name)]
            public PowerTradesOptions Value { get; set; }
        }

        public static T GetRequiredService<T>()
        {
            IHost testHost = GetTestHost();
            return testHost.Services.GetRequiredService<T>();
        }
        public static IHostBuilder GetTestBuilder()
        {
            var env = "Testing";
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", env);
            return Program.CreateDefaultBuilder().UseEnvironment(env);
        }
        public static IHost GetTestHost()
        {
            return GetTestBuilder().Build();
        }

        public static string CapturedStdOut(Action callback)
        {
            TextWriter originalStdOut = Console.Out;

            using var newStdOut = new StringWriter();
            Console.SetOut(newStdOut);

            callback.Invoke();
            var capturedOutput = newStdOut.ToString();

            Console.SetOut(originalStdOut);

            return capturedOutput;
        }
        public static string CapturedStd(Action callback)
        {
            var stdOut = CapturedStdOut(callback);
            var stdErr = CapturedStdError(callback);

            return string.IsNullOrWhiteSpace(stdOut) ? stdErr : stdOut; 
        }

        public static string CapturedStdError(Action callback)
        {
            TextWriter originalStdError = Console.Error;

            using var newStdError = new StringWriter();
            Console.SetError(newStdError);

            callback.Invoke();
            var capturedOutput = newStdError.ToString();

            Console.SetError(originalStdError);

            return capturedOutput;
        }
    }
}