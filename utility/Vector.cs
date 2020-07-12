using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace utility
{
    public readonly struct Vector : IEquatable<Vector>
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        public Vector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector Zero => new Vector();
        public static Vector One => new Vector(1, 1, 1);
        public static Vector UnitX => new Vector(1, 0, 0);
        public static Vector UnitY => new Vector(0, 1, 0);
        public static Vector UnitZ => new Vector(0, 0, 1);

        public override bool Equals(object? obj)
        {
            return obj is Vector other && Equals(other);
        }

        public override string ToString()
        {
            return $"({X} {Y} {Z})";
        }

        public double Length => Math.Sqrt(LengthSquared);

        public double LengthSquared => Dot(this);

        public double Distance(Vector other)
        {
            return (other - this).Length;
        }

        public Vector Normalized => this / Length;

        public Vector Cross(Vector rhs)
        {
            return new Vector(
                Y * rhs.Z - Z * rhs.Y,
                Z * rhs.X - X * rhs.Z,
                X * rhs.Y - Y * rhs.X);
        }

        public double Dot(Vector rhs)
        {
            return X * rhs.X + Y * rhs.Y + Z * rhs.Z;
        }

        public Vector Abs()
        {
            return new Vector(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
        }

        public static Vector operator +(in Vector left, in Vector right)
        {
            return new Vector(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public static Vector operator -(in Vector left, in Vector right)
        {
            return new Vector(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        public static Vector operator *(in Vector left, in Vector right)
        {
            return new Vector(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
        }

        public static Vector operator *(in Vector left, in double right)
        {
            return new Vector(left.X * right, left.Y * right, left.Z * right);
        }

        public static Vector operator *(in double left, in Vector right)
        {
            return right * left;
        }

        public static Vector operator /(in Vector left, in Vector right)
        {
            return new Vector(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
        }

        public static Vector operator /(in Vector left, in double right)
        {
            return new Vector(left.X / right, left.Y / right, left.Z / right);
        }

        public static Vector operator -(in Vector left)
        {
            return new Vector(-left.X, -left.Y, -left.Z);
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public static bool operator ==(in Vector left, in Vector right)
        {
            return left.X == right.X &&
                   left.Y == right.Y &&
                   left.Z == right.Z;
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public static bool operator !=(in Vector left, in Vector right)
        {
            return left.X != right.X ||
                   left.Y != right.Y ||
                   left.Z != right.Z;
        }

        public bool Equals(Vector other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public double[] ToArray()
        {
            return new[] {X, Y, Z};
        }

        public double MaxValue => Math.Max(X, Math.Max(Y, Z));

        public int MaxAxis()
        {
            var data = Abs().ToArray();
            var maxI = -1;
            var max = double.NegativeInfinity;
            for (var i = 0; i < data.Length; i++)
            {
                if (data[i] <= max)
                    continue;

                maxI = i;
                max = data[i];
            }

            if (maxI == -1) throw new Exception();

            return maxI;
        }

        public IEnumerable<Vector> StepsTo(Vector other, int n)
        {
            Debug.Assert(n > 0);
            var d = (other - this) / (n - 1);
            var self = this;
            return Enumerable.Range(0, n).Select(i => d * i + self);
        }
    }

    public readonly struct Vector2 : IEquatable<Vector2>
    {
        public readonly double X;
        public readonly double Y;

        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 Zero => new Vector2();

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
    }
}
