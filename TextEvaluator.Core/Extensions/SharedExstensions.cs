using System.Security.Cryptography;
using System.Text;
using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.Core.Extensions
{
    public static class SharedExstensions
    {
        public static string GetHashText(this string text) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        public static void LogInfo(this ILogging? logging, string message, params object?[] args) => logging?.Log(LogTypes.Info, message, args);
        public static void LogWarning(this ILogging? logging, string message, params object?[] args) => logging?.Log(LogTypes.Warning, message, args);
        public static void LogError(this ILogging? logging, string message, params object?[] args) => logging?.Log(LogTypes.Error, message, args);
        public static void LogFatal(this ILogging? logging, string message, params object?[] args) => logging?.Log(LogTypes.Fatal, message, args);
    }
}