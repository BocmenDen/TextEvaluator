using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.Core
{
    public class GradingEngine : IGradingEngine
    {
        private readonly Func<string, ILogging?, IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>>> _apply;
        public IReadOnlyList<IGradingCriterion> Criterion { get; private set; }
        public string HashText { get; private set; }

        internal GradingEngine(string hash, Func<string, ILogging?, IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>>> apply, IReadOnlyList<IGradingCriterion> gradingCriteria)
        {
            Criterion = gradingCriteria;
            HashText = hash;
            _apply = apply;
        }

        public IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> ApplyAsync(string text, ILogging? logging = null)
        {
            using var log = logging?.CreateChildLogging(nameof(GradingEngine), this);
            return _apply(text, log);
        }
    }
}