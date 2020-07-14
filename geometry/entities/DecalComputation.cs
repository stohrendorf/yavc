using System;
using System.Collections.Generic;
using System.Linq;
using geometry.components;

namespace geometry.entities
{
    internal static class DecalComputation
    {
        public const double Margin = 4;
        private static readonly double deg45 = Math.Sin(45 * Math.PI / 180);

        private static Vector[] ComputeDecalBasis(Vector n)
        {
            Vector[] result = new Vector[2];
            if (Math.Abs(n.Z) > deg45)
            {
                result[1] = Vector.UnitX.Cross(n).Normalized;
                result[0] = n.Cross(result[1]).Normalized;
            }
            else
            {
                result[0] = n.Cross(-Vector.UnitZ).Normalized;
                result[1] = result[0].Cross(n).Normalized;
            }

            return result;
        }

        private static Vertex Intersect(Vertex a, Vertex b, int edge)
        {
            var dUv = b.UV - a.UV;
            var dCo = b.Co - a.Co;
            var dAlpha = b.Alpha - a.Alpha;

            var t = edge switch
            {
                0 => (a.UV.X - 0) / dUv.X,
                1 => (a.UV.X - 1) / dUv.X,
                2 => (a.UV.Y - 0) / dUv.Y,
                3 => (a.UV.Y - 1) / dUv.Y,
                _ => throw new ArgumentException("Invalid edge", nameof(edge))
            };

            return new Vertex(a.Co - dCo * t, a.UV - dUv * t, a.Alpha - dAlpha * t);
        }

        private static bool Inside(Vertex vert, int edge)
        {
            return edge switch
            {
                0 => vert.UV.X > 0.0,
                1 => vert.UV.X < 1.0,
                2 => vert.UV.Y > 0.0,
                3 => vert.UV.Y < 1.0,
                _ => false
            };
        }

        private static List<Vertex> ShClip(IReadOnlyList<Vertex> verts, int edge)
        {
            if (verts.Count == 0)
                return new List<Vertex>();

            var p0 = verts[^1];
            var result = new List<Vertex>();
            foreach (var p1 in verts)
            {
                if (Inside(p1, edge))
                {
                    if (!Inside(p0, edge))
                        result.Add(Intersect(p0, p1, edge));
                    result.Add(p1);
                }
                else
                {
                    if (Inside(p0, edge))
                        result.Add(Intersect(p1, p0, edge));
                }

                p0 = p1;
            }

            return result;
        }

        internal static Polygon? CreateClippedPoly(Decal decal, Face face)
        {
            var textureSpaceBasis = ComputeDecalBasis(face.Plane.Normal);

            var u0 = textureSpaceBasis[0].Dot(decal.Origin) - decal.Material.DecalWidth / 2.0;
            var v0 = textureSpaceBasis[1].Dot(decal.Origin) - decal.Material.DecalHeight / 2.0;
            var clipped = face.Polygon.Vertices.Select(vert =>
            {
                var u = textureSpaceBasis[0].Dot(vert.Co) - u0;
                var v = textureSpaceBasis[1].Dot(vert.Co) - v0;
                return new Vertex(vert.Co,
                    new Vector2(u / decal.Material.DecalWidth, v / decal.Material.DecalHeight),
                    vert.Alpha);
            }).ToList();

            for (var i = 0; i < 4; ++i)
            {
                clipped = ShClip(clipped, i);
                if (clipped.Count == 0) return null;
            }

            var poly = new Polygon();
            // fix UV coordinates and add slight offset to hover above the surface
            foreach (var p in clipped.Select(vert =>
                new Vertex(vert.Co + face.Plane.Normal * 0.1, new Vector2(vert.UV.X, 1 - vert.UV.Y), vert.Alpha)))
                poly.Add(p);
            return poly;
        }
    }
}
