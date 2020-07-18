using System;
using System.Diagnostics.CodeAnalysis;

namespace geometry.components
{
    public class Vertex : IEquatable<Vertex>
    {
        public double Alpha;
        public Vector Co;
        public Vector2 UV;

        public Vertex(Vector co, Vector2 uv, double alpha)
        {
            Co = co;
            UV = uv;
            Alpha = alpha;
        }

        public bool Equals(Vertex? other)
        {
            return other != null && Co.Equals(other.Co) && UV.Equals(other.UV);
        }

        public override bool Equals(object? obj)
        {
            return obj is Vertex other && Equals(other);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            return HashCode.Combine(Co, UV);
        }

        public static bool operator ==(Vertex left, Vertex right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vertex? left, Vertex? right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return $"Co={Co} UV={UV} Alpha={Alpha}";
        }

        public bool FuzzyEquals(Vertex other, double margin = 1e-4)
        {
            return Co.FuzzyEquals(other.Co, margin) &&
                   UV.FuzzyEquals(other.UV, margin) &&
                   Math.Abs(Alpha - other.Alpha) < margin;
        }
    }
}
