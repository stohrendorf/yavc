using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace geometry.components;

internal sealed class VertexAlphaAccessor(IList<Vertex> vertices) : IEnumerable<Vector>
{
  public double this[int i]
  {
    set => vertices[i].Alpha = value;
  }

  [MustDisposeResource]
  public IEnumerator<Vector> GetEnumerator()
  {
    return vertices.Select(static v => v.Co).GetEnumerator();
  }

  [MustDisposeResource]
  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
}
