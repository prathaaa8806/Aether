using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Services;

namespace JarvisCoreEngine.Commands
{
    public class FetchCommand : ICommand
    {
        public string Name => "fetch";
        public string Description => "Fetches a URL. Usage: fetch <url>";

        private readonly HttpService _http;
        private readonly IAppLogger _logger;

        public FetchCommand(HttpService http, IAppLogger logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task ExecuteAsync(string[] args)
        {
            if (args.Length < 1)
            {
                _logger.Warn("Usage: fetch <url>");
                return;
            }

            string url = args[0];
            string? result = await _http.GetAsync(url);

            if (result == null)
            {
                _logger.Warn("No response received.");
                return;
            }

            // Print first 800 chars for readability
            string preview = result.Length > 800 ? result.Substring(0, 800) + "..." : result;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("------ RESPONSE ------");
            Console.WriteLine(preview);
            Console.WriteLine("----------------------");
            Console.ResetColor();
        }
    }
}