namespace JarvisCoreEngine.Interfaces
{
    public interface IScheduledJob
    {
        string Name { get; }
        Task ExecuteAsync();
    }
}