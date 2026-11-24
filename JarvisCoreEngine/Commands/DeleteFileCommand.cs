using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Services;

namespace JarvisCoreEngine.Commands
{
    public class DeleteFileCommand : ICommand
    {
        public string Name => "delete";
        public string Description => "Deletes a file. Usage: delete <filename>";

        private readonly FileService _fileService;
        private readonly IAppLogger _logger;

        public DeleteFileCommand(FileService fileService, IAppLogger logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        public Task ExecuteAsync(string[] args)
        {
            if (args.Length < 1)
            {
                _logger.Warn("Usage: delete <filename>");
                return Task.CompletedTask;
            }

            string fileName = args[0];
            _fileService.DeleteFile(fileName);
            return Task.CompletedTask;
        }
    }
}