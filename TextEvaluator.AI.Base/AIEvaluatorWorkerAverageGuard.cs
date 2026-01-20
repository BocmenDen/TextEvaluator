using TextEvaluator.Core.Base;
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
                int i = 0;
                do
                {
                    var result = await GetResultCrit(crit, text, log);
                    if (!result.IsNotError) { yield return new(crit, result); break; }
                    gradingResultAI = (AIEvaluatorResult)result;
                } while (Math.Abs(gradingResultAI.Score - (gradingResultAI.RetryResults.Select(x => x.Score).Sum() / count)) > maxOffset && i++ < 4);
                if (i >= 4) { yield return new (crit, GradingResult.CreateError<AIEvaluatorResult>("Невалидый ответ эксперта! Требует ВНИМАНИЯ")); continue; }
                yield return new(crit, gradingResultAI);
            }
            yield break;
        }
    }
}