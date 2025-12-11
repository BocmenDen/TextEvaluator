using System.Text.RegularExpressions;
using TextEvaluator.Core.Base;
using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.CollectionGrading.TextLengh
{
    public partial class GradingWorkerTextLengh : IGradingWorker<GradingCriterionTextLength, GradingResult>
    {
        [GeneratedRegex(@"\b\w+\b")]
        private static partial Regex GetCountWord();

        public string HashText => nameof(GradingWorkerTextLengh).GetHashText();

        public async IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<GradingCriterionTextLength> gradingCriterions, string text, ILogging? logging = null)
        {
            using var log = logging?.CreateChildLogging(nameof(GradingWorkerTextLengh), this);
            int wordCount = GetCountWord().Count(text);
            foreach (var crit in gradingCriterions)
            {
                double res = 0;
                if (wordCount < crit.MinLength)
                {
                    res = 0;
                }
                else if (wordCount > crit.MaxLength)
                {
                    res = crit.MaxScore;
                }
                else
                {
                    wordCount -= crit.MinLength;
                    res = (double)wordCount / (crit.MaxLength - crit.MinLength);
                    res *= crit.MaxScore;
                }
                log.LogInfo("Результат оценки по критерию {crit}: {score}", crit, res);
                yield return new(crit, new GradingResult()
                {
                    Score = res
                });
            }
            yield break;
        }
    }
}
