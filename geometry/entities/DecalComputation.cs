using System;
using System.Diagnostics;
using System.Linq;
using geometry.components;

namespace geometry.entities;

internal static class DecalComputation
{
  public const double Eps = 4;
  private static readonly double sin45Deg = 1.0 / Math.Sqrt(2);

  private static (Vector s, Vector t) ComputeDecalBasis(Vector n)
  {
    if (Math.Abs(n.Z) > sin45Deg)
    {
      var t = Vector.UnitX.Cross(n);
      var s = n.Cross(t);
      return (s.Normalized, t.Normalized);
    }
    else
    {
      var s = Vector.UnitZ.Cross(n);
      var t = s.Cross(n);
      return (s.Normalized, t.Normalized);
    }
  }

  private static Vertex ClampToUVEdge(Vertex a, Vertex b, Edge edge)
  {
    var dUv = b.UV - a.UV;
    var dCo = b.Co - a.Co;
    var dAlpha = b.Alpha - a.Alpha;

    var t = edge switch
    {
      Edge.Left => (0 - a.UV.X) / dUv.X,
      Edge.Right => (1 - a.UV.X) / dUv.X,
      Edge.Top => (0 - a.UV.Y) / dUv.Y,
      Edge.Bottom => (1 - a.UV.Y) / dUv.Y,
      _ => throw new ArgumentException("Invalid edge", nameof(edge)),
    };

    Debug.Assert(t is >= 0 and <= 1);

    return new Vertex(a.Co + dCo * t, a.UV + dUv * t, a.Alpha + dAlpha * t);
  }

  private static bool IsInsideUVRect(Vector2 uv, Edge edge)
  {
    return edge switch
    {
      Edge.Left => uv.X >= 0,
      Edge.Right => uv.X <= 1,
      Edge.Top => uv.Y >= 0,
      Edge.Bottom => uv.Y <= 1,
      _ => throw new ArgumentException("Invalid edge", nameof(edge)),
    };
  }

  private static VertexCollection ClampToUVRegion(VertexCollection vertices, Edge edge)
  {
    var result = new VertexCollection();
    if (vertices.Count == 0)
      return result;

    foreach (var (p0, p1) in vertices.CyclicPairs())
    {
      var p0Inside = IsInsideUVRect(p0.UV, edge);
      var p1Inside = IsInsideUVRect(p1.UV, edge);
      switch (p0Inside)
      {
        case false when !p1Inside:
          continue;
        case true:
          result.Add(p0);
          break;
      }

      if (p0Inside != p1Inside)
        result.Add(ClampToUVEdge(p0, p1, edge));
    }

    return result;
  }

  internal static Polygon? CreateClippedPoly(Decal decal, Side side)
  {
    var (s, t) = ComputeDecalBasis(side.Plane.Normal);
    var clipped = side.Polygon.Vertices.Select(vert => new Vertex(vert.Co,
      new Vector2(
        s.Dot(vert.Co - decal.Origin) / decal.Material.DecalWidth + 0.5,
        t.Dot(vert.Co - decal.Origin) / decal.Material.DecalHeight + 0.5
      ),
      vert.Alpha)).ToVertexCollection();

    foreach (var edge in typeof(Edge).GetEnumValues().Cast<Edge>())
    {
      clipped = ClampToUVRegion(clipped, edge);
      if (clipped.Count == 0) return null;
    }

    var poly = new Polygon();
    // add slight offset to hover above the surface
    var offset = side.Plane.Normal.Normalized * 0.1;
    foreach (var p in clipped.Select(vert =>
               new Vertex(vert.Co + offset, decal.Material.BaseTextureTransform.Apply(vert.UV), vert.Alpha)))
      poly.Add(p);
    return poly;
  }

  private enum Edge
  {
    Left,
    Right,
    Top,
    Bottom,
  }
}
