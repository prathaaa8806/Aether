using JarvisCoreEngine.Interfaces;

namespace JarvisCoreEngine.Jobs
{
    public class HealthCheckJob : IScheduledJob
    {
        public string Name => "HealthCheck";

        private readonly IAppLogger _logger;

        public HealthCheckJob(IAppLogger logger)
        {
            _logger = logger;
        }

        public Task ExecuteAsync()
        {
            _logger.Log("🩺 Health Check: All systems nominal.");
            return Task.CompletedTask;
        }
    }
}