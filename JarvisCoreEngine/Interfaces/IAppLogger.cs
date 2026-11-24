namespace JarvisCoreEngine.Interfaces
{
    public interface IAppLogger
    {
        void Log(string message);
        void Warn(string message);
        void Error(string message);
    }
}