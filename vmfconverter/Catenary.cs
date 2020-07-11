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
            if (n < 2)
            {
                yield return b;
                yield break;
            }

            var d = (b - a) / (n - 1);
            for (var i = 0; i < n; ++i) yield return a + d * i;
        }

        public static IEnumerable<Vector> Calculate(Vector p0, Vector p1, double totalLength, int subdiv)
        {
            if (p0.X > p1.X)
                (p0, p1) = (p1, p0);

            // scale to unit cube to avoid numeric overflow in hyperbolic functions
            var scale = (p1 - p0).Abs().MaxValue;
            if (scale < MinStep)
                throw new ArgumentException($"Distance between points {p0} and {p1} too small");

            p0 /= scale;
            p1 /= scale;
            totalLength /= scale;

            if (totalLength <= p0.Distance(p1))
                // rope is stretched: straight line
            {
                foreach (var v in LinSpace(p0.X, p1.X, subdiv).Zip(LinSpace(p0.Y, p1.Y, subdiv))
                    .Zip(LinSpace(p0.Z, p1.Z, subdiv), ((double x, double y) xy, double z) => (xy.x, xy.y, z))
                    .Select(xyz => new Vector(xyz.x, xyz.y, xyz.z)))
                    yield return v * scale;
                yield break;
            }

            var dz = p1.Z - p0.Z;
            var horizontalDist = Math.Sqrt(Math.Pow(p1.X - p0.X, 2) + Math.Pow(p1.Y - p0.Y, 2));
            if (Math.Abs(horizontalDist) < VerticalThreshold) // almost perfectly vertical
            {
                var vSag = (totalLength - Math.Abs(dz)) / 2;
                var nSag = (int) Math.Ceiling(subdiv * vSag / totalLength);
                var zMax = Math.Max(p0.Z, p1.Z);
                var zMin = Math.Min(p0.Z, p1.Z);
                var zValues = LinSpace(zMax, zMin - vSag, subdiv - nSag).Concat(LinSpace(zMin - vSag, zMin, nSag));

                var xValues = Enumerable.Repeat((p0.X + p1.X) / 2, subdiv);
                var yValues = Enumerable.Repeat((p0.Y + p1.Y) / 2, subdiv);
                foreach (var v in xValues.Zip(yValues)
                    .Zip(zValues, ((double x, double y) xy, double z) => (xy.x, xy.y, z))
                    .Select(xyz => new Vector(xyz.x, xyz.y, xyz.z)))
                    yield return v * scale;

                yield break;
            }

            var stretchedHDist = Math.Sqrt(totalLength * totalLength - dz * dz);

            double G(double s)
            {
                var result = 2 * Math.Sinh(s * horizontalDist / 2) / s - stretchedHDist;
                Debug.Assert(!double.IsInfinity(result) && !double.IsNaN(result),
                    $"{nameof(s)}={s}, {nameof(horizontalDist)}={horizontalDist}");
                return result;
            }

            double Dg(double s)
            {
                return Math.Cosh(s * horizontalDist / 2) * horizontalDist / (2 * s) -
                       2 * Math.Sinh(s * horizontalDist / 2) / (s * s);
            }

            var sag = Math.Sqrt(totalLength * totalLength - dz * dz) / 2;
            for (var i = 0; i < MaxIter; ++i)
            {
                var g = G(sag);
                var dg = Dg(sag);

                if (Math.Abs(g) < MinG || Math.Abs(dg) < MinDg)
                    break;

                var search = -g / dg;
                Debug.Assert(!double.IsNaN(g) && !double.IsInfinity(g));
                Debug.Assert(!double.IsNaN(dg) && !double.IsInfinity(dg));

                double alpha = 1;
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

            var xLeft = 0.5 * (Math.Log((totalLength + dz) / (totalLength - dz)) / sag - horizontalDist);
            var xMin = p0.X - xLeft;
            var bias = p0.Z - Math.Cosh(xLeft * sag) / sag;
            foreach (var (x, y) in LinSpace(p0.X, p1.X, subdiv).Zip(LinSpace(p0.Y, p1.Y, subdiv), (x, y) => (x, y)))
            {
                var z = bias + Math.Cosh((x - xMin) * sag) / sag;
                yield return new Vector(x, y, z) * scale;
            }
        }
    }
}
