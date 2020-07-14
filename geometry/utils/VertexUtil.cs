using System;
using System.Linq;
using geometry.components;

namespace geometry.utils
{
    public static class VertexUtil
    {
        public static void NormalizeUV(this VertexCollection vertices)
        {
            if (vertices.Count == 0)
                return;

            var minX = vertices.Select(_ => _.UV0.X).Min();
            minX = Math.Floor(minX);
            var minY = vertices.Select(_ => _.UV0.Y).Min();
            minY = Math.Floor(minY);

            foreach (var vertex in vertices) vertex.UV0 = new Vector2(vertex.UV0.X - minX, vertex.UV0.Y - minY);
        }
    }
}
