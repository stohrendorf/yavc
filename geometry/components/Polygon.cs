using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using geometry.utils;

namespace geometry.components;

public class Polygon
{
  public readonly VertexCollection Vertices = new();

  public int Count => Vertices.Count;

  internal IEnumerable<Plane> EdgePlanes
  {
    get
    {
      var cos = Vertices.Co.ToList();
      for (var i = 0; i < cos.Count; ++i)
      {
        var p1 = cos[i];
        var p2 = cos[(i + 1) % cos.Count];
        var p3 = cos[(i + 2) % cos.Count];
        var e1 = p1 - p2;
        var e2 = p3 - p2;
        var n = e1.Cross(e2);
        yield return Plane.CreateFromVertices(p1, p2, p1 + n);
      }
    }
  }

  public void Add(Vertex v)
  {
    Vertices.Add(v);
  }

  internal IEnumerable<Polygon> Split(Plane split)
  {
    if (Count == 0)
      yield break;

    var a = Cut(split);
    if (a.Count >= 3)
      yield return a;
    var b = Cut(split.NormalFlipped());
    if (b.Count >= 3)
      yield return b;
    Debug.Assert(a.Count >= 3 || b.Count >= 3);
  }

  public Polygon Cut(Plane split)
  {
    const double epsilon = 1e-6;

    var vertices = new List<Vertex>();

    void DoSplit(Vertex p1, Vertex p2)
    {
      var dot1 = split.DistanceTo(p1.Co);
      var dot2 = split.DistanceTo(p2.Co);
      switch (dot1)
      {
        case <= 0 - epsilon when dot2 < 0 - epsilon:
          // the edge is fully behind the plane
          return;
        case >= 0 - epsilon:
          // keep points in front of the plane
          vertices.Add(p1);
          break;
      }

      if (dot1 > 0 - epsilon && dot2 >= 0 - epsilon)
        // the edge is fully in front of the plane
        return;

      // plane: dot(p.normal, x) - p.d == 0
      // edge: x = e0 + lambda*eDir
      // -> dot(p.normal, e0 + lambda*eDir) - p.d == 0
      // -> dot(p.normal, e0 + lambda*eDir) == p.d
      // -> dot(p.normal, e0) + lambda*dot(p.normal, eDir) == p.d
      // -> lambda*dot(p.normal, eDir) == p.d - dot(p.normal, e0)
      // -> lambda == (p.d - dot(p.normal, e0)) / dot(p.normal, eDir)

      var d = p2.Co - p1.Co;
      var lambda = -dot1 / split.Normal.Dot(d);
      if (!double.IsFinite(lambda))
        return;

      // as both points are now on opposite sides of the plane, the intersection point must be on the edge
      if (lambda is < 0 - 1e-2 or > 1 + 1e-2)
        throw new Exception($"Lambda not on edge: p1=({p1}) p2=({p2}) lambda={lambda} split={split}");

      vertices.Add(new Vertex(p1.Co + lambda * d, p1.UV + lambda * (p2.UV - p1.UV),
        p1.Alpha + lambda * (p2.Alpha - p1.Alpha)));
    }

    foreach (var (first, second) in Vertices.CyclicPairs()) DoSplit(first, second);

    var result = new Polygon();
    if (vertices.Count == 0)
      return result;

    var prev = vertices[^1];
    foreach (var vertex in vertices)
    {
      if (prev.FuzzyEquals(vertex))
        continue;
      prev = vertex;
      result.Add(vertex);
    }

    result.Vertices.NormalizeUV();
    return result;
  }
}
