using geometry.components;
using NUnit.Framework;

namespace geometry.entities
{
    [TestFixture]
    public static class TestFace
    {
        [Test]
        public static void TestConstruction()
        {
            var axis = new TextureAxis(Vector.One, 1, 1);
            var plane = Plane.CreateFromVertices(Vector.Zero, Vector.UnitZ, Vector.UnitY);
            var face = new Face(plane, null, axis, axis, null);
            Assert.That(face.Polygon.Count, Is.EqualTo(4));
            Assert.That(face.Polygon.Vertices[0].Co.X, Is.EqualTo(0.0));
            Assert.That(face.Polygon.Vertices[1].Co.X, Is.EqualTo(0.0));
            Assert.That(face.Polygon.Vertices[2].Co.X, Is.EqualTo(0.0));
            Assert.That(face.Polygon.Vertices[3].Co.X, Is.EqualTo(0.0));
            Assert.That(face.Plane.Normal, Is.EqualTo(-Vector.UnitX));
            Assert.That(face.Plane.D, Is.EqualTo(0.0));
        }
    }
}
