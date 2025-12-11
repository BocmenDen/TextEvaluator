namespace TextEvaluator.Core.Interfaces
{
    public interface IGradingWorker<C, R> : IHasHash
        where C : IGradingCriterion
        where R : IGradingResult
    {
        public IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<C> gradingCriterions, string text, ILogging? logging = null);
    }
}