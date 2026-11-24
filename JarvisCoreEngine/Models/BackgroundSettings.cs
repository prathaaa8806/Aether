namespace JarvisCoreEngine.Models
{
    public class BackgroundSettings
    {
        public bool EnableBackgroundService { get; set; } = true;
        public int HeartbeatIntervalSeconds { get; set; } = 15;
    }
}