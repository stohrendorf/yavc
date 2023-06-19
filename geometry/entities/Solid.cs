using System.Collections.Generic;
using System.Linq;
using geometry.components;
using geometry.materials;
using utility;

namespace geometry.entities;

public sealed class Solid
{
  private readonly Vector _bBoxMax;
  private readonly Vector _bBoxMin;
  private readonly IReadOnlyList<Side> _sides;
  private readonly IndexedSet<Vertex> _vertices = new();

  public Solid(int id, IReadOnlyList<Side> sides)
  {
    ID = id;
    _sides = sides;

    for (var i = 0; i < sides.Count; i++)
    {
      var side = sides[i];

      for (var j = 0; j < sides.Count; j++)
      {
        if (j == i)
        {
          continue;
        }

        side.Polygon = side.Polygon.Cut(sides[j].Plane.NormalFlipped());
        if (side.Polygon.Count == 0)
        {
          break;
        }
      }
    }

    int AddVertex(Vertex v)
    {
      var existing = _vertices.Data.Where(x => x.Key.FuzzyEquals(v)).Select(static x => (int?)x.Value)
        .FirstOrDefault();
      return existing ?? _vertices.Add(v);
    }

    if (sides.Any(static side => side.Displacement is not null))
    {
      foreach (var side in sides.Where(static side => side.Displacement is not null && side.Material != null))
      {
        if (!PolygonIndicesByMaterial.TryGetValue(side.Material!, out var pi))
        {
          pi = PolygonIndicesByMaterial[side.Material!] = new List<List<int>>();
        }

        var polygons = side.Displacement!.Convert(side);
        pi.AddRange(polygons.Select(polygon => polygon.Vertices.Select(AddVertex).ToList()));
      }
    }
    else
    {
      foreach (var side in sides.Where(static side => side.Material is not null))
      {
        if (!PolygonIndicesByMaterial.TryGetValue(side.Material!, out var pi))
        {
          pi = PolygonIndicesByMaterial[side.Material!] = new List<List<int>>();
        }

        pi.Add(Enumerable.Range(0, side.Polygon.Count)
          .Select(fi => AddVertex(side.Polygon.Vertices[fi])).ToList());
      }
    }

    (_bBoxMin, _bBoxMax) = _vertices.Data.Select(static kv => kv.Key.Co).GetBBox();
  }

  public int ID { get; }

  public Dictionary<VMT, List<List<int>>> PolygonIndicesByMaterial { get; } = new();

  public IEnumerable<Vertex> Vertices => _vertices.GetOrdered();
  public IEnumerable<Side> Sides => _sides;

  public bool Contains(Vector v, double eps = DecalComputation.Eps)
  {
    return !double.IsInfinity(_bBoxMin.X) && v.In(_bBoxMin, _bBoxMax, eps);
  }
}
