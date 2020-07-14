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
                var f = sides[i];

                for (var j = 0; j < sides.Count; j++)
                {
                    if (j == i)
                        continue;

                    f.Polygon.Cut(sides[j].Plane.NormalFlipped());
                    if (f.Polygon.Count == 0)
                        break;
                }
            }

            int addVertex(Vertex v)
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

                    var (vertices, polysIndices) = side.Displacement!.Convert(side);
                    foreach (var polyIndices in polysIndices)
                        pi.Add(polyIndices.Select(idx => addVertex(vertices[idx])).ToList());
                }
            else
                foreach (var side in sides.Where(_ => _.Material != null))
                {
                    if (!PolygonIndicesByMaterial.TryGetValue(side.Material!, out var pi))
                        pi = PolygonIndicesByMaterial[side.Material!] = new List<List<int>>();

                    pi.Add(Enumerable.Range(0, side.Polygon.Count)
                        .Select(fi => addVertex(side.Polygon.Vertices[fi])).ToList());
                }

            var minX = double.PositiveInfinity;
            var minY = double.PositiveInfinity;
            var minZ = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            var maxY = double.NegativeInfinity;
            var maxZ = double.NegativeInfinity;

            foreach (var v in _vertices.Data.Select(kv => kv.Key.Co))
            {
                if (v.X < minX) minX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.Z < minZ) minZ = v.Z;
                if (v.X > maxX) maxX = v.X;
                if (v.Y > maxY) maxY = v.Y;
                if (v.Z > maxZ) maxZ = v.Z;
            }

            _bBoxMin = new Vector(minX, minY, minZ);
            _bBoxMax = new Vector(maxX, maxY, maxZ);
        }

        public int ID { get; }

        public Dictionary<VMT, List<List<int>>> PolygonIndicesByMaterial { get; } =
            new Dictionary<VMT, List<List<int>>>();

        public IEnumerable<Vertex> Vertices => _vertices.GetOrdered();
        public IEnumerable<Side> Sides => _sides;

        public bool Contains(Vector v, double margin = DecalComputation.Margin)
        {
            if (double.IsInfinity(_bBoxMin.X))
                return false;

            return v.X + margin >= _bBoxMin.X && v.X - margin <= _bBoxMax.X &&
                   v.Y + margin >= _bBoxMin.Y && v.Y - margin <= _bBoxMax.Y &&
                   v.Z + margin >= _bBoxMin.Z && v.Z - margin <= _bBoxMax.Z;
        }
    }
}
