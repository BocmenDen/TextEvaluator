using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.Core.Models
{
    public class GradingDecoratingWorker<C, R>(IGradingWorker<C, R> original, Func<C, ILogging?, bool> filter, Func<IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>>, ILogging?, IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>>> applayeReselt, string? hashKey = null) : IGradingWorker<C, R>
        where C : IGradingCriterion
        where R : IGradingResult
    {
        private readonly IGradingWorker<C, R> _original = original;
        private readonly Func<C, ILogging?, bool> _filter = filter;
        private readonly Func<IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>>, ILogging?, IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>>> _applayeReselt = applayeReselt;
        public string HashText { get; private set; } = $"{nameof(GradingDecoratingWorker<,>)}|{hashKey}|{original.HashText}".GetHashText();

        public IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<C> gradingCriterions, string text, ILogging? logging = null)
        {
            using var log = logging?.CreateChildLogging(nameof(GradingDecoratingWorker<,>), this);
            var crit = gradingCriterions.Where(c => _filter(c, log));
            var result = _original.GetResult(crit, text, log);
            return _applayeReselt(result, log);
        }
    }
}