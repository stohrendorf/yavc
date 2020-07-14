using System;
using geometry.utils;

namespace geometry.components
{
    public class Polygon
    {
        public readonly VertexCollection Vertices = new VertexCollection();

        public int Count => Vertices.Count;

        public void Add(Vertex v)
        {
            Vertices.Add(v);
        }

        public void Cut(Plane split)
        {
            var result = new Polygon();

            void doSplit(Vertex p1, Vertex p2)
            {
                var dot1 = split.Dot(p1.Co);
                var dot2 = split.Dot(p2.Co);
                if (dot1 < 0 && dot2 < 0)
                    // the edge is fully behind the plane
                    return;

                if (dot1 >= 0)
                    // keep points in front of the plane
                    result.Add(p1);

                if (dot1 > 0 && dot2 >= 0)
                    // the edge is fully in front of the plane
                    return;

                // plane: dot(p.normal, x) - p.d == 0
                // edge: x = e0 + lambda*eDir
                // -> dot(p.normal, e0 + lambda*eDir) - p.d == 0
                // -> dot(p.normal, e0 + lambda*eDir) == p.d
                // -> dot(p.normal, e0) + lambda*dot(p.normal, eDir) == p.d
                // -> lambda*dot(p.normal, eDir) == p.d - dot(p.normal, e0)
                // -> lambda == (p.d - dot(p.normal, e0)) / dot(p.normal, eDir)

                var d = p2.Co - p1.Co;
                var lambda = -dot1 / split.Normal.Dot(d);
                if (double.IsNaN(lambda))
                    return;

                // as both points are now on opposite sides of the plane, the intersection point must be on the edge
                if (lambda < 0 - 1e-6 || lambda > 1 + 1e-6)
                    throw new Exception($"Lambda not on edge: p1=({p1}) p2=({p2}) lambda={lambda} split={split}");

                var uv = p2.UV0 - p1.UV0;
                result.Add(new Vertex(p1.Co + lambda * d, p1.UV0 + lambda * uv, 1));
            }

            for (var i = 0; i < Count; i++)
            {
                var i2 = (i + 1) % Count;
                doSplit(Vertices[i], Vertices[i2]);
            }

            Vertices.Clear();
            if (result.Vertices.Count == 0)
                return;

            Vertex prev = result.Vertices[^1];
            foreach (var vertex in result.Vertices)
            {
                if (prev.FuzzyEquals(vertex))
                    continue;
                prev = vertex;
                Vertices.Add(vertex);
            }

            Vertices.NormalizeUV();
        }
    }
}
