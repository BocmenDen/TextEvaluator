using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.Core.Base
{
    public class GradingResult : IGradingResult
    {
        public required double Score { get; init; }
        public string? Error { get; init; }

        public bool IsNotError => string.IsNullOrEmpty(Error);

        public override string ToString() => IsNotError ? Score.ToString() : Error!;

        public static GradingResult CreateError(string error) => new() { Score = -1, Error = error };
    }
}
