using TextEvaluator.Core.Base;

namespace TextEvaluator.AI.AIDetector
{
    public class AIDetectResult: GradingResult
    {
        public TypeResult TypeResult { get; init; }
    }
    public enum TypeResult: byte
    {
        AI = 0,
        Human = 1
    }
}
