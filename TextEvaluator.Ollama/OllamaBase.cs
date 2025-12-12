using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Interfaces;
using TextEvaluator.Core.Models;

namespace TextEvaluator.Ollama
{
    [method: JsonConstructor]
    public class OllamaBase(string url, string model, int countRetry = 2)
    {
        private const string MESSAGE_ERROR_SCORE = $"Полученная оценка {{{ILogging.CRIT_RESULT_SCORE_PARAM}_output}} больше заданной {{{ILogging.CRIT_RESULT_SCORE_PARAM}_input}}";
        private const string MESSAGE_RETRY_GET_SCORE = $"Полученная оценка равна {{{ILogging.CRIT_RESULT_SCORE_PARAM}}}, повторю запрос для уточнения";
        private const string MESSAGE_START_GET_SCORE = $"Выполняю попытку оценивания: {{{ILogging.INDEX_PARAM}}}";
        private const string RESULT_UNKOWN_ERROR = "Не удалось оценить, неизвестная ошибка";
        private const string MESSAGE_SEND = "Отправляю запрос на {url}/api/chat: {payload}";
        private readonly IEnumerable<Message> RootMessageFormat = [Message.CreateSystem("Пример формата ответа JSON: { \"score\": 0.2, \"description\": \"Такая оценка была поставленна из-за ...\"}")];

        internal string Url { get; private set; } = url;
        internal string Model { get; private set; } = model;
        private readonly int _countRetry = countRetry;

        private readonly HttpClient _client = new()
        {
            BaseAddress = new Uri(url)
        };

        public async Task<GradingResultDescription> GetFormatResponse(IEnumerable<Message> messages, double maxScore, ILogging? logging = null)
        {
            GradingResultDescription returnItem = new()
            {
                Score = -1,
                Error = RESULT_UNKOWN_ERROR
            };
            int countRetry = 0;
            int globalRetry = 0;
            using var log = logging?.CreateChildLogging(typeof(OllamaBase), this);
            try
            {
                do
                {
                    log.LogInfo(MESSAGE_START_GET_SCORE, globalRetry++);
                    var payload = new
                    {
                        model = Model,
                        messages = RootMessageFormat.Concat(messages).Select(x => new
                        {
                            role = x.Role,
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
                    var json = System.Text.Json.JsonSerializer.Serialize(payload);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    log.LogInfo(MESSAGE_SEND, _client.BaseAddress, json);
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

                            returnItem = new GradingResultDescription()
                            {
                                Score = score,
                                Description = description
                            };
                        }
                    }

                    if (returnItem.Score > maxScore)
                    {
                        log.LogWarning(MESSAGE_ERROR_SCORE, returnItem.Score, maxScore);
                        continue;
                    }
                    if (countRetry++ < _countRetry)
                    {
                        log.LogWarning(MESSAGE_RETRY_GET_SCORE, 0.0);
                        continue;
                    }
                    break;
                } while (true);
            }
            catch (Exception e)
            {
                returnItem = new GradingResultDescription()
                {
                    Score = -1,
                    Error = e.Message
                };
                log.LogException(e);
            }

            return returnItem;
        }
    }
}