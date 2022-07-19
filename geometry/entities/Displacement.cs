using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using geometry.components;
using geometry.utils;

namespace geometry.entities;

public class Displacement
{
  public readonly List<List<double>> Alphas = new();
  public readonly List<List<double>> Distances = new();
  public readonly List<List<Vector>> Normals = new();
  public readonly List<List<Vector>> OffsetNormals = new();
  public readonly List<List<Vector>> Offsets = new();
  private IList<Polygon>? _flatPolygons;

  private IList<Polygon>? _polygons;
  private Side? _side;
  public double Elevation;
  public int Power;
  public Vector StartPosition;

  internal IEnumerable<Polygon> DisplacedPolygons
  {
    get
    {
      Debug.Assert(_polygons != null);
      return _polygons;
    }
  }

  internal IEnumerable<Polygon> FlatPolygons
  {
    get
    {
      Debug.Assert(_flatPolygons != null);
      return _flatPolygons;
    }
  }

  public int Dimension => 1 << Power;

  internal IEnumerable<Plane> EdgePlanes
  {
    get
    {
      Debug.Assert(_side != null);
      int[] vertexWindingIndices = { -1, -1, -1, -1 };
      var bestDistance = double.PositiveInfinity;
      for (var i = 0; i < _side.Polygon.Count; i++)
      {
        var distance = _side.Polygon.Vertices.Co[i].Distance(StartPosition);

        if (distance >= bestDistance)
          continue;

        vertexWindingIndices[0] = i;
        vertexWindingIndices[1] = (i + 1) % 4;
        vertexWindingIndices[2] = (i + 2) % 4;
        vertexWindingIndices[3] = (i + 3) % 4;
        bestDistance = distance;
      }

      var size = Dimension + 1;

      var cos = vertexWindingIndices.Select(idx => _side.Polygon.Vertices.Co[idx]).ToArray();
      var steps01 = cos[0].StepsTo(cos[1], size).ToArray();
      var steps03 = cos[0].StepsTo(cos[3], size).ToArray();
      var steps12 = cos[1].StepsTo(cos[2], size).ToArray();
      var steps32 = cos[3].StepsTo(cos[2], size).ToArray();

      var edge01 = cos[1] - cos[0];
      var edge03 = cos[3] - cos[0];

      for (var s = 0; s < size; s++)
      {
        var a = steps32[s];
        var b = steps01[s];
        var ab = (b - a).Cross(edge01);
        Debug.Assert(ab.Length >= 1);
        yield return Plane.CreateFromVertices(a, b, ab);

        a = steps03[s];
        b = steps12[s];
        ab = (b - a).Cross(edge03);
        Debug.Assert(ab.Length >= 1);
        yield return Plane.CreateFromVertices(a, b, ab);
      }
    }
  }

  internal IEnumerable<Polygon> Convert(Side side)
  {
    if (side.Polygon.Count != 4)
      throw new ArgumentException($"Expected polygon with 4 vertices, got {side.Polygon.Count}");

    if (_side != null)
    {
      Debug.Assert(_polygons != null);
      if (_side != side)
        throw new ArgumentException();
      return _polygons;
    }

    _side = side;

    var size = Dimension + 1;

    int[] vertexWindingIndices = { -1, -1, -1, -1 };
    var bestDistance = double.PositiveInfinity;
    for (var i = 0; i < side.Polygon.Count; i++)
    {
      var distance = side.Polygon.Vertices.Co[i].Distance(StartPosition);

      if (distance >= bestDistance)
        continue;

      vertexWindingIndices[0] = i;
      vertexWindingIndices[1] = (i + 1) % 4;
      vertexWindingIndices[2] = (i + 2) % 4;
      vertexWindingIndices[3] = (i + 3) % 4;
      bestDistance = distance;
    }

    if (vertexWindingIndices[0] == -1)
      throw new Exception("Failed to determine starting vertex index");

    var cos = vertexWindingIndices.Select(idx => _side.Polygon.Vertices.Co[idx]).ToArray();
    var steps32 = cos[3].StepsTo(cos[2], size).ToArray();
    var uvSteps32 = side.CalcUV(cos[3]).StepsTo(side.CalcUV(cos[2]), size)
      .Select(uv => side.Material?.BaseTextureTransform.Apply(uv) ?? uv).ToArray();
    var steps01 = cos[0].StepsTo(cos[1], size).ToArray();
    var uvSteps01 = side.CalcUV(cos[0]).StepsTo(side.CalcUV(cos[1]), size)
      .Select(uv => side.Material?.BaseTextureTransform.Apply(uv) ?? uv).ToArray();

    var vertices = new VertexCollection();
    var flatVertices = new VertexCollection();
    for (var s = 0; s < size; s++)
    {
      vertices.AddRange(steps01[s].StepsTo(steps32[s], size).Zip(uvSteps01[s].StepsTo(uvSteps32[s], size),
        static (co, uv) => new Vertex(co, uv, 1)));
      flatVertices.AddRange(steps01[s].StepsTo(steps32[s], size).Zip(uvSteps01[s].StepsTo(uvSteps32[s], size),
        static (co, uv) => new Vertex(co, uv, 1)));
    }

    Debug.Assert(vertices.Count == size * size);

    for (var i = 0; i < size; i++)
    for (var j = 0; j < size; j++)
    {
      var normal = Normals[i][j] * Distances[i][j];
      Vector offset;
      if (Offsets[i].Count == 0 || OffsetNormals[i].Count == 0)
        offset = Vector.Zero;
      else
        offset = Offsets[i][j] + OffsetNormals[i][j];

      vertices.Co[i * size + j] += side.Plane.Normal * Elevation + normal + offset;
    }

    for (var i = 0; i < size; i++)
    for (var j = 0; j < size; j++)
    {
      vertices.Alpha[i * size + j] = Alphas[i][j];
      flatVertices.Alpha[i * size + j] = Alphas[i][j];
    }

    vertices.NormalizeUV();

    _polygons = new List<Polygon>();
    _flatPolygons = new List<Polygon>();
    for (var i = 0; i < Dimension; i++)
    for (var j = 0; j < Dimension; j++)
    {
      var idx0 = i * size + j;
      var p = new Polygon();
      p.Add(vertices[idx0]);
      p.Add(vertices[idx0 + 1]);
      p.Add(vertices[idx0 + size + 1]);
      p.Add(vertices[idx0 + size]);
      _polygons.Add(p);

      p = new Polygon();
      p.Add(flatVertices[idx0]);
      p.Add(flatVertices[idx0 + 1]);
      p.Add(flatVertices[idx0 + size + 1]);
      p.Add(flatVertices[idx0 + size]);
      _flatPolygons.Add(p);
    }

    return _polygons;
  }
}
