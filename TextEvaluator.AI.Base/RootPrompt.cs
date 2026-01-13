using System.Text.RegularExpressions;
using TextEvaluator.Core.Base;
using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.AI.Base
{
    public partial class RootPrompt : IHasHash
    {
        [GeneratedRegex(@"\{([\w|\.]+)\}")]
        private static partial Regex PatternInsert();

        public string PromptTemplate { get; private set; }
        public string HashText { get; private set; }

        public RootPrompt(string promptTemplate)
        {
            PromptTemplate = promptTemplate;
            HashText = PromptTemplate.GetHashText();
        }

        public string ApplayPromptTemplate(GradingCriterion gradingCriterion)
        {
            string template = PromptTemplate ?? string.Empty;

            var result = PatternInsert().Replace(template, match =>
            {
                string key = match.Groups[1].Value;
                if (gradingCriterion.GetValues.TryGetValue(key, out var getter))
                    return getter();
                else
                    return match.Value;
            });

            return result;
        }

        public override string ToString() => PromptTemplate;
    }
}
