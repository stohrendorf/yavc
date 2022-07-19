using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace geometry.components;

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

  public static Vector Zero => new();
  internal static Vector One => new(1, 1, 1);
  internal static Vector UnitX => new(1, 0, 0);
  internal static Vector UnitY => new(0, 1, 0);
  internal static Vector UnitZ => new(0, 0, 1);

  public override bool Equals(object? obj)
  {
    return obj is Vector other && Equals(other);
  }

  public override string ToString()
  {
    return $"({X} {Y} {Z})";
  }

  public double Length => Math.Sqrt(LengthSquared);

  internal double LengthSquared => Dot(this);

  public double Distance(Vector other)
  {
    return (other - this).Length;
  }

  internal Vector Normalized => this / Length;

  internal Vector Cross(Vector rhs)
  {
    return new Vector(
      Y * rhs.Z - Z * rhs.Y,
      Z * rhs.X - X * rhs.Z,
      X * rhs.Y - Y * rhs.X);
  }

  internal double Dot(Vector rhs)
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
    return new[] { X, Y, Z };
  }

  internal int MaxAxis()
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

  internal IEnumerable<Vector> StepsTo(Vector other, int n)
  {
    Debug.Assert(n > 0);
    var d = (other - this) / (n - 1);
    var self = this;
    return Enumerable.Range(0, n).Select(i => d * i + self);
  }

  internal bool FuzzyEquals(Vector other, double margin = 1e-4)
  {
    return Math.Abs(X - other.X) <= margin && Math.Abs(Y - other.Y) <= margin &&
           Math.Abs(Z - other.Z) <= margin;
  }

  public Vector2 AsVector2()
  {
    return new Vector2(X, Y);
  }
}