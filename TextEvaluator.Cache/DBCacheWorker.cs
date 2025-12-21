using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using TextEvaluator.Core.Extensions;
using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.Cache
{
    public class DBCacheWorker<C> : DbContext, IGradingWorker<C>
        where C : IGradingCriterion
    {
        public const string MESSAGE_ADD_CACHE = $"Результат {{{ILogging.CRIT_RESULT_OBJ_PARAM}}} критерия {{{ILogging.CRIT_PARAM}}}, сохранён в кеш";
        public const string MESSAGE_LOAD_CACHE = $"Результат {{{ILogging.CRIT_RESULT_OBJ_PARAM}}} критерия {{{ILogging.CRIT_PARAM}}}, взят из кеша";

        public DbSet<DBCacheModel> DataTable { private get; init; } = null!;
        public string HashText => _worker.HashText;
        private readonly IGradingWorker<C> _worker;
        [JsonConstructor]
        public DBCacheWorker(DbContextOptions<DBCacheWorker<C>> options, IGradingWorker<C> original) : base(options)
        {
            Database.EnsureCreated();
            _worker = original;
        }

        public async IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> GetResult(IEnumerable<C> gradingCriterions, string text, ILogging? logging = null)
        {
            string key = text.GetHashText();
            List<C> critNoCache = [];
            using var log = logging?.CreateChildLogging(typeof(DBCacheWorker<>), this);
            foreach (var crit in gradingCriterions)
            {
                var search = await DataTable.AsNoTracking().FirstOrDefaultAsync(x => x.IDCriteria == crit.HashText && x.Key == key && x.IDModel == _worker.HashText);
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
                log.LogInfo(MESSAGE_LOAD_CACHE, itemRes, crit);
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
                        IDModel = _worker.HashText,
                        JsonData = JsonSerializer.Serialize(item.Value, type),
                        FullNameTypename = typeName
                    };
                    DataTable.Add(entity);
                    await SaveChangesAsync();
                    log.LogInfo(MESSAGE_ADD_CACHE, item.Value, item.Key);
                }
                yield return item;
            }

            yield break;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DBCacheModel>()
                .HasKey(x => new { x.Key, x.IDCriteria });

            modelBuilder.Entity<DBCacheModel>()
                .Property(x => x.JsonData)
                .IsRequired();

            modelBuilder.Entity<DBCacheModel>()
                .Property(x => x.FullNameTypename)
                .IsRequired();
        }
    }
}