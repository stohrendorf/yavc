using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace geometry.components;

internal class VertexAlphaAccessor : IEnumerable<Vector>
{
  private readonly IList<Vertex> _vertices;

  public VertexAlphaAccessor(IList<Vertex> vertices)
  {
    _vertices = vertices;
  }

  public double this[int i]
  {
    get => _vertices[i].Alpha;
    set => _vertices[i].Alpha = value;
  }

  public IEnumerator<Vector> GetEnumerator()
  {
    return _vertices.Select(static _ => _.Co).GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
}