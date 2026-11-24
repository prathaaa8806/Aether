using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Services;

namespace JarvisCoreEngine.Commands
{
    public class WriteFileCommand : ICommand
    {
        public string Name => "write";
        public string Description => "Writes text to a file. Usage: write <filename> <content>";

        private readonly FileService _fileService;
        private readonly IAppLogger _logger;

        public WriteFileCommand(FileService fileService, IAppLogger logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        public Task ExecuteAsync(string[] args)
        {
            if (args.Length < 2)
            {
                _logger.Warn("Usage: write <filename> <content>");
                return Task.CompletedTask;
            }

            string fileName = args[0];
            string content = string.Join(' ', args.Skip(1));

            _fileService.WriteFile(fileName, content);
            return Task.CompletedTask;
        }
    }
}