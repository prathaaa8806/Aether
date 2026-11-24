using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Services;

namespace JarvisCoreEngine.Commands
{
    public class ResetCommand : ICommand
    {
        public string Name => "reset";
        public string Description => "Resets JARVIS's conversation memory.";

        private readonly AiService _ai;

        public ResetCommand(AiService ai)
        {
            _ai = ai;
        }

        public Task ExecuteAsync(string[] args)
        {
            _ai.ResetConversation();
            return Task.CompletedTask;
        }
    }
}