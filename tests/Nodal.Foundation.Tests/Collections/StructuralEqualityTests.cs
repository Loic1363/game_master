using System.Collections.Immutable;
using Nodal.Foundation.Collections;
using Xunit;

namespace Nodal.Foundation.Tests.Collections;

public class StructuralEqualityTests
{
    [Fact]
    public void ArraysWithEqualItemsInOrderAreEqual()
    {
        ImmutableArray<int> left = [1, 2, 3];
        ImmutableArray<int> right = [1, 2, 3];

        Assert.True(StructuralEquality.ItemsEqual(left, right));
        Assert.Equal(StructuralEquality.ItemsHash(left), StructuralEquality.ItemsHash(right));
    }

    [Fact]
    public void ArraysDifferingInOrderLengthOrItemsAreNotEqual()
    {
        ImmutableArray<int> items = [1, 2, 3];

        Assert.False(StructuralEquality.ItemsEqual(items, [3, 2, 1]));
        Assert.False(StructuralEquality.ItemsEqual(items, [1, 2]));
        Assert.False(StructuralEquality.ItemsEqual(items, [1, 2, 4]));
    }

    [Fact]
    public void DefaultArraysAreEqualToEachOtherAndToNothingElse()
    {
        var uninitialized = default(ImmutableArray<int>);

        Assert.True(StructuralEquality.ItemsEqual(uninitialized, default));
        Assert.False(StructuralEquality.ItemsEqual(uninitialized, []));
        Assert.False(StructuralEquality.ItemsEqual([], uninitialized));
        Assert.Equal(0, StructuralEquality.ItemsHash(uninitialized));
    }

    [Fact]
    public void EmptyArraysAreEqual()
    {
        Assert.True(StructuralEquality.ItemsEqual(ImmutableArray<int>.Empty, []));
    }

    [Fact]
    public void DictionariesWithEqualEntriesAreEqual()
    {
        var left = Dictionary(("a", 1), ("b", 2));
        var right = Dictionary(("b", 2), ("a", 1));

        Assert.True(StructuralEquality.EntriesEqual(left, right));
        Assert.Equal(StructuralEquality.EntriesHash(left), StructuralEquality.EntriesHash(right));
    }

    [Fact]
    public void DictionariesDifferingInKeysValuesOrCountAreNotEqual()
    {
        var entries = Dictionary(("a", 1), ("b", 2));

        Assert.False(StructuralEquality.EntriesEqual(entries, Dictionary(("a", 1), ("b", 3))));
        Assert.False(StructuralEquality.EntriesEqual(entries, Dictionary(("a", 1), ("c", 2))));
        Assert.False(StructuralEquality.EntriesEqual(entries, Dictionary(("a", 1))));
    }

    [Fact]
    public void NullDictionariesAreEqualToEachOtherAndToNothingElse()
    {
        Assert.True(StructuralEquality.EntriesEqual<string, int>(null, null));
        Assert.False(StructuralEquality.EntriesEqual(null, Dictionary(("a", 1))));
        Assert.False(StructuralEquality.EntriesEqual(Dictionary(("a", 1)), null));
        Assert.Equal(0, StructuralEquality.EntriesHash<string, int>(null));
    }

    private static ImmutableSortedDictionary<string, int> Dictionary(
        params (string Key, int Value)[] entries) =>
        entries.Aggregate(
            ImmutableSortedDictionary.Create<string, int>(StringComparer.Ordinal),
            (dictionary, entry) => dictionary.Add(entry.Key, entry.Value));
}
