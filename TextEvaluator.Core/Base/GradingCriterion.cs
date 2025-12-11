using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.Core.Base
{
    public abstract class GradingCriterion : IGradingCriterion
    {
        protected Dictionary<string, Func<string>> _getValues = [];
        public IReadOnlyDictionary<string, Func<string>> GetValues => _getValues;
        public double MaxScore { get; init; }
        public string? Criterion { get; init; }
        public string HashText
        {
            get
            {
                field ??= ComputeHashText();
                return field!;
            }
        }

        public GradingCriterion()
        {
            _getValues.Add($"{nameof(GradingCriterion)}.{nameof(MaxScore)}", () => MaxScore.ToString());
            _getValues.Add($"{nameof(GradingCriterion)}.{nameof(Criterion)}", () => Criterion ?? string.Empty);
        }

        protected abstract string ComputeHashText();
        public override string ToString() => $"MaxScore[{MaxScore}] -> {Criterion}";
    }
}
