using TextEvaluator.Core.Base;
using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Interfaces;
using TextEvaluator.Core.Models;

namespace TextEvaluator.AI.Base
{
    public abstract class AIRequestBase(int countRetry) : IHasHash
    {
        private const string MESSAGE_ERROR_SCORE = $"Полученная оценка {{{ILogging.CRIT_RESULT_SCORE_PARAM}_output}} больше заданной {{{ILogging.CRIT_RESULT_SCORE_PARAM}_input}}";
        private const string MESSAGE_RETRY_GET_SCORE = $"Полученная оценка равна {{{ILogging.CRIT_RESULT_SCORE_PARAM}}}, повторю запрос для уточнения";
        private const string MESSAGE_START_GET_SCORE = $"Выполняю попытку оценивания: {{{ILogging.INDEX_PARAM}}}";

        private readonly int _countRetry = countRetry;

        public abstract string HashText { get; }

        public async Task<GradingResultAI> GetResult(IEnumerable<Message> messages, double maxScore, ILogging? logging = null)
        {
            GradingResultAI returnItem;
            int countRetry = 0;
            int globalRetry = 0;
            using var log = logging?.CreateChildLogging(typeof(AIRequestBase), this);

            do
            {
                try
                {
                    log.LogInfo(MESSAGE_START_GET_SCORE, globalRetry++);

                    returnItem = await GetFormatResponse(messages, maxScore, log);

                    if (returnItem.Score > maxScore)
                    {
                        log.LogWarning(MESSAGE_ERROR_SCORE, returnItem.Score, maxScore);
                        continue;
                    }
                    if (returnItem.Score == 0 && countRetry++ < _countRetry)
                    {
                        log.LogWarning(MESSAGE_RETRY_GET_SCORE, 0.0);
                        continue;
                    }
                    break;
                }
                catch (Exception e)
                {
                    returnItem = GradingResult.CreateError<GradingResultAI>(e.Message);
                    log.LogException(e);
                    if (globalRetry++ > _countRetry) break;
                }
            } while (true);

            return returnItem;
        }

        protected abstract Task<GradingResultAI> GetFormatResponse(IEnumerable<Message> messages, double maxScore, ILogging? logging = null);
    }
}