using JarvisCoreEngine.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace JarvisCoreEngine.Core
{
    public class CommandRegistry
    {
        private readonly IServiceProvider _provider;
        private List<ICommand>? _commands;

        public CommandRegistry(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IEnumerable<ICommand> Commands
        {
            get
            {
                _commands ??= _provider.GetServices(typeof(ICommand))
                                       .Cast<ICommand>()
                                       .ToList();
                return _commands;
            }
        }

        public ICommand? Find(string name)
        {
            return Commands.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}