using JarvisCoreEngine.Core;
using JarvisCoreEngine.Interfaces;

namespace JarvisCoreEngine.Commands
{
    public class HelpCommand : ICommand
    {
        public string Name => "help";
        public string Description => "Lists all available commands.";

        private readonly CommandRegistry _registry;
        private readonly IAppLogger _logger;

        public HelpCommand(CommandRegistry registry, IAppLogger logger)
        {
            _registry = registry;
            _logger = logger;
        }

        public Task ExecuteAsync(string[] args)
        {
            _logger.Log("Available Commands:");
            foreach (var cmd in _registry.Commands)
            {
                Console.WriteLine($"  {cmd.Name,-10} → {cmd.Description}");
            }
            return Task.CompletedTask;
        }
    }
}