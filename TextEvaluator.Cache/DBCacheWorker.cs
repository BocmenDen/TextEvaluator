using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.Cache
{
    public class DBCacheWorker<C, R> : DbContext, IGradingWorker<C, R>
        where C : IGradingCriterion
        where R : IGradingResult
    {
        public DbSet<DBCacheModel> DataTable { private get; init; } = null!;
        public string HashText => _worker.HashText;
        private readonly IGradingWorker<C, R> _worker;
                
        public DBCacheWorker(DbContextOptions<DBCacheWorker<C, R>> options, IGradingWorker<C, R> worker) : base(options)
        {
            Database.EnsureCreated();
            _worker = worker;
        }

        public async IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<C> gradingCriterions, string text, ILogging? logging = null)
        {
            string key = text.GetHashText();
            List<C> critNoCache = [];
            using var log = logging?.CreateChildLogging(nameof(DBCacheWorker<,>), this);
            foreach (var crit in gradingCriterions)
            {
                var search = await DataTable.AsNoTracking().FirstOrDefaultAsync(x => x.IDCriteria == crit.HashText && x.Key == key);
                if (search == null)
                {
                    critNoCache.Add(crit);
                    continue;
                }
                var type = Type.GetType(search.FullNameTypename);
                if (type == null)
                {
                    critNoCache.Add(crit);
                    continue;
                }
                var result = JsonSerializer.Deserialize(search.JsonData, type);
                if (result is not IGradingResult itemRes)
                {
                    critNoCache.Add(crit);
                    continue;
                }
                log.LogInfo("Критерий {crit}, взят из кеша", crit);
                yield return new(crit, itemRes);
            }

            await foreach (var item in _worker.GetResult(critNoCache, text, logging))
            {
                var type = item.Value.GetType();
                var typeName = type.AssemblyQualifiedName;
                if (typeName != null && item.Value.IsNotError)
                {
                    var entity = new DBCacheModel
                    {
                        Key = key,
                        IDCriteria = item.Key.HashText,
                        JsonData = JsonSerializer.Serialize(item.Value, type),
                        FullNameTypename = typeName
                    };
                    DataTable.Add(entity);
                    await SaveChangesAsync();
                    log.LogInfo("Результат {result} критерия {crit}, сохранён в кеш", item.Value, item.Key);
                    yield return item;
                }
            }

            yield break;
        }
    }
}
