using TextEvaluator.Core.Models;

namespace TextEvaluator.AI.Base
{
    public class AIEvaluatorResult : GradingResultAI
    {
        public IReadOnlyList<GradingResultAI> RetryResults { get; init; } = [];
    }
}
