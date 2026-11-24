using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Services;

namespace JarvisCoreEngine.Commands
{
    public class ForgetCommand : ICommand
    {
        public string Name => "forget";
        public string Description => "Completely wipes JARVIS's long-term memory.";

        private readonly AiService _ai;

        public ForgetCommand(AiService ai)
        {
            _ai = ai;
        }

        public Task ExecuteAsync(string[] args)
        {
            _ai.ForgetEverything();
            return Task.CompletedTask;
        }
    }
}