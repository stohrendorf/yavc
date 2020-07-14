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
        public double Elevation;
        public int Power;
        public Vector StartPosition;

        private int Dimension => 1 << Power;

        public (VertexCollection vertices, List<List<int>> indices) Convert(Side side)
        {
            if (side.Polygon.Count != 4)
                throw new ArgumentException($"Expected polygon with 4 vertices, got {side.Polygon.Count}");

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

            var baseA = side.Polygon.Vertices.Co[vertexWindingIndices[3]]
                .StepsTo(side.Polygon.Vertices.Co[vertexWindingIndices[2]], size).ToList();

            var uvBaseA = side.CalcUV(side.Polygon.Vertices.Co[vertexWindingIndices[3]])
                .StepsTo(side.CalcUV(side.Polygon.Vertices.Co[vertexWindingIndices[2]]), size).ToList();

            var baseB = side.Polygon.Vertices.Co[vertexWindingIndices[0]]
                .StepsTo(side.Polygon.Vertices.Co[vertexWindingIndices[1]], size).ToList();

            var uvBaseB = side.CalcUV(side.Polygon.Vertices.Co[vertexWindingIndices[0]])
                .StepsTo(side.CalcUV(side.Polygon.Vertices.Co[vertexWindingIndices[1]]), size).ToList();

            var vertices = new VertexCollection();
            for (var s = 0; s < size; s++)
                vertices.AddRange(baseB[s].StepsTo(baseA[s], size).Zip(uvBaseB[s].StepsTo(uvBaseA[s], size),
                    (co, uv) => new Vertex(co, uv, 1)));

            Debug.Assert(vertices.Count == size * size);

            for (var i = 0; i < size; i++)
            for (var j = 0; j < size; j++)
            {
                var normal = Normals[i][j] * Distances[i][j];
                var offset = Offsets[i][j] + OffsetNormals[i][j];

                vertices[i * size + j].Co += side.Plane.Normal * Elevation + normal + offset;
            }

            for (var i = 0; i < size; i++)
            for (var j = 0; j < size; j++)
                vertices[i * size + j].Alpha = Alphas[i][j];

            vertices.NormalizeUV();

            var indices = new List<List<int>>();

            for (var i = 0; i < Dimension; i++)
            for (var j = 0; j < Dimension; j++)
            {
                var idx0 = i * size + j;
                indices.Add(new List<int>
                {
                    idx0, idx0 + 1, idx0 + size + 1, idx0 + size
                });
            }

            Debug.Assert(indices.Count == Dimension * Dimension);
            Debug.Assert(indices.SelectMany(_ => _).All(idx => idx < size * size));

            return (vertices, indices);
        }
    }
}
