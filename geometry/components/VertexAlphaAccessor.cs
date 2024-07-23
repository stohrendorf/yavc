using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace geometry.components;

internal sealed class VertexAlphaAccessor(IList<Vertex> _vertices) : IEnumerable<Vector>
{
    public double this[int i]
    {
        get => _vertices[i].Alpha;
        set => _vertices[i].Alpha = value;
    }

    [MustDisposeResource]
    public IEnumerator<Vector> GetEnumerator()
    {
        return _vertices.Select(static v => v.Co).GetEnumerator();
    }

    [MustDisposeResource]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}