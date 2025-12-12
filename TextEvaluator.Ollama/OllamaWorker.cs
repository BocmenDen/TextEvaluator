using System.Text.Json.Serialization;
using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Interfaces;
using TextEvaluator.Core.Models;

namespace TextEvaluator.Ollama
{
    [method: JsonConstructor]
    public class OllamaWorker(OllamaBase ollamaBase) : IGradingWorker<GradingCriterionPrompt>
    {
        private const string MESSAGE_START_HANDLE = $"Обрабатываю критерий: {{{ILogging.CRIT_PARAM}}}";
        private const string MESSAGE_RESULT = $"Результат обработки критерия: {{{ILogging.CRIT_RESULT_OBJ_PARAM}}}";
        private readonly OllamaBase _ollamaBase = ollamaBase;
        public string HashText { get; private set; } = $"{ollamaBase.Model}|{nameof(OllamaWorker)}".GetHashText();

        public OllamaWorker(string url, string model) : this(new OllamaBase(url, model)) { }

        public async IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<GradingCriterionPrompt> gradingCriterions, string text, ILogging? logging = null)
        {
            using var log = logging?.CreateChildLogging(typeof(OllamaWorker), this);
            foreach (var crit in gradingCriterions)
            {
                log.LogInfo(MESSAGE_START_HANDLE, crit);
                var result = await _ollamaBase.GetFormatResponse(
                        [
                            Message.CreateSystem(crit.ApplayPromptTemplate()),
                            Message.CreateUser(text),
                        ], crit.MaxScore);
                log.LogInfo(MESSAGE_RESULT, result);
                yield return new(crit, result);
            }
            yield break;
        }
    }
}