using JarvisCoreEngine.Interfaces;

namespace JarvisCoreEngine.Commands
{
    public class TimeCommand : ICommand
    {
        public string Name => "time";
        public string Description => "Displays the current system time.";

        private readonly IAppLogger _logger;

        public TimeCommand(IAppLogger logger)
        {
            _logger = logger;
        }

        public Task ExecuteAsync(string[] args)
        {
            _logger.Log($"Current Time: {DateTime.Now}");
            return Task.CompletedTask;
        }
    }
}