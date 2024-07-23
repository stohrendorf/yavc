using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace geometry.components;

public sealed class VertexUVAccessor : IEnumerable<Vector2>
{
    private readonly IList<Vertex> _vertices;

    internal VertexUVAccessor(IList<Vertex> vertices)
    {
        _vertices = vertices;
    }

    internal Vector2 this[int i]
    {
        get => _vertices[i].UV;
        set => _vertices[i].UV = value;
    }

    [MustDisposeResource]
    public IEnumerator<Vector2> GetEnumerator()
    {
        return _vertices.Select(static v => v.UV).GetEnumerator();
    }

    [MustDisposeResource]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}