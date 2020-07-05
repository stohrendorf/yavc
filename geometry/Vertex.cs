using System;
using utility;

namespace geometry
{
    public class Vertex : IEquatable<Vertex>
    {
        private const int Rounding = 4;

        private Vector _co;
        private Vector2 _uv;

        public double Alpha;

        public Vertex(Vector co, Vector2 uv, double alpha)
        {
            Co = co;
            UV = uv;
            Alpha = alpha;
        }

        public Vector Co
        {
            get => _co;
            set => _co = new Vector(Round(value.X), Round(value.Y), Round(value.Z));
        }

        public Vector2 UV
        {
            get => _uv;
            set => _uv = new Vector2(Round(value.X), Round(value.Y));
        }

        public bool Equals(Vertex other)
        {
            return Co.Equals(other.Co) && UV.Equals(other.UV);
        }

        private static double Round(double value)
        {
            return Math.Round(value, Rounding, MidpointRounding.ToPositiveInfinity);
        }

        public override bool Equals(object? obj)
        {
            return obj is Vertex other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Co, UV);
        }

        public static bool operator ==(Vertex left, Vertex right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vertex left, Vertex right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"Co={_co} UV={_uv} Alpha={Alpha}";
        }
    }
}
