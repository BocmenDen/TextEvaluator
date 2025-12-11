namespace TextEvaluator.Core.Interfaces
{
    public interface IGradingCriterion : IHasHash
    {
        public IReadOnlyDictionary<string, Func<string>> GetValues { get; }
        public double MaxScore { get; init; }
        public string? Criterion { get; init; }
    }
}
