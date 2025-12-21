using TextEvaluator.Core.Base;
using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Models;

namespace TextEvaluator.AI.Base
{
    public class GradingCriterionAI : GradingCriterion
    {
        public required RootPrompt RootPrompt { get; init; }
        public required string Prompt { get; init; }

        public GradingCriterionAI()
        {
            _getValues.Add($"{nameof(GradingCriterionAI)}.{nameof(Prompt)}", () => Prompt ?? string.Empty);
            _getValues.Add($"{nameof(RootPrompt)}.{nameof(RootPrompt.PromptTemplate)}", () => RootPrompt!.PromptTemplate ?? string.Empty);
        }

        public string ApplayPromptTemplate() => RootPrompt.ApplayPromptTemplate(this);

        protected override string ComputeHashText() => $"{nameof(GradingCriterionAI)}{MaxScore:0.##########}{Prompt}|{RootPrompt.HashText}".GetHashText();
    }
}