using TextEvaluator.Core.Base;
using TextEvaluator.Core.Extensions;

namespace TextEvaluator.CollectionGrading.TextLengh
{
    public class GradingCriterionTextLength : GradingCriterion
    {
        public required int MinLength { get; init; } = 0;
        public required int MaxLength { get; init; }

        public GradingCriterionTextLength()
        {
            _getValues.Add($"{nameof(GradingCriterionTextLength)}.{nameof(MinLength)}", () => MinLength.ToString());
            _getValues.Add($"{nameof(GradingCriterionTextLength)}.{nameof(MaxLength)}", () => MaxLength.ToString());
        }

        protected override string ComputeHashText() => $"{nameof(GradingCriterionTextLength)}{MaxScore:0.##########}{MinLength}{MaxLength}".GetHashText();
    }
}
