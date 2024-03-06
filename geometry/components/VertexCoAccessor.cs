using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace geometry.components;

public sealed class VertexCoAccessor : IEnumerable<Vector>
{
    private readonly IList<Vertex> _vertices;

    internal VertexCoAccessor(IList<Vertex> vertices)
    {
        _vertices = vertices;
    }

    public Vector this[int i]
    {
        get => _vertices[i].Co;
        set => _vertices[i].Co = value;
    }

    public IEnumerator<Vector> GetEnumerator()
    {
        return _vertices.Select(static v => v.Co).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}