using System.Text;
using System.Text.Json.Serialization;
using TextEvaluator.Core.Base;
using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Interfaces;
using TextEvaluator.Core.Models;

namespace TextEvaluator.AI.Base
{
    [method: JsonConstructor]
    public class AIEvaluatorWorker(AIRequestBase aiRequestBase, int count, IGradingWorker<GradingCriterionAI> original, RootPrompt prompt) : IGradingWorker<GradingCriterionAI>
    {
        private const string MESSAGE_EXPERT_RESULT = $"Эксперт {{{ILogging.INDEX_PARAM}}}, бал {{{ILogging.CRIT_RESULT_SCORE_PARAM}}}, коментарий {{{ILogging.DESCRIPTION_PARAM}}}";
        private const string MESSAGE_FINAL_RESULT = $"Заключение, бал {{{ILogging.CRIT_RESULT_SCORE_PARAM}}}, коментарий {{{ILogging.DESCRIPTION_PARAM}}}";

        private readonly IGradingWorker<GradingCriterionAI> _worker = original;
        private readonly AIRequestBase _aiRequestBase = aiRequestBase;
        public string HashText { get; private set; } = $"{aiRequestBase.HashText}|{prompt.HashText}|{original.HashText}".GetHashText();
        private readonly int _countRetry = count;
        private readonly RootPrompt _rootPrompt = prompt;

        public async IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<GradingCriterionAI> gradingCriterions, string text, ILogging? logging = null)
        {
            using var log = logging?.CreateChildLogging(typeof(AIEvaluatorWorker), this);
            foreach (var crit in gradingCriterions)
                yield return new(crit, await GetResultCrit(crit, text, log));
            yield break;
        }

        private async Task<GradingResultAI> GetResultCrit(GradingCriterionAI crit, string text, ILogging? logging = null)
        {
            var result = await _worker.GetResult(Enumerable.Range(0, _countRetry).Select(x => crit), text, logging).ToListAsync();

            if (result.Any(x => !x.Value.IsNotError))
                return (GradingResultAI)result.First().Value;

            StringBuilder sb = new();

            sb.AppendLine("Вот результаты оценок экспертов: ");
            int i = 0;
            var resultCast = result.Select(x => x.Value).Where(x => x.IsNotError).Cast<GradingResultAI>().ToList();

            if (resultCast.Count == 0)
                return GradingResult.CreateError<AIEvaluatorResult>("Произошла ошибка пустой ответ экспертов");

            foreach (var item in resultCast)
            {
                sb.AppendLine($"Эксперт № {++i}");
                sb.AppendLine($"Бал: {item.Score}");
                sb.AppendLine($"Коментарий: {item.Description}");
                logging.LogInfo(MESSAGE_EXPERT_RESULT, i, item.Score, item.Description);
            }

            var resultReturn = await _aiRequestBase.GetResult(
                [
                    new Message(){ Content = _rootPrompt.ApplayPromptTemplate(crit), Role = RoleType.System },
                    new Message(){ Content = sb.ToString(), Role = RoleType.User },
                    new Message(){ Content = text, Role = RoleType.User },
                ], crit.MaxScore);

            if (!resultReturn.IsNotError)
                return GradingResult.CreateError<AIEvaluatorResult>("Неизвестная ошибка");

            logging.LogInfo(MESSAGE_FINAL_RESULT, resultReturn.Score, resultReturn.Description);

            return new AIEvaluatorResult()
            {
                Score = resultReturn.Score,
                Description = resultReturn.Description,
                RetryResults = resultCast
            };
        }
    }
}
