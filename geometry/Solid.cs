using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using utility;

namespace geometry
{
    public class Solid
    {
        private readonly Vector _bBoxMax;
        private readonly Vector _bBoxMin;
        private readonly IReadOnlyList<Face> _faces;
        private readonly IndexedSet<Vertex> _vertices = new IndexedSet<Vertex>();

        public Solid(int id, IReadOnlyList<Face> faces)
        {
            ID = id;
            _faces = faces;

            for (var i = 0; i < faces.Count; i++)
            {
                var f = faces[i];

                for (var j = 0; j < faces.Count; j++)
                {
                    if (j == i)
                        continue;

                    f.Polygon.Cut(faces[j].Plane.NormalFlipped());
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

            if (faces.Any(_ => _.Displacement != null))
                foreach (var face in faces.Where(_ => _.Displacement != null && _.Material != null))
                {
                    if (!PolygonIndicesByMaterial.TryGetValue(face.Material!, out var pi))
                        pi = PolygonIndicesByMaterial[face.Material!] = new List<List<int>>();

                    var (vertices, facesIndices) = face.Displacement!.Convert(face);
                    foreach (var faceIndices in facesIndices)
                        pi.Add(faceIndices.Select(fi => addVertex(vertices[fi])).ToList());
                }
            else
                foreach (var face in faces.Where(_ => _.Material != null))
                {
                    if (!PolygonIndicesByMaterial.TryGetValue(face.Material!, out var pi))
                        pi = PolygonIndicesByMaterial[face.Material!] = new List<List<int>>();

                    pi.Add(Enumerable.Range(0, face.Polygon.Count)
                        .Select(fi => addVertex(face.Polygon.Vertices[fi])).ToList());
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
        public IEnumerable<Face> Faces => _faces;

        public bool Contains(Vector v, double margin = DecalComputation.Margin)
        {
            if (double.IsInfinity(_bBoxMin.X))
                return false;

            return v.X + margin >= _bBoxMin.X && v.X - margin <= _bBoxMax.X &&
                   v.Y + margin >= _bBoxMin.Y && v.Y - margin <= _bBoxMax.Y &&
                   v.Z + margin >= _bBoxMin.Z && v.Z - margin <= _bBoxMax.Z;
        }
    }

    [TestFixture]
    public static class TestSolid
    {
        [Test]
        public static void TestRealWorldCSG()
        {
            var planes = new[]
            {
                "(-88 7020 -3248) (-160 6848 -3024) (-88 7020 -3024)".ParsePlaneString(),
                "(-88.9224 7020.3862 -3248) (-88.9224 7020.3862 -3024) (-160.9224 6848.3862 -3024)".ParsePlaneString(),
                "(-88 7020 -3248) (-88.9224 7020.3862 -3248) (-159.9998 6848 -3248)".ParsePlaneString(),
                "(-159.9998 6848 -3248) (-160.9223 6848.3862 -3248) (-160 6848 -3024)".ParsePlaneString(),
                "(-160 6848 -3024) (-160.9224 6848.3862 -3024) (-88 7020 -3024)".ParsePlaneString(),
                "(-88 7020 -3024) (-88.9224 7020.3862 -3024) (-88 7020 -3248)".ParsePlaneString()
            };
            var axis = new TextureAxis(Vector.One, 1, 1);
            var faces = planes.Select(plane =>
                new Face(plane, VMT.Empty, axis, axis, null)).ToList();

            var solid = new Solid(2, faces);
            var cos = solid.Vertices.ToList();
            Assert.That(cos.Count, Is.EqualTo(16));
            Assert.That(cos.Select(_ => _.Co.X).NotInRange(-161, -87.9), Is.Empty);
            Assert.That(cos.Select(_ => _.Co.Y).NotInRange(6848, 7020.3862), Is.Empty);
            Assert.That(cos.Select(_ => _.Co.Z).NotInRange(-3248, -3024), Is.Empty);
        }

        [Test]
        public static void TestRealWorldCSGSimplified()
        {
            var planes = new[]
            {
                "(5 2 0) (2 0 1) (5 2 1)".ParsePlaneString(),
                "(4 3 0) (4 3 1) (0 1 1)".ParsePlaneString(),
                "(5 2 0) (4 3 0) (3 0 0)".ParsePlaneString(),
                "(3 0 0) (1 1 0) (2 0 1)".ParsePlaneString(),
                "(2 0 1) (0 1 1) (5 2 1)".ParsePlaneString(),
                "(5 2 1) (4 3 1) (5 2 0)".ParsePlaneString()
            };
            var axis = new TextureAxis(Vector.One, 1, 1);
            var faces = planes.Select(plane =>
                new Face(plane, VMT.Empty, axis, axis, null)).ToList();

            var solid = new Solid(2, faces);
            var cos = solid.Vertices.ToList();
            Assert.That(cos.Count, Is.EqualTo(19));
            Assert.That(cos.Select(_ => _.Co.X).NotInRange(0, 5), Is.Empty);
            Assert.That(cos.Select(_ => _.Co.Y).NotInRange(0, 3), Is.Empty);
            Assert.That(cos.Select(_ => _.Co.Z).NotInRange(0, 1), Is.Empty);
        }

        [Test]
        public static void TestSimpleCut()
        {
            var planes = new[]
            {
                "(0 1 0) (0 0 0) (1 0 0)".ParsePlaneString(), // bottom (normal 0,0,-1)
                "(0 0 1) (0 1 1) (1 1 1)".ParsePlaneString() // top (normal 0,0,1)
            };

            var axis = new TextureAxis(Vector.One, 1, 1);
            var faces = planes.Select(plane =>
                new Face(plane, VMT.Empty, axis, axis, null)).ToList();

            var solid = new Solid(2, faces);
            var cos = solid.Vertices.ToList();
            Assert.That(cos.Count, Is.EqualTo(8));
            Assert.That(cos.Select(_ => _.Co.Z).NotInRange(0, 1), Is.Empty);
        }

        [Test]
        public static void TestUnitCubeCSG()
        {
            var planes = new[]
            {
                "(0 1 0) (0 0 0) (1 0 0)".ParsePlaneString(), // bottom (normal 0,0,-1)
                "(0 0 1) (0 1 1) (1 1 1)".ParsePlaneString(), // top (normal 0,0,1)
                "(0 0 1) (0 0 0) (0 1 0)".ParsePlaneString(), // left (normal -1,0,0)
                "(1 1 1) (1 1 0) (1 0 0)".ParsePlaneString(), // right (normal 1,0,0)
                "(1 0 1) (1 0 0) (0 0 0)".ParsePlaneString(), // front (normal 0,-1,0)
                "(0 1 1) (0 1 0) (1 1 0)".ParsePlaneString() // back (normal 0,1,0)
            };

            var axis = new TextureAxis(Vector.One, 1, 1);
            var faces = planes.Select(plane =>
                new Face(plane, VMT.Empty, axis, axis, null)).ToList();

            var solid = new Solid(2, faces);
            var cos = solid.Vertices.ToList();
            Assert.That(cos.Count, Is.EqualTo(14));
            Assert.That(cos.Select(_ => _.Co.X).NotInRange(0, 1), Is.Empty);
            Assert.That(cos.Select(_ => _.Co.Y).NotInRange(0, 1), Is.Empty);
            Assert.That(cos.Select(_ => _.Co.Z).NotInRange(0, 1), Is.Empty);
        }
    }
}
