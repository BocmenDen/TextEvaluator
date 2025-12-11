namespace TextEvaluator.Core.Interfaces
{
    public interface IGradingResult
    {
        public double Score { get; init; }
        public string? Error { get; init; }

        public bool IsNotError { get; }
    }
}