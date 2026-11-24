namespace JarvisCoreEngine.Interfaces
{
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }
        Task ExecuteAsync(string[] args);
    }
}