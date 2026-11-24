namespace JarvisCoreEngine.Models
{
    public class HttpSettings
    {
        public int DefaultTimeoutSeconds { get; set; } = 30;
        public string UserAgent { get; set; } = "JARVIS-Core-Engine/1.0";
    }
}