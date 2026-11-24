namespace JarvisCoreEngine.Models
{
    public class FileWatcherSettings
    {
        public bool EnableFileWatcher { get; set; } = true;
        public bool WatchSubdirectories { get; set; } = false;
        public string FileFilter { get; set; } = "*.*";
    }
}