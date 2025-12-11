using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Interfaces;
using TextEvaluator.Core.Models;

namespace TextEvaluator.Ollama
{
    public class OllamaWorker(OllamaBase ollamaBase) : IGradingWorker<GradingCriterionPrompt, GradingResultDescription>
    {
        private readonly OllamaBase _ollamaBase = ollamaBase;
        public string HashText { get; private set; } = $"{ollamaBase.Model}|{nameof(OllamaWorker)}".GetHashText();

        public OllamaWorker(string url, string model) : this(new OllamaBase(url, model)) { }

        public async IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<GradingCriterionPrompt> gradingCriterions, string text, ILogging? logging = null)
        {
            using var log = logging?.CreateChildLogging(nameof(OllamaWorker), this);
            foreach (var crit in gradingCriterions)
            {
                log.LogInfo("Обрабатываю критерий: {crit}", crit);
                var messageContent = await _ollamaBase.GetFormatResponse(
                        [
                            Message.CreateSystem(crit.ApplayPromptTemplate()),
                            Message.CreateSystem("Пример формата ответа JSON: { \"score\": 0.2, \"description\": \"Такая оценка была поставленна из-за ...\"}"),
                            Message.CreateUser(text),
                        ], crit.MaxScore);
                KeyValuePair<IGradingCriterion, IGradingResult> result = new(crit, messageContent);
                log.LogInfo("Результат обработки критерия: {result}", result);
                yield return result;
            }
            yield break;
        }
    }
}