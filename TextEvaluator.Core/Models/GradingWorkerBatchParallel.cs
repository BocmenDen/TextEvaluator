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
            List<Task<List<KeyValuePair<IGradingCriterion, IGradingResult>>>> tasks = [];
            bool isNotStop = true;
            while (isNotStop)
            {
                var batch = new List<C>(batchSize);
                for (int i = 0; i < batchSize; i++)
                {
                    if(enumerator.MoveNext())
                        batch.Add(enumerator.Current);
                    else
                    {
                        isNotStop = false;
                        break;
                    }
                }

                if (batch.Count == 0)
                    break;

                tasks.AddRange(original.GetResult(batch, text, log).ToListAsync().AsTask());
            }
            foreach (var task in tasks)
            {
                var resItems = await task;
                foreach(var item in resItems)
                    yield return item;
            }
        }
    }
}