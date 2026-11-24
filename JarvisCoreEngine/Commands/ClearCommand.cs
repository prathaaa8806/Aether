using JarvisCoreEngine.Interfaces;

namespace JarvisCoreEngine.Commands
{
    public class ClearCommand : ICommand
    {
        public string Name => "clear";
        public string Description => "Clears the console screen.";

        public Task ExecuteAsync(string[] args)
        {
            Console.Clear();
            return Task.CompletedTask;
        }
    }
}