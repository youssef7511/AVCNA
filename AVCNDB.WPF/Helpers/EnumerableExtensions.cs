namespace AVCNDB.WPF.Helpers;

public static class EnumerableExtensions
{
    /// <summary>
    /// Like <c>ToDictionary</c> but tolerant of duplicate keys: the first occurrence wins
    /// and later duplicates are ignored, so unexpected duplicate data (e.g. two medics
    /// sharing a PCT code) never crashes with "An item with the same key has already been
    /// added". Entries with a null key are skipped.
    /// </summary>
    public static Dictionary<TKey, TSource> ToFirstWinsDictionary<TSource, TKey>(
        this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        where TKey : notnull
    {
        var dict = new Dictionary<TKey, TSource>();
        foreach (var item in source)
        {
            var key = keySelector(item);
            if (key is not null)
                dict.TryAdd(key, item);
        }
        return dict;
    }
}
