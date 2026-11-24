using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Services;

namespace JarvisCoreEngine.Commands
{
    public class AskCommand : ICommand
    {
        public string Name => "ask";
        public string Description => "Ask JARVIS anything. Usage: ask <your question>";

        private readonly AiService _ai;
        private readonly IAppLogger _logger;

        public AskCommand(AiService ai, IAppLogger logger)
        {
            _ai = ai;
            _logger = logger;
        }

        public async Task ExecuteAsync(string[] args)
        {
            if (args.Length < 1)
            {
                _logger.Warn("Usage: ask <your question>");
                return;
            }

            string question = string.Join(' ', args);
            string? answer = await _ai.AskAsync(question);

            if (string.IsNullOrWhiteSpace(answer))
            {
                _logger.Warn("No answer received.");
                return;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n🤖 JARVIS:");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(answer);
            Console.ResetColor();
        }
    }
}