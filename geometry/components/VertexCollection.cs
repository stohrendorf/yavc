using System.Collections;
using System.Collections.Generic;

namespace geometry.components;

public sealed class VertexCollection : IEnumerable<Vertex>
{
    private readonly List<Vertex> _vertices = [];

    internal readonly VertexAlphaAccessor Alpha;
    public readonly VertexCoAccessor Co;
    public readonly VertexUVAccessor UV;

    internal VertexCollection(IEnumerable<Vertex> vertices) : this()
    {
        AddRange(vertices);
    }

    internal VertexCollection()
    {
        Co = new VertexCoAccessor(_vertices);
        Alpha = new VertexAlphaAccessor(_vertices);
        UV = new VertexUVAccessor(_vertices);
    }

    public int Count => _vertices.Count;

    internal Vertex this[int idx] => _vertices[idx];

    public IEnumerator<Vertex> GetEnumerator()
    {
        return _vertices.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    internal void AddRange(IEnumerable<Vertex> vertices)
    {
        _vertices.AddRange(vertices);
    }

    internal void Add(Vertex vertex)
    {
        _vertices.Add(vertex);
    }
}