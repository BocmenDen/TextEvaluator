using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Text;
using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.Core
{
    public class GradingEngineBuilder
    {
        private readonly List<IGradingCriterion> _gradingCriteria = [];
        private readonly List<object> gradingWorkers = [];

        public void AddGradingCriterion<T>(T gradingCriterion) where T : IGradingCriterion => _gradingCriteria.Add(gradingCriterion);
        public void AddGradingWorker(object gradingWorker) => gradingWorkers.Add(gradingWorker);


        public GradingEngine CreateEngine()
        {
            Dictionary<Type, object> dictWorkers = GetWorkerByCritType();
            Dictionary<Type, List<object>> dictCriteria = ConnectCritToWorker(dictWorkers);
            List<Func<string, ILogging?, IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>>>> fList = GetFunctions(dictWorkers, dictCriteria);

            async IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> apply(string text, ILogging? logging)
            {
                foreach (var f in fList)
                {
                    var res = f(text, logging);
                    await foreach (var fRes in res)
                    {
                        yield return fRes;
                    }
                }
            }

            var hash = ComputeHashBytes();
            return new GradingEngine(hash, apply, _gradingCriteria);
        }

        private static List<Func<string, ILogging?, IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>>>> GetFunctions(Dictionary<Type, object> dictWorkers, Dictionary<Type, List<object>> dictCriteria)
        {
            List<Func<string, ILogging?, IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>>>> fList = [];
            foreach (var typeCrit in dictCriteria)
            {
                var worker = dictWorkers[typeCrit.Key]!;
                var method = worker.GetType().GetMethod(nameof(IGradingWorker<,>.GetResult), [typeof(IEnumerable<>).MakeGenericType(typeCrit.Key), typeof(string), typeof(ILogging)]) ?? throw new Exception("Не найден нужный метод у исполнителя");

                var workerExpr = Expression.Constant(worker);
                var criteriaExpr = Expression.Constant(CastToTypedEnumerable(typeCrit.Value, typeCrit.Key));
                var textParam = Expression.Parameter(typeof(string), "text");

                var callExpr = Expression.Call(workerExpr, method, criteriaExpr, textParam);

                var typeResult = method.ReturnType.GetGenericArguments()[0];
                var genericArgs = typeResult.GetGenericArguments();

                var methodConvertCall = typeof(GradingEngineBuilder)
                    .GetMethod(nameof(ConvertToInterfaceResult), BindingFlags.Static | BindingFlags.NonPublic)!
                    .MakeGenericMethod(genericArgs[0], genericArgs[1]);

                var callConvertExpr = Expression.Call(methodConvertCall, callExpr);

                var lambda = Expression.Lambda<Func<string, ILogging?, IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>>>>(
                    callConvertExpr,
                    textParam
                );

                var compiledResult = lambda.Compile();
                fList.Add(compiledResult);
            }
            return fList;
        }

        private static async IAsyncEnumerable<KeyValuePair<IGradingCriterion, IGradingResult>> ConvertToInterfaceResult<C, R>(IAsyncEnumerable<KeyValuePair<C, R>> rs)
            where C : IGradingCriterion
            where R : IGradingResult
        {
            await foreach (var item in rs)
                yield return new(item.Key, item.Value);
            yield break;
        }

        private Dictionary<Type, List<object>> ConnectCritToWorker(Dictionary<Type, object> dictWorkers)
        {
            Dictionary<Type, List<object>> dictCriteria = [];
            foreach (var criterion in _gradingCriteria)
            {
                var type = criterion.GetType();

                var searchWorker = dictWorkers.FirstOrDefault(x => x.Key.IsAssignableFrom(type)).Key??throw new Exception($"Не найден исполнитель оценки для следующего типа критерия {type.Name}");

                if (!dictCriteria.ContainsKey(type))
                    dictCriteria[searchWorker] = [];
                dictCriteria[searchWorker].Add(criterion);
            }
            return dictCriteria;
        }

        private Dictionary<Type, object> GetWorkerByCritType()
        {
            Dictionary<Type, object> dictWorkers = [];
            foreach (var worker in gradingWorkers)
            {
                var interfaces = worker.GetType().GetInterfaces();

                foreach (var iface in interfaces)
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IGradingWorker<,>))
                    {
                        var criterionType = iface.GetGenericArguments()[0];
                        if (!dictWorkers.ContainsKey(criterionType))
                            dictWorkers[criterionType] = worker;
                    }
                }
            }
            return dictWorkers;
        }

        public static object CastToTypedEnumerable(IEnumerable<object> raw, Type criterionType)
        {
            var castMethod = typeof(Enumerable)
                .GetMethod(nameof(Enumerable.Cast), BindingFlags.Static | BindingFlags.Public)!
                .MakeGenericMethod(criterionType)!;

            var result = castMethod.Invoke(null, [raw])!;
            return result;
        }

        private string ComputeHashBytes()
        {
            BigInteger combined = BigInteger.Zero;
            foreach (var item in _gradingCriteria.Concat(gradingWorkers))
            {
                if (item is IHasHash hashable)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(hashable.HashText);
                    BigInteger value = new(bytes);
                    combined ^= value;
                }
            }
            byte[] finalHash = System.Security.Cryptography.SHA256.HashData(combined.ToByteArray());
            return Convert.ToBase64String(finalHash);
        }
    }
}