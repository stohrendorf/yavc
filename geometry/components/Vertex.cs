using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace geometry.components
{
    public class Vertex : IEquatable<Vertex>
    {
        public const int UVChannels = 4;

        public double Alpha;

        public Vector Co;
        public Vector2[] UVs;

        public Vertex(Vector co, Vector2 uv, double alpha)
        {
            Co = co;
            UVs = new[] {uv, Vector2.Zero, Vector2.Zero, Vector2.Zero};
            Alpha = alpha;

            Debug.Assert(UVs.Length == UVChannels);
        }

        public Vector2 UV0
        {
            get => UVs[0];
            set => UVs[0] = value;
        }

        public bool Equals(Vertex? other)
        {
            return other != null && Co.Equals(other.Co) && UVs.SequenceEqual(other.UVs);
        }

        public override bool Equals(object? obj)
        {
            return obj is Vertex other && Equals(other);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            return HashCode.Combine(Co, UVs);
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
            return $"Co={Co} UV={string.Join('|', UVs)} Alpha={Alpha}";
        }

        public bool FuzzyEquals(Vertex other, double margin = 1e-4)
        {
            return Co.FuzzyEquals(other.Co, margin) &&
                   UVs.Zip(other.UVs).All(ab => ab.First.FuzzyEquals(ab.Second, margin)) &&
                   Math.Abs(Alpha - other.Alpha) < margin;
        }
    }
}
