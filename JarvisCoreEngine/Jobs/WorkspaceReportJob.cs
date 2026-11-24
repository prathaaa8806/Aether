using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Services;

namespace JarvisCoreEngine.Jobs
{
    public class WorkspaceReportJob : IScheduledJob
    {
        public string Name => "WorkspaceReport";

        private readonly IAppLogger _logger;
        private readonly FileService _fileService;

        public WorkspaceReportJob(IAppLogger logger, FileService fileService)
        {
            _logger = logger;
            _fileService = fileService;
        }

        public Task ExecuteAsync()
        {
            var files = _fileService.ListFiles().ToList();
            _logger.Log($"📊 Workspace Report: {files.Count} file(s) present.");
            return Task.CompletedTask;
        }
    }
}