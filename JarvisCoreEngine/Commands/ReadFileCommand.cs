using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Services;

namespace JarvisCoreEngine.Commands
{
    public class ReadFileCommand : ICommand
    {
        public string Name => "read";
        public string Description => "Reads content of a file. Usage: read <filename>";

        private readonly FileService _fileService;
        private readonly IAppLogger _logger;

        public ReadFileCommand(FileService fileService, IAppLogger logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        public Task ExecuteAsync(string[] args)
        {
            if (args.Length < 1)
            {
                _logger.Warn("Usage: read <filename>");
                return Task.CompletedTask;
            }

            string fileName = args[0];
            string? content = _fileService.ReadFile(fileName);

            if (content != null)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("------ FILE CONTENT ------");
                Console.WriteLine(content);
                Console.WriteLine("--------------------------");
                Console.ResetColor();
            }

            return Task.CompletedTask;
        }
    }
}