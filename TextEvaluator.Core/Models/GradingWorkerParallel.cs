using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.Core.Models
{
    public class GradingWorkerParallel<C, R> : IGradingWorker<C, R>
        where C : IGradingCriterion
        where R : IGradingResult
    {
        public string HashText { get; private set; }
        private readonly IReadOnlyList<IGradingWorker<C, R>> _original;
        public GradingWorkerParallel(IEnumerable<IGradingWorker<C, R>> original, int batchSize)
        {
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
            using var log = logging?.CreateChildLogging(nameof(GradingWorkerParallel<,>), this);
            var batch = new List<C>(_original.Count);
            while (true)
            {
                batch.Clear();
                for (int i = 0; i < _original.Count && enumerator.MoveNext(); i++)
                    batch.Add(enumerator.Current);

                if (batch.Count == 0)
                    yield break;

                var tasks = batch
                    .Select(async (crit, i) =>
                    {
                        var resultReturn = await _original[i].GetResult([crit], text, log).ToListAsync();
                        return resultReturn;
                    })
                    .ToList();

                var results = await Task.WhenAll(tasks);

                foreach (var item in results.SelectMany(x => x))
                    yield return item;
            }
        }
    }
}
