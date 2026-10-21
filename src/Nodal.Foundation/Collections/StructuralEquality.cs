using System.Collections.Immutable;

namespace Nodal.Foundation.Collections;

public static class StructuralEquality
{
    public static bool ItemsEqual<TItem>(ImmutableArray<TItem> left, ImmutableArray<TItem> right)
    {
        if (left.IsDefault || right.IsDefault)
        {
            return left.IsDefault && right.IsDefault;
        }

        return left.Length == right.Length && left.SequenceEqual(right);
    }

    public static int ItemsHash<TItem>(ImmutableArray<TItem> items)
    {
        if (items.IsDefault)
        {
            return 0;
        }

        var hash = new HashCode();
        hash.Add(items.Length);

        foreach (var item in items)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }

    public static bool EntriesEqual<TKey, TValue>(
        ImmutableSortedDictionary<TKey, TValue>? left, ImmutableSortedDictionary<TKey, TValue>? right)
        where TKey : notnull
    {
        if (left is null || right is null)
        {
            return ReferenceEquals(left, right);
        }

        return left.Count == right.Count && left.SequenceEqual(right);
    }

    public static int EntriesHash<TKey, TValue>(ImmutableSortedDictionary<TKey, TValue>? entries)
        where TKey : notnull
    {
        if (entries is null)
        {
            return 0;
        }

        var hash = new HashCode();
        hash.Add(entries.Count);

        foreach (var entry in entries)
        {
            hash.Add(entry.Key);
            hash.Add(entry.Value);
        }

        return hash.ToHashCode();
    }
}
