using System.Text.Json.Serialization;
using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.Core.Models
{
    public class GradingWorkerParallel<C> : IGradingWorker<C>
        where C : IGradingCriterion
    {
        public string HashText { get; private set; }
        private readonly IReadOnlyList<IGradingWorker<C>> _original;
        private readonly int _batchSize;
        [method: JsonConstructor]
        public GradingWorkerParallel(IEnumerable<IGradingWorker<C>> original, int batchSize)
        {
            _batchSize = batchSize;
            if (!original.Any())
                throw new ArgumentException("Коллекция воркеров пуста");

            _original = [.. original];

            if (_original.Select(x => x.HashText).GroupBy(x => x).Count() > 1)
                throw new Exception("Хеш всех воркеров должен совпадать; иначе система перестаёт быть детерминированной.");

            _original = [.. original];
            HashText = _original[0].HashText;
        }

        public async IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<C> gradingCriterions, string text, ILogging? logging = null)
        {
            using var enumerator = gradingCriterions.GetEnumerator();
            using var log = logging?.CreateChildLogging(typeof(GradingWorkerParallel<>), this);
            int indexWorker = 0;
            List<Task<List<KeyValuePair<IGradingCriterion, IGradingResult>>>> tasks = [];
            while (true)
            {
                var batch = new List<C>(_original.Count);
                for (int i = 0; i < _batchSize && enumerator.MoveNext(); i++)
                    batch.Add(enumerator.Current);

                if (batch.Count == 0)
                    yield break;

                if (tasks.Count(x => !x.IsCompleted) > _batchSize)
                {
                    foreach (var taskC in tasks)
                    {
                        var taskRes = await taskC;
                        foreach (var resItem in taskRes)
                        {
                            yield return resItem;
                        }
                    }
                }

                tasks.Add(_original[indexWorker].GetResult(batch, text, log).ToListAsync().AsTask());
                indexWorker = (indexWorker + 1) % _batchSize;
            }
        }
    }
}
