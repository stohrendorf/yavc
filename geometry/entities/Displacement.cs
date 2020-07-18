using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using geometry.components;
using geometry.utils;

namespace geometry.entities
{
    public class Displacement
    {
        public readonly List<List<double>> Alphas = new List<List<double>>();
        public readonly List<List<double>> Distances = new List<List<double>>();
        public readonly List<List<Vector>> Normals = new List<List<Vector>>();
        public readonly List<List<Vector>> OffsetNormals = new List<List<Vector>>();
        public readonly List<List<Vector>> Offsets = new List<List<Vector>>();
        private IList<Polygon>? _flatPolygons;

        private IList<Polygon>? _polygons;
        private Side? _side;
        public double Elevation;
        public int Power;
        public Vector StartPosition;

        public IEnumerable<Polygon> DisplacedPolygons
        {
            get
            {
                Debug.Assert(_polygons != null);
                return _polygons;
            }
        }

        public IEnumerable<Polygon> FlatPolygons
        {
            get
            {
                Debug.Assert(_flatPolygons != null);
                return _flatPolygons;
            }
        }

        public int Dimension => 1 << Power;

        public IEnumerable<Plane> EdgePlanes
        {
            get
            {
                Debug.Assert(_side != null);
                int[] vertexWindingIndices = {-1, -1, -1, -1};
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

                var stepsA = _side.Polygon.Vertices.Co[vertexWindingIndices[3]]
                    .StepsTo(_side.Polygon.Vertices.Co[vertexWindingIndices[2]], size).ToList();
                var stepsB = _side.Polygon.Vertices.Co[vertexWindingIndices[0]]
                    .StepsTo(_side.Polygon.Vertices.Co[vertexWindingIndices[1]], size).ToList();

                var stepsC = stepsB[0].StepsTo(stepsA[0], size).ToArray();
                var stepsD = stepsB[^1].StepsTo(stepsA[^1], size).ToArray();

                var edgeA = _side.Polygon.Vertices.Co[vertexWindingIndices[2]] -
                            _side.Polygon.Vertices.Co[vertexWindingIndices[3]];
                var edgeB = _side.Polygon.Vertices.Co[vertexWindingIndices[1]] -
                            _side.Polygon.Vertices.Co[vertexWindingIndices[0]];
                for (var s = 0; s < size; s++)
                {
                    var a = stepsA[s];
                    var b = stepsB[s];
                    var c = (b - a).Cross(edgeA);
                    yield return Plane.CreateFromVertices(a, b, c);

                    a = stepsC[s];
                    b = stepsD[s];
                    c = (b - a).Cross(edgeB);
                    yield return Plane.CreateFromVertices(a, b, c);
                }
            }
        }

        public IEnumerable<Polygon> Convert(Side side)
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

            int[] vertexWindingIndices = {-1, -1, -1, -1};
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

            var stepsA = side.Polygon.Vertices.Co[vertexWindingIndices[3]]
                .StepsTo(side.Polygon.Vertices.Co[vertexWindingIndices[2]], size).ToList();

            var uvStepsA = side.CalcUV(side.Polygon.Vertices.Co[vertexWindingIndices[3]])
                .StepsTo(side.CalcUV(side.Polygon.Vertices.Co[vertexWindingIndices[2]]), size).ToList();

            var stepsB = side.Polygon.Vertices.Co[vertexWindingIndices[0]]
                .StepsTo(side.Polygon.Vertices.Co[vertexWindingIndices[1]], size).ToList();

            var uvStepsB = side.CalcUV(side.Polygon.Vertices.Co[vertexWindingIndices[0]])
                .StepsTo(side.CalcUV(side.Polygon.Vertices.Co[vertexWindingIndices[1]]), size).ToList();

            var vertices = new VertexCollection();
            var flatVertices = new VertexCollection();
            for (var s = 0; s < size; s++)
            {
                vertices.AddRange(stepsB[s].StepsTo(stepsA[s], size).Zip(uvStepsB[s].StepsTo(uvStepsA[s], size),
                    (co, uv) => new Vertex(co, uv, 1)));
                flatVertices.AddRange(stepsB[s].StepsTo(stepsA[s], size).Zip(uvStepsB[s].StepsTo(uvStepsA[s], size),
                    (co, uv) => new Vertex(co, uv, 1)));
            }

            Debug.Assert(vertices.Count == size * size);

            for (var i = 0; i < size; i++)
            for (var j = 0; j < size; j++)
            {
                var normal = Normals[i][j] * Distances[i][j];
                var offset = Offsets[i][j] + OffsetNormals[i][j];

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
}
