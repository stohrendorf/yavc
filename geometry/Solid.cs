using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using utility;

namespace geometry
{
    public class Solid
    {
        private readonly IndexedSet<Vertex> _vertices = new IndexedSet<Vertex>();

        public Solid(int id, IReadOnlyList<Face> faces)
        {
            ID = id;

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

            if (faces.Any(_ => _.Displacement != null))
                foreach (var face in faces.Where(_ => _.Displacement != null))
                {
                    if (!PolygonIndicesByMaterial.TryGetValue(face.Material, out var pi))
                        pi = PolygonIndicesByMaterial[face.Material] = new List<List<int>>();

                    var (vertices, facesIndices) = face.Displacement!.Convert(face);
                    foreach (var faceIndices in facesIndices)
                        pi.Add(faceIndices.Select(fi => _vertices.Add(vertices[fi])).ToList());
                }
            else
                foreach (var face in faces)
                {
                    if (!PolygonIndicesByMaterial.TryGetValue(face.Material, out var pi))
                        pi = PolygonIndicesByMaterial[face.Material] = new List<List<int>>();

                    pi.Add(Enumerable.Range(0, face.Polygon.Count)
                        .Select(fi => _vertices.Add(face.Polygon.Vertices[fi])).ToList());
                }
        }

        public int ID { get; }

        public Dictionary<string, List<List<int>>> PolygonIndicesByMaterial { get; } =
            new Dictionary<string, List<List<int>>>();

        public IEnumerable<Vertex> Vertices => _vertices.GetOrdered();
    }

    [TestFixture]
    public static class TestSolid
    {
        [Test]
        public static void TestRealWorldCSG()
        {
            var planes = new[]
            {
                ParserUtil.ParsePlaneString("(-88 7020 -3248) (-160 6848 -3024) (-88 7020 -3024)"),
                ParserUtil.ParsePlaneString(
                    "(-88.9224 7020.3862 -3248) (-88.9224 7020.3862 -3024) (-160.9224 6848.3862 -3024)"),
                ParserUtil.ParsePlaneString("(-88 7020 -3248) (-88.9224 7020.3862 -3248) (-159.9998 6848 -3248)"),
                ParserUtil.ParsePlaneString("(-159.9998 6848 -3248) (-160.9223 6848.3862 -3248) (-160 6848 -3024)"),
                ParserUtil.ParsePlaneString("(-160 6848 -3024) (-160.9224 6848.3862 -3024) (-88 7020 -3024)"),
                ParserUtil.ParsePlaneString("(-88 7020 -3024) (-88.9224 7020.3862 -3024) (-88 7020 -3248)")
            };
            var faces = planes.Select(plane =>
                new Face(plane, "material", Vector.One, 1, Vector.One, 1, null, 512, 512)).ToList();

            var solid = new Solid(2, faces);
            var cos = solid.Vertices.ToList();
            Assert.That(cos.Count, Is.EqualTo(24));
            Assert.That(cos.Select(_ => _.Co.X).NotInRange(-161, -87.9), Is.Empty);
            Assert.That(cos.Select(_ => _.Co.Y).NotInRange(6848, 7020.3862), Is.Empty);
            Assert.That(cos.Select(_ => _.Co.Z).NotInRange(-3248, -3024), Is.Empty);
        }

        [Test]
        public static void TestRealWorldCSGSimplified()
        {
            var planes = new[]
            {
                ParserUtil.ParsePlaneString("(5 2 0) (2 0 1) (5 2 1)"),
                ParserUtil.ParsePlaneString("(4 3 0) (4 3 1) (0 1 1)"),
                ParserUtil.ParsePlaneString("(5 2 0) (4 3 0) (3 0 0)"),
                ParserUtil.ParsePlaneString("(3 0 0) (1 1 0) (2 0 1)"),
                ParserUtil.ParsePlaneString("(2 0 1) (0 1 1) (5 2 1)"),
                ParserUtil.ParsePlaneString("(5 2 1) (4 3 1) (5 2 0)")
            };
            var faces = planes.Select(plane =>
                new Face(plane, "material", Vector.One, 1, Vector.One, 1, null, 512, 512)).ToList();

            var solid = new Solid(2, faces);
            var cos = solid.Vertices.ToList();
            Assert.That(cos.Count, Is.EqualTo(4 * 6));
            Assert.That(cos.Select(_ => _.Co.X).NotInRange(0, 5), Is.Empty);
            Assert.That(cos.Select(_ => _.Co.Y).NotInRange(0, 3), Is.Empty);
            Assert.That(cos.Select(_ => _.Co.Z).NotInRange(0, 1), Is.Empty);
        }

        [Test]
        public static void TestSimpleCut()
        {
            var planes = new[]
            {
                ParserUtil.ParsePlaneString("(0 1 0) (0 0 0) (1 0 0)"), // bottom (normal 0,0,-1)
                ParserUtil.ParsePlaneString("(0 0 1) (0 1 1) (1 1 1)") // top (normal 0,0,1)
            };

            var faces = planes.Select(plane =>
                new Face(plane, "material", Vector.One, 1, Vector.One, 1, null, 512, 512)).ToList();

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
                ParserUtil.ParsePlaneString("(0 1 0) (0 0 0) (1 0 0)"), // bottom (normal 0,0,-1)
                ParserUtil.ParsePlaneString("(0 0 1) (0 1 1) (1 1 1)"), // top (normal 0,0,1)
                ParserUtil.ParsePlaneString("(0 0 1) (0 0 0) (0 1 0)"), // left (normal -1,0,0)
                ParserUtil.ParsePlaneString("(1 1 1) (1 1 0) (1 0 0)"), // right (normal 1,0,0)
                ParserUtil.ParsePlaneString("(1 0 1) (1 0 0) (0 0 0)"), // front (normal 0,-1,0)
                ParserUtil.ParsePlaneString("(0 1 1) (0 1 0) (1 1 0)") // back (normal 0,1,0)
            };

            var faces = planes.Select(plane =>
                new Face(plane, "material", Vector.One, 1, Vector.One, 1, null, 512, 512)).ToList();

            var solid = new Solid(2, faces);
            var cos = solid.Vertices.ToList();
            Assert.That(cos.Count, Is.EqualTo(8));
            Assert.That(cos.Select(_ => _.Co.X).NotInRange(0, 1), Is.Empty);
            Assert.That(cos.Select(_ => _.Co.Y).NotInRange(0, 1), Is.Empty);
            Assert.That(cos.Select(_ => _.Co.Z).NotInRange(0, 1), Is.Empty);
        }
    }
}
