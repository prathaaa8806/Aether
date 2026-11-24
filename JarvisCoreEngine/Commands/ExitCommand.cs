using JarvisCoreEngine.Interfaces;

namespace JarvisCoreEngine.Commands
{
    public class ExitCommand : ICommand
    {
        public string Name => "exit";
        public string Description => "Shuts down the engine.";

        private readonly IAppLogger _logger;

        public ExitCommand(IAppLogger logger)
        {
            _logger = logger;
        }

        public Task ExecuteAsync(string[] args)
        {
            _logger.Warn("Shutting down...");
            Environment.Exit(0);
            return Task.CompletedTask;
        }
    }
}