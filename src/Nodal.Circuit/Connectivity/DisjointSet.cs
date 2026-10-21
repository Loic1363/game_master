namespace Nodal.Circuit.Connectivity;

internal sealed class DisjointSet
{
    private readonly int[] _parent;
    private readonly int[] _size;

    public DisjointSet(int count)
    {
        _parent = new int[count];
        _size = new int[count];

        for (var index = 0; index < count; index++)
        {
            _parent[index] = index;
            _size[index] = 1;
        }
    }

    public int Find(int index)
    {
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
}
