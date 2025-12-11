namespace TextEvaluator.Core.Interfaces
{
    public interface IGradingEngine : IHasHash
    {
        public IReadOnlyList<IGradingCriterion> Criterion { get; }
        public IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> ApplyAsync(string text, ILogging? logging = null);
    }
}