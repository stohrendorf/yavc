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

            var minX = vertices.Select(_ => _.UV.X).Min();
            minX = Math.Floor(minX);
            var minY = vertices.Select(_ => _.UV.Y).Min();
            minY = Math.Floor(minY);

            foreach (var vertex in vertices) vertex.UV = new Vector2(vertex.UV.X - minX, vertex.UV.Y - minY);
        }
    }
}
