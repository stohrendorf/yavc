using System.Collections.Generic;

namespace utility;

public sealed class IndexedSet<T> where T : notnull
{
    private readonly Dictionary<T, int> _data = new();

    public IEnumerable<KeyValuePair<T, int>> Data => _data;

    public int Add(T key)
    {
        if (_data.TryGetValue(key, out var idx))
        {
            return idx;
        }

        idx = _data.Count;
        _data.Add(key, idx);
        return idx;
    }

    public IEnumerable<T> GetOrdered()
    {
        var reversed = new SortedDictionary<int, T>();
        foreach (var (key, value) in _data)
        {
            reversed.Add(value, key);
        }

        return reversed.Values;
    }
}