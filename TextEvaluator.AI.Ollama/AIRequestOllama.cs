using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TextEvaluator.AI.Base;
using TextEvaluator.Core.Base;
using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.AI.Ollama
{
    [method: JsonConstructor]
    public class AIRequestOllama(string url, string model, int countRetry = 3) : AIRequestBase(countRetry)
    {
        private const string RESULT_UNKOWN_ERROR = "Не удалось оценить, неизвестная ошибка";

        private readonly string _hashText = model.GetHashText();
        public override string HashText => _hashText;

        private readonly HttpClient _client = new()
        {
            BaseAddress = new Uri(url),
            Timeout = TimeSpan.FromMinutes(5),
        };

        protected override async Task<GradingResultAI> GetFormatResponse(IEnumerable<Message> messages, double maxScore, ILogging? logging = null)
        {
            var payload = new
            {
                model,
                messages = messages.Select(x => new
                {
                    role = x.Role.ConvertRole(),
                    content = x.Content
                }).ToList(),
                stream = false,
                format = new
                {
                    type = "object",
                    properties = new
                    {
                        score = new
                        {
                            type = "number"
                        },
                        description = new { type = "string" }
                    },
                    required = new[] { "score", "description" }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _client.PostAsync("/api/chat", content);
            response.EnsureSuccessStatusCode();
            var responseText = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseText);
            if (doc.RootElement.TryGetProperty("message", out var messageElem) &&
                messageElem.TryGetProperty("content", out var contentElem))
            {
                var messageContent = contentElem.GetString();
                if (!string.IsNullOrEmpty(messageContent))
                {
                    using var resultDoc = JsonDocument.Parse(messageContent);
                    var root = resultDoc.RootElement;

                    var score = root.GetProperty("score").GetDouble();
                    var description = root.GetProperty("description").GetString() ?? string.Empty;

                    return new GradingResultAI()
                    {
                        Score = score,
                        Description = description
                    };
                }
            }

            return GradingResult.CreateError<GradingResultAI>(RESULT_UNKOWN_ERROR);
        }
    }
}
