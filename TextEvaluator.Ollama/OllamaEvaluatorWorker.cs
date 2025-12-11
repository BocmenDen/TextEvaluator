using System.Text;
using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Interfaces;
using TextEvaluator.Core.Models;

namespace TextEvaluator.Ollama
{
    public class OllamaEvaluatorWorker : IGradingWorker<GradingCriterionPrompt, GradingResultDescription>
    {
        private readonly IGradingWorker<GradingCriterionPrompt, GradingResultDescription> _worker;
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
            HashText = GetHashText(_ollamaBase, count, prompt, _worker);
        }

        public OllamaEvaluatorWorker(OllamaBase ollamaBase, int count, RootPrompt prompt, IGradingWorker<GradingCriterionPrompt, GradingResultDescription> worker)
        {
            _worker = worker;
            _rootPrompt = prompt;
            _ollamaBase = ollamaBase;
            _countRetry = count;
            HashText = GetHashText(ollamaBase, count, prompt, worker);
        }

        public OllamaEvaluatorWorker(string url, string model, int count, RootPrompt prompt, IGradingWorker<GradingCriterionPrompt, GradingResultDescription> worker)
            : this(new OllamaBase(url, model), count, prompt, worker) { }

        private static string GetHashText(OllamaBase ollamaBase, int count, RootPrompt prompt, IGradingWorker<GradingCriterionPrompt, GradingResultDescription> worker)
            => $"{count}|{ollamaBase.Model}|{prompt.HashText}|{worker.HashText}".GetHashText();

        public async IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<GradingCriterionPrompt> gradingCriterions, string text, ILogging? logging = null)
        {
            using var log = logging?.CreateChildLogging(nameof(OllamaEvaluatorWorker), this);
            foreach (var crit in gradingCriterions)
                yield return new(crit, await GetResultCrit(crit, text, logging));
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
                logging.LogInfo("Эксперт {expert}, бал {score}, коментарий {description}", i, item.Score, item.Description);
            }

            var resultReturn = await _ollamaBase.GetFormatResponse(
                [
                    Message.CreateSystem(_rootPrompt.ApplayPromptTemplate(crit)),
                        Message.CreateSystem(sb.ToString()),
                        Message.CreateUser(text),
                    ], crit.MaxScore);

            logging.LogInfo("Заключение, бал {score}, коментарий {description}", resultReturn.Score, resultReturn.Description);

            return resultReturn;
        }
    }
}
