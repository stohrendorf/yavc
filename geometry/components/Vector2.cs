using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace geometry.components
{
    public readonly struct Vector2 : IEquatable<Vector2>
    {
        public readonly double X;
        public readonly double Y;

        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static readonly Vector2 Zero = new Vector2(0, 0);
        public static readonly Vector2 One = new Vector2(1, 1);

        public override bool Equals(object? obj)
        {
            return obj is Vector2 other && Equals(other);
        }

        public override string ToString()
        {
            return $"({X} {Y})";
        }

        public static Vector2 operator +(in Vector2 left, in Vector2 right)
        {
            return new Vector2(left.X + right.X, left.Y + right.Y);
        }

        public static Vector2 operator -(in Vector2 left, in Vector2 right)
        {
            return new Vector2(left.X - right.X, left.Y - right.Y);
        }

        public static Vector2 operator *(in Vector2 left, in Vector2 right)
        {
            return new Vector2(left.X * right.X, left.Y * right.Y);
        }

        public static Vector2 operator *(in Vector2 left, in double right)
        {
            return new Vector2(left.X * right, left.Y * right);
        }

        public static Vector2 operator *(in double left, in Vector2 right)
        {
            return right * left;
        }

        public static Vector2 operator /(in Vector2 left, in Vector2 right)
        {
            return new Vector2(left.X / right.X, left.Y / right.Y);
        }

        public static Vector2 operator /(in Vector2 left, in double right)
        {
            return new Vector2(left.X / right, left.Y / right);
        }

        public static Vector2 operator -(in Vector2 left)
        {
            return new Vector2(-left.X, -left.Y);
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public static bool operator ==(in Vector2 left, in Vector2 right)
        {
            return left.X == right.X &&
                   left.Y == right.Y;
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public static bool operator !=(in Vector2 left, in Vector2 right)
        {
            return left.X != right.X ||
                   left.Y != right.Y;
        }

        public bool Equals(Vector2 other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public IEnumerable<Vector2> StepsTo(Vector2 other, int n)
        {
            Debug.Assert(n > 0);
            var d = (other - this) / (n - 1);
            var self = this;
            return Enumerable.Range(0, n).Select(i => d * i + self);
        }

        public double Distance(Vector2 other)
        {
            return (other - this).Length;
        }

        public double Length => Math.Sqrt(LengthSquared);

        public double LengthSquared => Dot(this);

        public double Dot(Vector2 rhs)
        {
            return X * rhs.X + Y * rhs.Y;
        }

        public bool FuzzyEquals(Vector2 other, double margin = 1e-4)
        {
            return Math.Abs(X - other.X) <= margin && Math.Abs(Y - other.Y) <= margin;
        }
    }
}
