using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TextEvaluator.AI.Base;
using TextEvaluator.Core.Base;
using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.AI.Bothub
{
    public class AIRequestBothub : AIRequestBase
    {
        public const string RESULT_UNKOWN_ERROR = "Не удалось оценить, неизвестная ошибка";

        private const string URL = "https://bothub.chat/api/v2/openai/v1/chat/completions";

        private readonly string _model;
        private readonly string _hashText;
        public override string HashText => _hashText;

        private readonly HttpClient _client;

        [JsonConstructor]
        public AIRequestBothub(string apiKey, string model, int countRetry = 3, int timeoutMinutes = 8) : base(countRetry)
        {
            _model = model;
            _hashText = model.GetHashText();
            _client = new()
            {
                Timeout = TimeSpan.FromMinutes(timeoutMinutes)
            };
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        protected override async Task<GradingResultAI> GetFormatResponse(IEnumerable<Message> messages, double maxScore, ILogging? logging = null)
        {
            var payload = new
            {
                model = _model,
                messages = messages.Select(x => new
                {
                    role = x.Role.ConvertRole(),
                    content = x.Content
                }).ToList(),
                stream = false,
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "grading_result",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            properties = new
                            {
                                score = new
                                {
                                    type = "number",
                                    description = $"Итоговый бал от 0.0 до {maxScore}"
                                },
                                description = new
                                {
                                    type = "string",
                                    description = $"Поле для комментария, почему поставлен именно такой бал",
                                    content_language = "ru"
                                }
                            },
                            required = new[] { "score", "description" },
                            additionalProperties = false
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _client.PostAsync(URL, content);
            response.EnsureSuccessStatusCode();
            var responseText = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseText);
            if (doc.RootElement.TryGetProperty("choices", out var choicesElem) &&
                choicesElem.ValueKind == JsonValueKind.Array &&
                choicesElem.GetArrayLength() > 0)
            {
                var choice = choicesElem[0];

                if (choice.TryGetProperty("message", out var messageElem) &&
                    messageElem.TryGetProperty("content", out var contentElem))
                {
                    var messageContent = contentElem.GetString();

                    if (!string.IsNullOrWhiteSpace(messageContent))
                    {
                        using var resultDoc = JsonDocument.Parse(messageContent);
                        var root = resultDoc.RootElement;

                        var score = root.GetProperty("score").GetDouble();
                        var description = root.GetProperty("description").GetString() ?? string.Empty;

                        return new GradingResultAI
                        {
                            Score = score,
                            Description = description
                        };
                    }
                }
            }

            return GradingResult.CreateError<GradingResultAI>(RESULT_UNKOWN_ERROR);
        }
    }
}
