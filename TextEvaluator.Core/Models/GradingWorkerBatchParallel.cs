using System.Text.Json.Serialization;
using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.Core.Models
{
    [method: JsonConstructor]
    public class GradingWorkerBatchParallel<C>(IGradingWorker<C> original, int batchSize) : IGradingWorker<C>
        where C : IGradingCriterion
    {
        public string HashText => original.HashText;

        public async IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<C> gradingCriterions, string text, ILogging? logging = null)
        {
            using var enumerator = gradingCriterions.GetEnumerator();
            using var log = logging?.CreateChildLogging(typeof(GradingWorkerBatchParallel<>), this);
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

                foreach (var item in results.SelectMany(x => x))
                    yield return item;
            }
        }
    }
}