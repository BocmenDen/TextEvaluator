using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.AI.Base
{
    public class AIEvaluatorWorkerAverageGuard(AIRequestBase aiRequestBase, int count, IGradingWorker<GradingCriterionAI> original, RootPrompt prompt, double maxAverageDeviationPercent) : AIEvaluatorWorker(aiRequestBase, count, original, prompt)
    {
        public override async IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<GradingCriterionAI> gradingCriterions, string text, ILogging? logging = null)
        {
            using var log = logging?.CreateChildLogging(typeof(AIEvaluatorWorkerAverageGuard), this);
            foreach (var crit in gradingCriterions)
            {
                double maxOffset = crit.MaxScore * maxAverageDeviationPercent;
                AIEvaluatorResult gradingResultAI = null!;
                do
                {
                    var result = await GetResultCrit(crit, text, log);
                    if (!result.IsNotError) { yield return new(crit, result); break; }
                    gradingResultAI = (AIEvaluatorResult)result;
                } while (Math.Abs(gradingResultAI.Score - (gradingResultAI.RetryResults.Select(x => x.Score).Sum() / count)) > maxOffset);
                yield return new(crit, gradingResultAI);
            }
            yield break;
        }
    }
}