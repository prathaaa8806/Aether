using System.Text.Json;
using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Models;

namespace JarvisCoreEngine.Services
{
    public class MemoryService
    {
        private readonly IAppLogger _logger;
        private readonly MemorySettings _settings;
        private readonly string _memoryFilePath;

        public MemoryService(IAppLogger logger, MemorySettings settings)
        {
            _logger = logger;
            _settings = settings;

            var folder = Path.Combine(AppContext.BaseDirectory, _settings.MemoryFolder);
            Directory.CreateDirectory(folder);

            _memoryFilePath = Path.Combine(folder, _settings.MemoryFileName);
        }

        public List<ChatMessage> Load()
        {
            if (!_settings.EnablePersistentMemory)
                return new List<ChatMessage>();

            try
            {
                if (!File.Exists(_memoryFilePath))
                {
                    _logger.Log("🧠 No previous memory found. Fresh start.");
                    return new List<ChatMessage>();
                }

                var json = File.ReadAllText(_memoryFilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return new List<ChatMessage>();

                var memory = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new List<ChatMessage>();
                _logger.Log($"🧠 Memory loaded: {memory.Count} message(s).");
                return memory;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load memory: {ex.Message}");
                return new List<ChatMessage>();
            }
        }

        public void Save(List<ChatMessage> messages)
        {
            if (!_settings.EnablePersistentMemory) return;

            try
            {
                var json = JsonSerializer.Serialize(messages, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_memoryFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save memory: {ex.Message}");
            }
        }

        public void Clear()
        {
            try
            {
                if (File.Exists(_memoryFilePath))
                    File.Delete(_memoryFilePath);

                _logger.Warn("🗑 Memory wiped.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to clear memory: {ex.Message}");
            }
        }
    }
}