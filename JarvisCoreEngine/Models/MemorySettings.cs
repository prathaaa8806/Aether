namespace JarvisCoreEngine.Models
{
    public class MemorySettings
    {
        public bool EnablePersistentMemory { get; set; } = true;
        public string MemoryFolder { get; set; } = "Memory";
        public string MemoryFileName { get; set; } = "jarvis_memory.json";
    }
}