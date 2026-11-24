using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Models;

namespace JarvisCoreEngine.Services
{
    public class SchedulerService
    {
        private readonly IAppLogger _logger;
        private readonly SchedulerSettings _settings;
        private readonly IEnumerable<IScheduledJob> _jobs;
        private CancellationTokenSource? _cts;

        public SchedulerService(
            IAppLogger logger,
            SchedulerSettings settings,
            IEnumerable<IScheduledJob> jobs)
        {
            _logger = logger;
            _settings = settings;
            _jobs = jobs;
        }

        public void Start()
        {
            if (!_settings.EnableScheduler)
            {
                _logger.Warn("Scheduler disabled in config.");
                return;
            }

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _logger.Log("⏰ Scheduler started.");

            foreach (var jobConfig in _settings.Jobs.Where(j => j.Enabled))
            {
                var job = _jobs.FirstOrDefault(j =>
                    j.Name.Equals(jobConfig.Name, StringComparison.OrdinalIgnoreCase));

                if (job == null)
                {
                    _logger.Warn($"Scheduled job not found: {jobConfig.Name}");
                    continue;
                }

                int interval = jobConfig.IntervalSeconds;

                Task.Run(async () =>
                {
                    _logger.Log($"📅 Job scheduled: {job.Name} (every {interval}s)");

                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            await job.ExecuteAsync();
                            await Task.Delay(TimeSpan.FromSeconds(interval), token);
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Job '{job.Name}' failed: {ex.Message}");
                        }
                    }

                    _logger.Warn($"Job stopped: {job.Name}");
                }, token);
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
        }
    }
}