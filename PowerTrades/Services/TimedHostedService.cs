using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PowerTrades
{
    public class TimedHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<TimedHostedService> logger;
        private int executionCount = 0;
        private Timer timer;
        private CancellationToken cancellationToken;
        private TimeSpan interval;
        private TimeSpan dueTime = TimeSpan.Zero;
        private Action job;

        public TimedHostedService(ILogger<TimedHostedService> logger)
        {
            this.logger = logger;
        }

        public TimedHostedService WithInterval(TimeSpan interval)
        {
            this.interval = interval;
            return this;
        }

        public TimedHostedService WithJob(Action job)
        {
            this.job = job;
            return this;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Timed Hosted Service running.");

            timer = new Timer(DoWork, null, dueTime, interval);
            this.cancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            
            var count = Interlocked.Increment(ref executionCount);

            if (count == int.MaxValue) {
                logger.LogInformation("Job count has reached maximum value setting it back to 0");
                executionCount = 0;
            }

            using var loggerScope = logger.BeginScope($"Job {count}");
            logger.LogInformation($"Timed Hosted Service is working on job. Count: {count}");
            try
            {
                job();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            logger.LogInformation($"Timed Hosted Service finished working on job.");
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning($"Cancelation received...");
                StopAsync(default);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Timed Hosted Service is stopping.");

            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}