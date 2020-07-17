using System.Linq;
using geometry.components;
using geometry.materials;
using geometry.utils;
using NUnit.Framework;
using utility;

namespace geometry.entities
{
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
            var sides = planes.Select(plane =>
                new Side(123, plane, VMT.Empty, axis, axis, null)).ToList();

            var solid = new Solid(2, sides);
            var cos = solid.Vertices.ToList();
            Assert.That(cos.Count, Is.EqualTo(19));
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
            var sides = planes.Select(plane =>
                new Side(123, plane, VMT.Empty, axis, axis, null)).ToList();

            var solid = new Solid(2, sides);
            var cos = solid.Vertices.ToList();
            Assert.That(cos.Count, Is.EqualTo(17));
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
            var sides = planes.Select(plane =>
                new Side(123, plane, VMT.Empty, axis, axis, null)).ToList();

            var solid = new Solid(2, sides);
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
            var sides = planes.Select(plane =>
                new Side(123, plane, VMT.Empty, axis, axis, null)).ToList();

            var solid = new Solid(2, sides);
            var cos = solid.Vertices.ToList();
            Assert.That(cos.Count, Is.EqualTo(14));
            Assert.That(cos.Select(_ => _.Co.X).NotInRange(0, 1), Is.Empty);
            Assert.That(cos.Select(_ => _.Co.Y).NotInRange(0, 1), Is.Empty);
            Assert.That(cos.Select(_ => _.Co.Z).NotInRange(0, 1), Is.Empty);
        }
    }
}
