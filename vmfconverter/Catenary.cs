using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using utility;

namespace VMFConverter
{
    public static class Catenary
    {
        private const int MaxIter = 1000;
        private const double VerticalThreshold = 1e-6;

        private static IEnumerable<double> LinSpace(double a, double b, int n)
        {
            if (n < 2) return Enumerable.Repeat(b, 1);

            var d = (b - a) / (n - 1);
            return Enumerable.Range(0, n).Select(i => a + d * i);
        }

        private static IEnumerable<Vector2> Calculate2D(double dx, double dy, double length, int subdivisions)
        {
            // https://math.stackexchange.com/questions/3557767/how-to-construct-a-catenary-of-a-specified-length-through-two-specified-points
            if (dx <= 0)
                throw new ArgumentException();
            if (dx < 1e-8)
                throw new ArgumentException();
            if (length <= 0 || length * length < dx * dx + dy * dy)
                throw new ArgumentException();

            var r = Math.Sqrt(length * length - dy * dy) / dx;
            Debug.Assert(r > 1, $"r={r} dx={dx} dy={dy} length={length}");

            var aN = r < 3 ? Math.Sqrt(6 * (r - 1)) : Math.Log(2 * r) + Math.Log(Math.Log(2 * r));
            for (var i = 0; i < MaxIter; ++i)
            {
                var xx = r * aN - Math.Sinh(aN);
                if (Math.Abs(xx / aN) <= 1e-15)
                    break;

                if (double.IsInfinity(aN) || double.IsNaN(aN))
                    throw new ArithmeticException("aN calculation gave NaN");
                aN += xx / (Math.Cosh(aN) - r);
            }

            var a = dx / (2 * aN);
            var b = dx / 2 - a * Math.Atanh(dy / length);
            var c = dy / 2 - length / (2 * Math.Tanh(aN));

            double f(double x)
            {
                var result = a * Math.Cosh((x - b) / a) + c;
                Debug.Assert(!double.IsNaN(result), $"x={x} aN={aN} a={a} b={b} c={c} dx={dx} dy={dy}");
                return result;
            }

            return LinSpace(0, 1, subdivisions).Select(x => new Vector2(x, f(x * dx)));
        }

        public static IEnumerable<Vector> Calculate(Vector p0, Vector p1, double additionalLength, int subdivisions)
        {
            if (p0.Distance(p1) < 1e-8)
                throw new ArgumentException($"Distance between points {p0} and {p1} too small");
            if (subdivisions < 0)
                throw new ArgumentException("subdivisions must be >= 0", nameof(subdivisions));

            var d = p1 - p0;
            var dLength = d.Length;
            var totalLength = dLength + additionalLength;
            var hDist = Math.Sqrt(d.X * d.X + d.Y * d.Y);

            if (dLength >= totalLength)
                // rope is stretched: straight line
                return new[] {p0, p1};

            if (hDist >= VerticalThreshold)
                return Calculate2D(hDist, p1.Z - p0.Z, totalLength, subdivisions).Select(xy => new Vector(
                    p0.X + xy.X * d.X,
                    p0.Y + xy.X * d.Y,
                    p0.Z + xy.Y
                ));

            var dz = Math.Abs(p1.Z - p0.Z);
            var overhang = totalLength - 2 * dz;
            var minZ = Math.Min(p0.Z, p1.Z);
            return new[] {p0, new Vector(p0.X, p0.Y, minZ - overhang), p1};
        }
    }
}
