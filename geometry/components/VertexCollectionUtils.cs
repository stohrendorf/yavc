using System.Collections.Generic;

namespace geometry.components;

internal static class VertexCollectionUtils
{
  public static IEnumerable<(Vertex a, Vertex b)> CyclicPairs(this VertexCollection vertexCollection)
  {
    if (vertexCollection.Count == 0)
    {
      yield break;
    }

    var prev = vertexCollection[^1];
    foreach (var element in vertexCollection)
    {
      yield return (prev, element);
      prev = element;
    }
  }

  public static VertexCollection ToVertexCollection(this IEnumerable<Vertex> vertices)
  {
    return new VertexCollection(vertices);
  }
}
