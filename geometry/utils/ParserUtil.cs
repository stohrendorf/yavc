using System;
using System.Globalization;
using System.Text.RegularExpressions;
using geometry.components;

namespace geometry.utils;

public static class ParserUtil
{
  private static readonly Regex
    axisPattern = new("^\\[([^ ]+) ([^ ]+) ([^ ]+) ([^ ]+)] ([^ ]+)$", RegexOptions.Compiled);

  private static readonly Regex
    planePattern = new(
      @"^\(([^ ]+) ([^ ]+) ([^ ]+)\) \(([^ ]+) ([^ ]+) ([^ ]+)\) \(([^ ]+) ([^ ]+) ([^ ]+)\)$",
      RegexOptions.Compiled);

  public static Plane ParsePlaneString(this string data)
  {
    var match = planePattern.Match(data);
    if (!match.Success)
    {
      throw new ArgumentException($"Invalid plane string: {data}");
    }

    var plane = new Vector[3];
    for (var i = 0; i < 3; ++i)
    {
      plane[i] = new Vector(
        ParseDouble(match.Groups[3 * i + 1].Value),
        ParseDouble(match.Groups[3 * i + 2].Value),
        ParseDouble(match.Groups[3 * i + 3].Value)
      );
    }

    return Plane.CreateFromVertices(plane[1], plane[0], plane[2]);
  }

  public static TextureAxis ParseTextureAxis(this string data)
  {
    var match = axisPattern.Match(data);
    if (!match.Success)
    {
      throw new ArgumentException($"Invalid texture axis string: {data}");
    }

    var axis = new Vector(
      ParseDouble(match.Groups[1].Value),
      ParseDouble(match.Groups[2].Value),
      ParseDouble(match.Groups[3].Value));
    var shift = ParseDouble(match.Groups[4].Value);
    var scale = ParseDouble(match.Groups[5].Value);
    if (Math.Abs(scale) < 1e-6)
    {
      scale = 0.25;
    }

    return new TextureAxis(axis, shift, scale);
  }

  public static double ParseDouble(this string value)
  {
    return double.Parse(value,
      NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint,
      CultureInfo.InvariantCulture);
  }

  public static int ParseInt(this string value)
  {
    return int.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
  }

  public static Vector ParseVector(this string data)
  {
    var cols = data.Split(" ");
    if (cols.Length != 3)
    {
      throw new ArgumentException($"Invalid amount of vector values (expected 3, found {cols.Length}", nameof(data));
    }

    return new Vector(
      ParseDouble(cols[0]),
      ParseDouble(cols[1]),
      ParseDouble(cols[2])
    );
  }

  internal static Vector2 ParseVector2(this string data)
  {
    var cols = data.Split(" ");
    if (cols.Length != 2)
    {
      throw new ArgumentException($"Invalid amount of vector values (expected 2, found {cols.Length}", nameof(data));
    }

    return new Vector2(
      ParseDouble(cols[0]),
      ParseDouble(cols[1])
    );
  }
}
