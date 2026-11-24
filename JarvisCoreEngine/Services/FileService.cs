using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Models;

namespace JarvisCoreEngine.Services
{
    public class FileService
    {
        private readonly WorkspaceSettings _settings;
        private readonly IAppLogger _logger;
        private readonly string _workspacePath;

        public FileService(WorkspaceSettings settings, IAppLogger logger)
        {
            _settings = settings;
            _logger = logger;

            _workspacePath = Path.Combine(AppContext.BaseDirectory, _settings.WorkspaceFolder);
            Directory.CreateDirectory(_workspacePath);
        }

        public string WorkspacePath => _workspacePath;

        // WRITE
        public bool WriteFile(string fileName, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    _logger.Error("Filename cannot be empty.");
                    return false;
                }

                string fullPath = Path.Combine(_workspacePath, fileName);
                File.WriteAllText(fullPath, content);

                _logger.Log($"File written: {fullPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to write file: {ex.Message}");
                return false;
            }
        }

        // READ
        public string? ReadFile(string fileName)
        {
            try
            {
                string fullPath = Path.Combine(_workspacePath, fileName);

                if (!File.Exists(fullPath))
                {
                    _logger.Warn($"File not found: {fileName}");
                    return null;
                }

                string content = File.ReadAllText(fullPath);
                _logger.Log($"File read: {fullPath}");
                return content;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to read file: {ex.Message}");
                return null;
            }
        }

        // LIST
        public IEnumerable<string> ListFiles()
        {
            try
            {
                var files = Directory.GetFiles(_workspacePath)
                                     .Select(Path.GetFileName)
                                     .Where(f => f != null)
                                     .Cast<string>();
                return files;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to list files: {ex.Message}");
                return Enumerable.Empty<string>();
            }
        }

        // DELETE
        public bool DeleteFile(string fileName)
        {
            try
            {
                string fullPath = Path.Combine(_workspacePath, fileName);

                if (!File.Exists(fullPath))
                {
                    _logger.Warn($"File not found: {fileName}");
                    return false;
                }

                File.Delete(fullPath);
                _logger.Log($"File deleted: {fullPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to delete file: {ex.Message}");
                return false;
            }
        }
    }
}