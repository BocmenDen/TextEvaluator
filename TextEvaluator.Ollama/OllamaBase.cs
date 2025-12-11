using System.Text;
using System.Text.Json;
using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Interfaces;
using TextEvaluator.Core.Models;

namespace TextEvaluator.Ollama
{
    public class OllamaBase(string url, string model)
    {
        internal string Url { get; private set; } = url;
        internal string Model { get; private set; } = model;

        private readonly HttpClient _client = new()
        {
            BaseAddress = new Uri(url)
        };

        public async Task<GradingResultDescription> GetFormatResponse(IEnumerable<Message> messages, double maxScope, ILogging? logging = null)
        {
            GradingResultDescription returnItem = new()
            {
                Score = -1,
                Error = "Не удалось оценить, неизвестная ошибка"
            };
            int countRetry = 0;
            int globalRetry = 0;
            using var log = logging?.CreateChildLogging(nameof(OllamaBase), this);
            try
            {
                do
                {
                    log.LogInfo("Выполняю попытку оценивания: {count}", globalRetry++);
                    var payload = new
                    {
                        model = Model,
                        messages = messages.Select(x => new
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

                            returnItem = new GradingResultDescription()
                            {
                                Score = score,
                                Description = description
                            };
                        }
                    }

                    if (returnItem.Score > maxScope)
                    {
                        log.LogWarning("Полученная оценка {scope_output} больше заданной {scope_input}", returnItem.Score, maxScope);
                        continue;
                    }
                    if (countRetry++ < 2)
                    {
                        log.LogWarning("Полученная оценка равна 0, повторю запрос для уточнения");
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
                log.LogError("Произошла неизвестная ошибка: {error}", e);
            }

            return returnItem;
        }
    }
}