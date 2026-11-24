using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Models;

namespace JarvisCoreEngine.Services
{
    public class FileWatcherService
    {
        private readonly IAppLogger _logger;
        private readonly FileWatcherSettings _settings;
        private readonly WorkspaceSettings _workspaceSettings;
        private FileSystemWatcher? _watcher;

        public FileWatcherService(
            IAppLogger logger,
            FileWatcherSettings settings,
            WorkspaceSettings workspaceSettings)
        {
            _logger = logger;
            _settings = settings;
            _workspaceSettings = workspaceSettings;
        }

        public void Start()
        {
            if (!_settings.EnableFileWatcher)
            {
                _logger.Warn("File watcher disabled in config.");
                return;
            }

            string path = Path.Combine(AppContext.BaseDirectory, _workspaceSettings.WorkspaceFolder);
            Directory.CreateDirectory(path);

            _watcher = new FileSystemWatcher(path)
            {
                Filter = _settings.FileFilter,
                IncludeSubdirectories = _settings.WatchSubdirectories,
                NotifyFilter = NotifyFilters.FileName
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Size
                             | NotifyFilters.CreationTime
            };

            _watcher.Created += OnCreated;
            _watcher.Changed += OnChanged;
            _watcher.Deleted += OnDeleted;
            _watcher.Renamed += OnRenamed;
            _watcher.Error += OnError;

            _watcher.EnableRaisingEvents = true;

            _logger.Log($"👁 File watcher active on: {path}");
        }

        public void Stop()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
                _logger.Warn("File watcher stopped.");
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
            => _logger.Log($"📂 Created: {e.Name}");

        private void OnChanged(object sender, FileSystemEventArgs e)
            => _logger.Log($"✏️ Modified: {e.Name}");

        private void OnDeleted(object sender, FileSystemEventArgs e)
            => _logger.Warn($"🗑 Deleted: {e.Name}");

        private void OnRenamed(object sender, RenamedEventArgs e)
            => _logger.Log($"🔁 Renamed: {e.OldName} → {e.Name}");

        private void OnError(object sender, ErrorEventArgs e)
            => _logger.Error($"Watcher error: {e.GetException().Message}");
    }
}