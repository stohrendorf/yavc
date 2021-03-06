using System.Collections.Generic;
using System.Linq;
using geometry.components;
using geometry.materials;
using utility;

namespace geometry.entities
{
    public class Solid
    {
        private readonly Vector _bBoxMax;
        private readonly Vector _bBoxMin;
        private readonly IReadOnlyList<Side> _sides;
        private readonly IndexedSet<Vertex> _vertices = new IndexedSet<Vertex>();

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
                        continue;

                    side.Polygon = side.Polygon.Cut(sides[j].Plane.NormalFlipped());
                    if (side.Polygon.Count == 0)
                        break;
                }
            }

            int AddVertex(Vertex v)
            {
                var existing = _vertices.Data.Where(x => x.Key.FuzzyEquals(v)).Select(x => (int?) x.Value)
                    .FirstOrDefault();
                return existing ?? _vertices.Add(v);
            }

            if (sides.Any(_ => _.Displacement != null))
                foreach (var side in sides.Where(_ => _.Displacement != null && _.Material != null))
                {
                    if (!PolygonIndicesByMaterial.TryGetValue(side.Material!, out var pi))
                        pi = PolygonIndicesByMaterial[side.Material!] = new List<List<int>>();

                    var polygons = side.Displacement!.Convert(side);
                    foreach (var polygon in polygons)
                        pi.Add(polygon.Vertices.Select(AddVertex).ToList());
                }
            else
                foreach (var side in sides.Where(_ => _.Material != null))
                {
                    if (!PolygonIndicesByMaterial.TryGetValue(side.Material!, out var pi))
                        pi = PolygonIndicesByMaterial[side.Material!] = new List<List<int>>();

                    pi.Add(Enumerable.Range(0, side.Polygon.Count)
                        .Select(fi => AddVertex(side.Polygon.Vertices[fi])).ToList());
                }

            (_bBoxMin, _bBoxMax) = _vertices.Data.Select(kv => kv.Key.Co).GetBBox();
        }

        public int ID { get; }

        public Dictionary<VMT, List<List<int>>> PolygonIndicesByMaterial { get; } =
            new Dictionary<VMT, List<List<int>>>();

        public IEnumerable<Vertex> Vertices => _vertices.GetOrdered();
        public IEnumerable<Side> Sides => _sides;

        public bool Contains(Vector v, double eps = DecalComputation.Eps)
        {
            return !double.IsInfinity(_bBoxMin.X) && v.In(_bBoxMin, _bBoxMax, eps);
        }
    }
}
