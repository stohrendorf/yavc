using System;
using System.Globalization;
using System.Text.RegularExpressions;
using geometry.components;

namespace geometry.utils;

public static partial class ParserUtil
{
    public static Plane ParseToPlane(this string data)
    {
        var match = GetPlanePattern().Match(data);
        if (!match.Success)
        {
            throw new ArgumentException($"Invalid plane string: {data}");
        }

        var plane = new Vector[3];
        for (var i = 0; i < 3; ++i)
        {
            plane[i] = new Vector(
                ParseToDouble(match.Groups[3 * i + 1].Value),
                ParseToDouble(match.Groups[3 * i + 2].Value),
                ParseToDouble(match.Groups[3 * i + 3].Value)
            );
        }

        return Plane.CreateFromVertices(plane[1], plane[0], plane[2]);
    }

    public static TextureAxis ParseToTextureAxis(this string data)
    {
        var match = GetAxisPattern().Match(data);
        if (!match.Success)
        {
            throw new ArgumentException($"Invalid texture axis string: {data}");
        }

        var axis = new Vector(
            ParseToDouble(match.Groups[1].Value),
            ParseToDouble(match.Groups[2].Value),
            ParseToDouble(match.Groups[3].Value));
        var shift = ParseToDouble(match.Groups[4].Value);
        var scale = ParseToDouble(match.Groups[5].Value);
        if (Math.Abs(scale) < 1e-6)
        {
            scale = 0.25;
        }

        return new TextureAxis(axis, shift, scale);
    }

    public static double ParseToDouble(this string value)
    {
        return double.Parse(value,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture);
    }

    public static int ParseToInt(this string value)
    {
        return int.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
    }

    public static Vector ParseToVector(this string data)
    {
        var cols = data.Split(" ");
        if (cols.Length != 3)
        {
            throw new ArgumentException($"Invalid amount of vector values (expected 3, found {cols.Length}",
                nameof(data));
        }

        return new Vector(
            ParseToDouble(cols[0]),
            ParseToDouble(cols[1]),
            ParseToDouble(cols[2])
        );
    }

    internal static Vector2 ParseToVector2(this string data)
    {
        var cols = data.Split(" ");
        if (cols.Length != 2)
        {
            throw new ArgumentException($"Invalid amount of vector values (expected 2, found {cols.Length}",
                nameof(data));
        }

        return new Vector2(
            ParseToDouble(cols[0]),
            ParseToDouble(cols[1])
        );
    }

    [GeneratedRegex("^\\[([^ ]+) ([^ ]+) ([^ ]+) ([^ ]+)] ([^ ]+)$", RegexOptions.Compiled)]
    private static partial Regex GetAxisPattern();

    [GeneratedRegex(@"^\(([^ ]+) ([^ ]+) ([^ ]+)\) \(([^ ]+) ([^ ]+) ([^ ]+)\) \(([^ ]+) ([^ ]+) ([^ ]+)\)$",
        RegexOptions.Compiled)]
    private static partial Regex GetPlanePattern();
}
