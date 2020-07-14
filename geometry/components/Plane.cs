using System;
using System.Diagnostics.CodeAnalysis;

namespace geometry.components
{
    public readonly struct Plane : IEquatable<Plane>
    {
        public readonly Vector Normal;
        public readonly double D;

        public Plane(Vector normal, double d)
        {
            Normal = normal;
            D = d;
        }

        public static Plane CreateFromVertices(Vector origin, Vector p1, Vector p2)
        {
            var n = (p1 - origin).Cross(p2 - origin).Normalized;
            var d = -n.Dot(origin);
            return new Plane(
                n, d
            );
        }

        public double DotCoordinate(Vector value)
        {
            return Normal.Dot(value) + D;
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public static bool operator ==(Plane value1, Plane value2)
        {
            return value1.Normal == value2.Normal && value1.D == value2.D;
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public static bool operator !=(Plane value1, Plane value2)
        {
            return value1.Normal != value2.Normal || value1.D != value2.D;
        }

        public bool Equals(Plane other)
        {
            return this == other;
        }

        public override bool Equals(object? obj)
        {
            return obj is Plane plane && Equals(plane);
        }

        public override string ToString()
        {
            return $"(normal:{Normal} d:{D})";
        }

        public override int GetHashCode()
        {
            return Normal.GetHashCode() + D.GetHashCode();
        }

        public Plane NormalFlipped()
        {
            return new Plane(-Normal, -D);
        }
    }
}
