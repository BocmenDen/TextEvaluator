using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.Core.Models
{
    public class GradingWorkerBatchParallel<C, R>(IGradingWorker<C, R> original, int batchSize) : IGradingWorker<C, R>
        where C : IGradingCriterion
        where R : IGradingResult
    {
        public string HashText => original.HashText;

        public async IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<C> gradingCriterions, string text, ILogging? logging = null)
        {
            using var enumerator = gradingCriterions.GetEnumerator();
            using var log = logging?.CreateChildLogging(nameof(GradingWorkerBatchParallel<,>), this);
            var batch = new List<C>(batchSize);
            while (true)
            {
                batch.Clear();
                for (int i = 0; i < batchSize && enumerator.MoveNext(); i++)
                    batch.Add(enumerator.Current);

                if (batch.Count == 0)
                    yield break;

                var tasks = batch
                    .Select(async crit =>
                    {
                        var resultReturn = await original.GetResult([crit], text, log).ToListAsync();
                        return resultReturn;
                    })
                    .ToList();

                var results = await Task.WhenAll(tasks);

                foreach(var item in results.SelectMany(x => x))
                    yield return item;
            }
        }
    }
}