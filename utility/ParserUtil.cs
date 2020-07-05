using System;
using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace utility
{
    public static class ParserUtil
    {
        private static readonly Regex
            axisPattern = new Regex("^\\[([^ ]+) ([^ ]+) ([^ ]+) ([^ ]+)] ([^ ]+)$", RegexOptions.Compiled);

        private static readonly Regex
            planePattern = new Regex(
                @"^\(([^ ]+) ([^ ]+) ([^ ]+)\) \(([^ ]+) ([^ ]+) ([^ ]+)\) \(([^ ]+) ([^ ]+) ([^ ]+)\)$",
                RegexOptions.Compiled);

        public static Plane ParsePlaneString(string data)
        {
            var match = planePattern.Match(data);
            if (!match.Success)
                throw new ArgumentException($"Invalid plane string: {data}");

            var plane = new Vector[3];
            for (var i = 0; i < 3; ++i)
                plane[i] = new Vector(
                    ParseDouble(match.Groups[3 * i + 1].Value),
                    ParseDouble(match.Groups[3 * i + 2].Value),
                    ParseDouble(match.Groups[3 * i + 3].Value)
                );

            return Plane.CreateFromVertices(plane[1], plane[0], plane[2]);
        }

        public static (Vector axis, double shift) ParseTextureAxis(string data)
        {
            var match = axisPattern.Match(data);
            if (!match.Success)
                throw new ArgumentException($"Invalid texture axis string: {data}");
            var axis = new Vector(
                ParseDouble(match.Groups[1].Value),
                ParseDouble(match.Groups[2].Value),
                ParseDouble(match.Groups[3].Value));
            var shift = ParseDouble(match.Groups[4].Value);
            var scale = ParseDouble(match.Groups[5].Value);
            if (Math.Abs(scale) < 1e-6)
                scale = 0.25;

            return (axis / scale, shift);
        }

        public static double ParseDouble(string value)
        {
            return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }

    [TestFixture]
    public static class ParserUtilTest
    {
        [Test]
        public static void TestDeterministicParsing()
        {
            var p1 = ParserUtil.ParsePlaneString("(0 1 0) (0 0 0) (0 0 1)");
            var p2 = ParserUtil.ParsePlaneString("(0 10 0) (0 0 0) (0 0 1)");
            var p3 = ParserUtil.ParsePlaneString("(0 1 0) (0 0 0) (0 0 10)");
            Assert.That(p1, Is.EqualTo(p2));
            Assert.That(p1, Is.EqualTo(p3));
        }
    }
}
