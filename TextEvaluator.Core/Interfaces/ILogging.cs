namespace TextEvaluator.Core.Interfaces
{
    public interface ILogging : IDisposable
    {
        public const string ERROR_PARAM = nameof(ERROR_PARAM);
        public const string INDEX_PARAM = nameof(INDEX_PARAM);
        public const string DESCRIPTION_PARAM = nameof(DESCRIPTION_PARAM);
        public const string CRIT_PARAM = nameof(CRIT_PARAM);
        public const string CRIT_RESULT_OBJ_PARAM = nameof(CRIT_RESULT_OBJ_PARAM);
        public const string CRIT_RESULT_SCORE_PARAM = nameof(CRIT_RESULT_SCORE_PARAM);

        void Log(LogTypes type, string message, params object?[] args);

        public ILogging? CreateChildLogging(Type typeChild, object? child = null);
    }

    public enum LogTypes
    {
        Info, Warning, Error, Fatal
    }
}
