namespace Nodal.Foundation.Collections;

public sealed class DisjointSet
{
    private readonly int[] _parent;
    private readonly int[] _size;

    public DisjointSet(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count), count, "A disjoint set cannot hold a negative number of elements.");
        }

        _parent = new int[count];
        _size = new int[count];

        for (var index = 0; index < count; index++)
        {
            _parent[index] = index;
            _size[index] = 1;
        }
    }

    public int Count => _parent.Length;

    public int Find(int index)
    {
        RequireMember(index);

        while (_parent[index] != index)
        {
            _parent[index] = _parent[_parent[index]];
            index = _parent[index];
        }

        return index;
    }

    public void Union(int left, int right)
    {
        var leftRoot = Find(left);
        var rightRoot = Find(right);

        if (leftRoot == rightRoot)
        {
            return;
        }

        if (_size[leftRoot] < _size[rightRoot])
        {
            (leftRoot, rightRoot) = (rightRoot, leftRoot);
        }

        _parent[rightRoot] = leftRoot;
        _size[leftRoot] += _size[rightRoot];
    }

    public bool AreJoined(int left, int right) => Find(left) == Find(right);

    private void RequireMember(int index)
    {
        if (index < 0 || index >= _parent.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index), index, $"A disjoint set of {_parent.Length} elements has no such element.");
        }
    }
}
