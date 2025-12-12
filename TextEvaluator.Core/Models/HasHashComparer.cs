using TextEvaluator.Core.Interfaces;

namespace TextEvaluator.Core.Models
{
    public class HasHashComparer<T> : IEqualityComparer<T> where T : IHasHash
    {
        public bool Equals(T? x, T? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.HashText == y.HashText;
        }

        public int GetHashCode(T obj)
        {
            return obj.HashText.GetHashCode();
        }
    }
}
