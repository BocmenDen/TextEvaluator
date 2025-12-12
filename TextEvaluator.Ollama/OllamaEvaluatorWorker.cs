using System.Text;
using System.Text.Json.Serialization;
using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Interfaces;
using TextEvaluator.Core.Models;

namespace TextEvaluator.Ollama
{
    public class OllamaEvaluatorWorker : IGradingWorker<GradingCriterionPrompt>
    {
        private const string MESSAGE_EXPERT_RESULT = $"Эксперт {{{ILogging.INDEX_PARAM}}}, бал {{{ILogging.CRIT_RESULT_SCORE_PARAM}}}, коментарий {{{ILogging.DESCRIPTION_PARAM}}}";
        private const string MESSAGE_FINAL_RESULT = $"Заключение, бал {{{ILogging.CRIT_RESULT_SCORE_PARAM}}}, коментарий {{{ILogging.DESCRIPTION_PARAM}}}";

        private readonly IGradingWorker<GradingCriterionPrompt> _worker;
        private readonly OllamaBase _ollamaBase;
        public string HashText { get; private set; }
        private readonly int _countRetry;
        private readonly RootPrompt _rootPrompt;

        public OllamaEvaluatorWorker(string url, string model, int count, RootPrompt prompt)
        {
            _ollamaBase = new OllamaBase(url, model);
            _worker = new OllamaWorker(_ollamaBase);
            _rootPrompt = prompt;
            _countRetry = count;
            HashText = GetHashText(_ollamaBase, count, _worker, prompt);
        }

        [JsonConstructor]
        public OllamaEvaluatorWorker(OllamaBase ollamaBase, int count, IGradingWorker<GradingCriterionPrompt> original, RootPrompt prompt)
        {
            _worker = original;
            _rootPrompt = prompt;
            _ollamaBase = ollamaBase;
            _countRetry = count;
            HashText = GetHashText(ollamaBase, count, original, prompt);
        }

        public OllamaEvaluatorWorker(string url, string model, int count, IGradingWorker<GradingCriterionPrompt> original, RootPrompt prompt)
            : this(new OllamaBase(url, model), count, original, prompt) { }

        private static string GetHashText(OllamaBase ollamaBase, int count, IGradingWorker<GradingCriterionPrompt> original, RootPrompt prompt)
            => $"{count}|{ollamaBase.Model}|{prompt.HashText}|{original.HashText}".GetHashText();

        public async IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<GradingCriterionPrompt> gradingCriterions, string text, ILogging? logging = null)
        {
            using var log = logging?.CreateChildLogging(typeof(OllamaEvaluatorWorker), this);
            foreach (var crit in gradingCriterions)
                yield return new(crit, await GetResultCrit(crit, text, log));
            yield break;
        }

        private async Task<GradingResultDescription> GetResultCrit(GradingCriterionPrompt crit, string text, ILogging? logging = null)
        {
            var tasks = Enumerable.Range(0, _countRetry).Select(_ => _worker.GetResult([crit], text).ToListAsync().AsTask());
            var results = await Task.WhenAll(tasks);

            var result = await _worker.GetResult(Enumerable.Range(0, _countRetry).Select(x => crit), text, logging).ToListAsync();

            if (result.Any(x => !x.Value.IsNotError))
                return (GradingResultDescription)result.First().Value;

            StringBuilder sb = new();

            sb.AppendLine("Оценки экспертов: ");
            int i = 0;
            foreach (var item in result.Select(x => x.Value).Where(x => x.IsNotError).Cast<GradingResultDescription>())
            {
                sb.AppendLine($"Эксперт № {++i}");
                sb.AppendLine($"Бал: {item.Score}");
                sb.AppendLine($"Коментарий: {item.Description}");
                logging.LogInfo(MESSAGE_EXPERT_RESULT, i, item.Score, item.Description);
            }

            var resultReturn = await _ollamaBase.GetFormatResponse(
                [
                    Message.CreateSystem(_rootPrompt.ApplayPromptTemplate(crit)),
                    Message.CreateSystem(sb.ToString()),
                    Message.CreateUser(text),
                ], crit.MaxScore);

            logging.LogInfo(MESSAGE_FINAL_RESULT, resultReturn.Score, resultReturn.Description);

            return resultReturn;
        }
    }
}
