using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace geometry.components
{
    public static class VectorUtils
    {
        public const double Epsilon = 1e-4;

        public static Vector? RayPlaneIntersection(Vector orig, Vector dir, Plane plane)
        {
            var nDotDir = plane.Normal.Dot(dir);
            if (Math.Abs(nDotDir) < 1e-6)
                return null;

            var lambda = (plane.Origin - orig).Dot(plane.Normal) / nDotDir;
            return orig + lambda * dir;
        }

        public static (Vector min, Vector max) GetBBox(this IEnumerable<Vector> vs)
        {
            var minX = double.PositiveInfinity;
            var minY = double.PositiveInfinity;
            var minZ = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            var maxY = double.NegativeInfinity;
            var maxZ = double.NegativeInfinity;

            foreach (var v in vs)
            {
                if (v.X < minX) minX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.Z < minZ) minZ = v.Z;
                if (v.X > maxX) maxX = v.X;
                if (v.Y > maxY) maxY = v.Y;
                if (v.Z > maxZ) maxZ = v.Z;
            }

            return (new Vector(minX, minY, minZ), new Vector(maxX, maxY, maxZ));
        }

        public static bool In(this Vector v, Vector bbMin, Vector bbMax, double eps)
        {
            return v.X + eps >= bbMin.X && v.X - eps <= bbMax.X &&
                   v.Y + eps >= bbMin.Y && v.Y - eps <= bbMax.Y &&
                   v.Z + eps >= bbMin.Z && v.Z - eps <= bbMax.Z;
        }

        public static bool CalcBarycentric(Vector p, Vector a, Vector b, Vector c, out double s, out double t,
            out double u)
        {
            Debug.Assert(Math.Abs(Plane.CreateFromVertices(a, b, c).DistanceTo(p)) < Epsilon);
            s = t = u = double.NaN;

            var v0 = b - a;
            var v1 = c - a;
            var v2 = p - a;
            var d00 = v0.Dot(v0);
            var d01 = v0.Dot(v1);
            var d11 = v1.Dot(v1);
            var d20 = v2.Dot(v0);
            var d21 = v2.Dot(v1);
            var denom = d00 * d11 - d01 * d01;
            t = (d11 * d20 - d01 * d21) / denom;
            if (t < 0 - Epsilon || t > 1 + Epsilon)
                return false;
            u = (d00 * d21 - d01 * d20) / denom;
            if (u < 0 - Epsilon || u > 1 + Epsilon)
                return false;
            s = 1 - t - u;
            if (s < 0 - Epsilon || s > 1 + Epsilon)
                return false;
            return true;
        }
    }
}
