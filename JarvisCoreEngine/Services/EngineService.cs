using JarvisCoreEngine.Core;
using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Models;

namespace JarvisCoreEngine.Services
{
    public class EngineService
    {
        private readonly CommandRegistry _registry;
        private readonly IAppLogger _logger;
        private readonly AppSettings _settings;

        public EngineService(CommandRegistry registry, IAppLogger logger, AppSettings settings)
        {
            _registry = registry;
            _logger = logger;
            _settings = settings;
        }

        public async Task StartAsync()
        {
            _logger.Log($"{_settings.AppName} v{_settings.Version}");
            _logger.Log($"Author: {_settings.Author}");
            _logger.Log(_settings.WelcomeMessage);

            while (true)
            {
                Console.Write("\nCommand > ");
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                var parts = input.Trim().Split(' ');
                var commandName = parts[0];
                var args = parts.Skip(1).ToArray();

                var command = _registry.Find(commandName);

                if (command == null)
                {
                    _logger.Error($"Unknown command: '{commandName}'. Type 'help'.");
                    continue;
                }

                try
                {
                    await command.ExecuteAsync(args);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Command failed: {ex.Message}");
                }
            }
        }
    }
}