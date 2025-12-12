namespace TextEvaluator.Core.Interfaces
{
    public interface IGradingWorker<in C> : IGradingWorkerBase
        where C : IGradingCriterion
    {
        public IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<C> gradingCriterions, string text, ILogging? logging = null);
    }

    public interface IGradingWorkerBase : IHasHash
    {

    }
}