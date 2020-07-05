using System.Collections.Generic;

namespace utility
{
    public class IndexedSet<T> where T : notnull
    {
        private readonly Dictionary<T, int> _data = new Dictionary<T, int>();

        public int Add(T key)
        {
            if (_data.TryGetValue(key, out var idx))
                return idx;

            idx = _data.Count;
            _data.Add(key, idx);
            return idx;
        }

        public IEnumerable<T> GetOrdered()
        {
            var reversed = new SortedDictionary<int, T>();
            foreach (var (key, value) in _data) reversed.Add(value, key);

            return reversed.Values;
        }
    }
}
