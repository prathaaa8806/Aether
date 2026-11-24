namespace JarvisCoreEngine.Models
{
    public class SchedulerSettings
    {
        public bool EnableScheduler { get; set; } = true;
        public List<ScheduledJobConfig> Jobs { get; set; } = new();
    }

    public class ScheduledJobConfig
    {
        public string Name { get; set; } = "";
        public int IntervalSeconds { get; set; } = 60;
        public bool Enabled { get; set; } = true;
    }
}