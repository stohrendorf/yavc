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
        private const double MinDg = 1e-10;
        private const double MinG = 1e-8;
        private const double StepFactor = 0.5;
        private const double MinStep = 1e-9;
        private const double VerticalThreshold = 1e-3;

        private static IEnumerable<double> LinSpace(double a, double b, int n)
        {
            if (n < 2) return Enumerable.Repeat(b, 1);

            var d = (b - a) / (n - 1);
            return Enumerable.Range(0, n).Select(i => a + d * i);
        }

        private static IEnumerable<Vector2> Calculate2DNorm(double dy, int subdiv)
        {
            if (Math.Abs(dy) >= 1)
                // rope is stretched: straight line
                return new[] {new Vector2(0, 0), new Vector2(1, dy)};

            var stretchedHDist = Math.Sqrt(1 - dy * dy);

            double G(double s)
            {
                var result = 2 * Math.Sinh(s / 2) / s - stretchedHDist;
                Debug.Assert(!double.IsInfinity(result) && !double.IsNaN(result), $"{nameof(s)}={s}");
                return result;
            }

            static double Dg(double s)
            {
                var result = Math.Cosh(s / 2) / (2 * s) - 2 * Math.Sinh(s / 2) / (s * s);
                Debug.Assert(!double.IsInfinity(result) && !double.IsNaN(result), $"{nameof(s)}={s}");
                return result;
            }

            double sag = 1;
            for (var i = 0; i < MaxIter; ++i)
            {
                var g = G(sag);
                var dg = Dg(sag);

                if (Math.Abs(g) < MinG || Math.Abs(dg) < MinDg)
                    break;

                var search = -g / dg;
                var alpha = 1.0;
                var sagNew = sag + alpha * search;
                while (sagNew < 0 || Math.Abs(G(sagNew)) > Math.Abs(g))
                {
                    alpha *= StepFactor;
                    if (alpha < MinStep)
                        break;
                    sagNew = sag + alpha * search;
                }

                sag = sagNew;
            }

            var xLeft = 0.5 * ((Math.Log(1 + dy) - Math.Log(1 - dy)) / sag - 1);
            var bias = -Math.Cosh(xLeft * sag) / sag;

            double CalcY(double x)
            {
                return bias + Math.Cosh((x + xLeft) * sag) / sag;
            }

            var endY = CalcY(1);

            // sinh and cosh are so incredibly unstable, we fix Y calculation errors by scaling the curve into 0..1 range
            var correction = Math.Abs(endY) < MinStep ? 1.0 : dy / endY;
            return LinSpace(0, 1, subdiv).Select(x => new Vector2(x, CalcY(x) * correction));
        }

        public static IEnumerable<Vector> Calculate(Vector p0, Vector p1, double totalLength, int subdiv)
        {
            if (p0.Distance(p1) < MinStep)
                throw new ArgumentException($"Distance between points {p0} and {p1} too small");

            var d = p1 - p0;
            if (Math.Sqrt(d.X * d.X + d.Y * d.Y) >= VerticalThreshold)
                return Calculate2DNorm(d.Z / totalLength, subdiv).Select(xy => new Vector(
                    xy.X * d.X + p0.X,
                    xy.X * d.Y + p0.Y,
                    xy.Y * totalLength + p0.Z
                ));

            if (totalLength < Math.Abs(p1.Z - p0.Z))
                // stretched
                return new[] {p0, p1};

            var dz = Math.Abs(p1.Z - p0.Z);
            var overhang = totalLength - 2 * dz;
            var minZ = Math.Min(p0.Z, p1.Z);
            return new[] {p0, new Vector(p0.X, p0.Y, minZ - overhang), p1};
        }
    }
}
