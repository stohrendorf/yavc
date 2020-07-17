using System.Collections.Generic;

namespace utility
{
    public static class LinqUtils
    {
        public static IEnumerable<T> Cyclic<T>(this IList<T> list)
        {
            if (list.Count == 0)
                yield break;

            foreach (var element in list) yield return element;

            yield return list[0];
        }

        public static IEnumerable<(T First, T Second)> Pairs<T>(this IList<T> enumerable)
        {
            if (enumerable.Count < 2)
                yield break;

            for (var i = 0; i < enumerable.Count - 1; ++i) yield return (enumerable[i], enumerable[i + 1]);
        }
    }
}
