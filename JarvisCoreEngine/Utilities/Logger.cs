using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Models;

namespace JarvisCoreEngine.Utilities
{
    public class Logger : IAppLogger
    {
        private readonly LoggingSettings _settings;
        private readonly string _logFilePath;
        private static readonly object _lock = new();

        public Logger(LoggingSettings settings)
        {
            _settings = settings;

            // Ensure folder exists
            var folderPath = Path.Combine(AppContext.BaseDirectory, _settings.LogFolder);
            Directory.CreateDirectory(folderPath);

            // Build daily log file path
            var fileName = string.Format(_settings.LogFileNameFormat, DateTime.Now);
            _logFilePath = Path.Combine(folderPath, fileName);
        }

        public void Log(string message) => Write("INFO", message, ConsoleColor.Green);
        public void Warn(string message) => Write("WARN", message, ConsoleColor.Yellow);
        public void Error(string message) => Write("ERROR", message, ConsoleColor.Red);

        private void Write(string level, string message, ConsoleColor color)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string formatted = $"[{timestamp}] [{level}] {message}";

            // Console output
            Console.ForegroundColor = color;
            Console.WriteLine(formatted);
            Console.ResetColor();

            // File output (thread-safe)
            if (_settings.EnableFileLogging)
            {
                try
                {
                    lock (_lock)
                    {
                        File.AppendAllText(_logFilePath, formatted + Environment.NewLine);
                    }
                }
                catch
                {
                    // Silent fail — never crash app due to log issues
                }
            }
        }
    }
}