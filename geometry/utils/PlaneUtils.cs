using System;
using System.Diagnostics;
using System.Linq;
using geometry.components;
using geometry.entities;

namespace geometry.utils
{
    public static class PlaneUtils
    {
        public static Polygon ToPolygon(this Plane plane, Side side)
        {
            Vector n;
            switch (plane.Normal.MaxAxis())
            {
                case 0:
                case 1:
                    n = Vector.UnitZ;
                    break;
                case 2:
                    n = Vector.UnitX;
                    break;
                default:
                    throw new Exception();
            }

            // project n onto p.Normal, then subtract that from the normal, giving the first base vector of the plane
            Debug.Assert(Math.Abs(plane.Normal.LengthSquared - 1) < 1e-8);
            var a = (n - n.Dot(plane.Normal) * plane.Normal).Normalized * 16384;
            // form the second base vector
            var b = a.Cross(plane.Normal);

            var origin = plane.Normal * -plane.D;

            var polygon = new Polygon();
            polygon.Add(new Vertex(origin - b + a, Vector2.Zero, 1));
            polygon.Add(new Vertex(origin + b + a, Vector2.Zero, 1));
            polygon.Add(new Vertex(origin + b - a, Vector2.Zero, 1));
            polygon.Add(new Vertex(origin - b - a, Vector2.Zero, 1));

            Debug.Assert(polygon.Vertices.Co.All(_ => Math.Abs(plane.DistanceTo(_)) < 1e-3));

            for (var i = 0; i < 4; i++)
                polygon.Vertices.UV[i] = side.CalcUV(polygon.Vertices.Co[i], side.Material?.BaseTextureTransform);

            polygon.Vertices.NormalizeUV();

            return polygon;
        }
    }
}
