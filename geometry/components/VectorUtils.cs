using System;
using System.Collections.Generic;

namespace geometry.components
{
    public static class VectorUtils
    {
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
    }
}
