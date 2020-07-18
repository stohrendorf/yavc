using System.Collections.Generic;

namespace geometry.components
{
    public static class VertexCollectionUtils
    {
        public static IEnumerable<Vertex> Cyclic(this VertexCollection vertexCollection)
        {
            if (vertexCollection.Count == 0)
                yield break;

            foreach (var element in vertexCollection) yield return element;

            yield return vertexCollection[0];
        }

        public static VertexCollection ToVertexCollection(this IEnumerable<Vertex> vertices)
        {
            return new VertexCollection(vertices);
        }
    }
}
