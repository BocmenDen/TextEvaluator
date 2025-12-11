using TextEvaluator.Core.Base;
using TextEvaluator.Core.Extensions;

namespace TextEvaluator.Core.Models
{
    public class GradingCriterionPrompt : GradingCriterion
    {
        public required RootPrompt RootPrompt { get; init; }
        public required string Prompt { get; init; }

        public GradingCriterionPrompt()
        {
            _getValues.Add($"{nameof(GradingCriterionPrompt)}.{nameof(Prompt)}", () => MaxScore.ToString());
            _getValues.Add($"{nameof(RootPrompt)}.{nameof(RootPrompt.PromptTemplate)}", () => RootPrompt!.PromptTemplate ?? string.Empty);
        }

        public string ApplayPromptTemplate() => RootPrompt.ApplayPromptTemplate(this);

        protected override string ComputeHashText() => $"{nameof(GradingCriterionPrompt)}{MaxScore:0.##########}{Prompt}|{RootPrompt.HashText}".GetHashText();
    }
}