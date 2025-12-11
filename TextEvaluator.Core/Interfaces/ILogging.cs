namespace TextEvaluator.Core.Interfaces
{
    public interface ILogging : IDisposable
    {
        void Log(LogTypes type, string message, params object?[] args);

        public ILogging CreateChildLogging(string nameChild, object? child = null);
    }

    public enum LogTypes
    {
        Info, Warning, Error, Fatal
    }
}
