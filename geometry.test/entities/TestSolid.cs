using geometry.components;
using geometry.entities;
using geometry.materials;
using geometry.utils;
using NUnit.Framework;
using utility;

namespace geometry.test.entities;

file static class TestSolid
{
    [Test]
    public static void TestRealWorldCSG()
    {
        var planes = new[]
        {
            "(-88 7020 -3248) (-160 6848 -3024) (-88 7020 -3024)".ParseToPlane(),
            "(-88.9224 7020.3862 -3248) (-88.9224 7020.3862 -3024) (-160.9224 6848.3862 -3024)".ParseToPlane(),
            "(-88 7020 -3248) (-88.9224 7020.3862 -3248) (-159.9998 6848 -3248)".ParseToPlane(),
            "(-159.9998 6848 -3248) (-160.9223 6848.3862 -3248) (-160 6848 -3024)".ParseToPlane(),
            "(-160 6848 -3024) (-160.9224 6848.3862 -3024) (-88 7020 -3024)".ParseToPlane(),
            "(-88 7020 -3024) (-88.9224 7020.3862 -3024) (-88 7020 -3248)".ParseToPlane(),
        };
        var axis = new TextureAxis(Vector.One, 1, 1);
        var sides = planes.Select(plane =>
            new Side(123, plane, VMT.Empty, axis, axis, null)).ToList();

        var solid = new Solid(2, sides);
        var cos = solid.Vertices.ToList();
        Assert.That(cos.Count, Is.EqualTo(8));
        Assert.That(cos.Select(static v => v.Co.X).NotInRange(-161, -87.9), Is.Empty);
        Assert.That(cos.Select(static v => v.Co.Y).NotInRange(6848, 7020.3862), Is.Empty);
        Assert.That(cos.Select(static v => v.Co.Z).NotInRange(-3248, -3024), Is.Empty);
    }

    [Test]
    public static void TestRealWorldCSGSimplified()
    {
        var planes = new[]
        {
            "(5 2 0) (2 0 1) (5 2 1)".ParseToPlane(),
            "(4 3 0) (4 3 1) (0 1 1)".ParseToPlane(),
            "(5 2 0) (4 3 0) (3 0 0)".ParseToPlane(),
            "(3 0 0) (1 1 0) (2 0 1)".ParseToPlane(),
            "(2 0 1) (0 1 1) (5 2 1)".ParseToPlane(),
            "(5 2 1) (4 3 1) (5 2 0)".ParseToPlane(),
        };
        var axis = new TextureAxis(Vector.One, 1, 1);
        var sides = planes.Select(plane =>
            new Side(123, plane, VMT.Empty, axis, axis, null)).ToList();

        var solid = new Solid(2, sides);
        var cos = solid.Vertices.ToList();
        Assert.That(cos.Count, Is.EqualTo(8));
        Assert.That(cos.Select(static v => v.Co.X).NotInRange(0, 5), Is.Empty);
        Assert.That(cos.Select(static v => v.Co.Y).NotInRange(0, 3), Is.Empty);
        Assert.That(cos.Select(static v => v.Co.Z).NotInRange(0, 1), Is.Empty);
    }

    [Test]
    public static void TestSimpleCut()
    {
        var planes = new[]
        {
            "(0 1 0) (0 0 0) (1 0 0)".ParseToPlane(), // bottom (normal 0,0,-1)
            "(0 0 1) (0 1 1) (1 1 1)".ParseToPlane(), // top (normal 0,0,1)
        };

        var axis = new TextureAxis(Vector.One, 1, 1);
        var sides = planes.Select(plane =>
            new Side(123, plane, VMT.Empty, axis, axis, null)).ToList();

        var solid = new Solid(2, sides);
        var cos = solid.Vertices.ToList();
        Assert.That(cos.Count, Is.EqualTo(8));
        Assert.That(cos.Select(static v => v.Co.Z).NotInRange(0, 1), Is.Empty);
    }

    [Test]
    public static void TestUnitCubeCSG()
    {
        var planes = new[]
        {
            "(0 1 0) (0 0 0) (1 0 0)".ParseToPlane(), // bottom (normal 0,0,-1)
            "(0 0 1) (0 1 1) (1 1 1)".ParseToPlane(), // top (normal 0,0,1)
            "(0 0 1) (0 0 0) (0 1 0)".ParseToPlane(), // left (normal -1,0,0)
            "(1 1 1) (1 1 0) (1 0 0)".ParseToPlane(), // right (normal 1,0,0)
            "(1 0 1) (1 0 0) (0 0 0)".ParseToPlane(), // front (normal 0,-1,0)
            "(0 1 1) (0 1 0) (1 1 0)".ParseToPlane(), // back (normal 0,1,0)
        };

        var axis = new TextureAxis(Vector.One, 1, 1);
        var sides = planes.Select(plane =>
            new Side(123, plane, VMT.Empty, axis, axis, null)).ToList();

        var solid = new Solid(2, sides);
        var cos = solid.Vertices.ToList();
        Assert.That(cos.Count, Is.EqualTo(8));
        Assert.That(cos.Select(static v => v.Co.X).NotInRange(0, 1), Is.Empty);
        Assert.That(cos.Select(static v => v.Co.Y).NotInRange(0, 1), Is.Empty);
        Assert.That(cos.Select(static v => v.Co.Z).NotInRange(0, 1), Is.Empty);
    }
}