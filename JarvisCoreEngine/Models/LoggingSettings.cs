namespace JarvisCoreEngine.Models
{
    public class LoggingSettings
    {
        public bool EnableFileLogging { get; set; } = true;
        public string LogFolder { get; set; } = "Logs";
        public string LogFileNameFormat { get; set; } = "log-{0:yyyy-MM-dd}.txt";
    }
}