using System.Text.Json.Serialization;
using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.AI.Base
{
    [method: JsonConstructor]
    public class AIWorker(AIRequestBase aiRequestBase) : IGradingWorker<GradingCriterionAI>
    {
        private const string MESSAGE_START_HANDLE = $"Обрабатываю критерий: {{{ILogging.CRIT_PARAM}}}";
        private const string MESSAGE_RESULT = $"Результат обработки критерия: {{{ILogging.CRIT_RESULT_OBJ_PARAM}}}";
        private readonly AIRequestBase _aiRequestBase = aiRequestBase;
        public string HashText { get; private set; } = $"{aiRequestBase.HashText}|{nameof(AIWorker)}".GetHashText();

        public async IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<GradingCriterionAI> gradingCriterions, string text, ILogging? logging = null)
        {
            using var log = logging?.CreateChildLogging(typeof(AIWorker), this);
            foreach (var crit in gradingCriterions)
            {
                log.LogInfo(MESSAGE_START_HANDLE, crit);
                var result = await _aiRequestBase.GetResult(
                        [
                            new Message(){ Content = crit.ApplayPromptTemplate(), Role = RoleType.System },
                            new Message(){ Content = $"ТЕКСТ ДЛЯ ОЦЕНИВАНИЯ:\n{text}", Role = RoleType.User },
                        ], crit.MaxScore);
                log.LogInfo(MESSAGE_RESULT, result);
                yield return new(crit, result);
            }
            yield break;
        }
    }
}