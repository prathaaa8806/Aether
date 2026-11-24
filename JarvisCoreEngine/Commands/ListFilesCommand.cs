using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Services;

namespace JarvisCoreEngine.Commands
{
    public class ListFilesCommand : ICommand
    {
        public string Name => "list";
        public string Description => "Lists all files in the Workspace folder.";

        private readonly FileService _fileService;
        private readonly IAppLogger _logger;

        public ListFilesCommand(FileService fileService, IAppLogger logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        public Task ExecuteAsync(string[] args)
        {
            var files = _fileService.ListFiles().ToList();

            if (files.Count == 0)
            {
                _logger.Warn("Workspace is empty.");
                return Task.CompletedTask;
            }

            _logger.Log($"Files in Workspace ({files.Count}):");
            foreach (var file in files)
            {
                Console.WriteLine($"  • {file}");
            }

            return Task.CompletedTask;
        }
    }
}