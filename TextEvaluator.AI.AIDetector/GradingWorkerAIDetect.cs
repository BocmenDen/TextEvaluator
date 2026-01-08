using System.Net.Http.Json;
using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.AI.AIDetector
{
    public class GradingWorkerAIDetect(string url) : IGradingWorker<GradingCriterionAIDetect>
    {
        private readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri(url),
            Timeout = TimeSpan.FromMinutes(10)
        };

        public string HashText => string.Empty;

        public async IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<GradingCriterionAIDetect> gradingCriterions, string text, ILogging? logging = null)
        {
            using var log = logging?.CreateChildLogging(typeof(GradingWorkerAIDetect), this); // TODO Logging
            var response = await _httpClient.PostAsJsonAsync("/detect", new { text });
            response.EnsureSuccessStatusCode();
            var aiResultBase = await response.Content.ReadFromJsonAsync<AIDetectResult>() ?? throw new Exception("Не удалось десериализовать ответ от сервера");
            foreach (var crit in gradingCriterions)
            {
                yield return new(crit, new AIDetectResult()
                {
                     Score = aiResultBase.Score * crit.MaxScore,
                     Error = aiResultBase.Error
                });
            }
            yield break;
        }
    }
}