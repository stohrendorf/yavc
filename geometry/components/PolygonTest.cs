using NUnit.Framework;

namespace geometry.components
{
    [TestFixture]
    public static class PolygonTest
    {
        [Test]
        public static void TestCut()
        {
            var p = new Polygon();
            p.Add(new Vertex(new Vector(5, 1, 0), new Vector2(1, 1), 1));
            p.Add(new Vertex(new Vector(10, -1, 0), new Vector2(1, -1), 1));
            p.Add(new Vertex(new Vector(-2, -1, 0), new Vector2(-1, -1), 1));
            p.Add(new Vertex(new Vector(-7, 1, 0), new Vector2(-1, 1), 1));
            // cut on the Y/Z plane, giving a cut with all X coordinates >= 0
            p.Cut(new Plane(new Vector(1, 0, 0), -2));
            Assert.That(p.Count, Is.EqualTo(4));
            Assert.That(p.Vertices[0].Co, Is.EqualTo(new Vector(5, 1, 0)));
            Assert.That(p.Vertices[1].Co, Is.EqualTo(new Vector(10, -1, 0)));
            Assert.That(p.Vertices[2].Co, Is.EqualTo(new Vector(2, -1, 0)));
            Assert.That(p.Vertices[3].Co, Is.EqualTo(new Vector(2, 1, 0)));
        }

        [Test]
        public static void TestCutAll()
        {
            var p = new Polygon();
            p.Add(new Vertex(new Vector(5, 1, 0), Vector2.Zero, 1));
            p.Add(new Vertex(new Vector(10, -1, 0), Vector2.Zero, 1));
            p.Add(new Vertex(new Vector(-2, -1, 0), Vector2.Zero, 1));
            p.Add(new Vertex(new Vector(-7, 1, 0), Vector2.Zero, 1));
            // cut on the Y/Z plane, giving a cut with all X coordinates >= 0
            p.Cut(new Plane(new Vector(1, 0, 0), -200));
            Assert.That(p.Count, Is.EqualTo(0));
        }

        [Test]
        public static void TestCutNone()
        {
            var p = new Polygon();
            p.Add(new Vertex(new Vector(5, 1, 0), Vector2.Zero, 1));
            p.Add(new Vertex(new Vector(10, -1, 0), Vector2.Zero, 1));
            p.Add(new Vertex(new Vector(-2, -1, 0), Vector2.Zero, 1));
            p.Add(new Vertex(new Vector(-7, 1, 0), Vector2.Zero, 1));
            // cut on the Y/Z plane, giving a cut with all X coordinates >= 0
            p.Cut(new Plane(new Vector(1, 0, 0), 200));
            Assert.That(p.Count, Is.EqualTo(4));
            Assert.That(p.Vertices[0].Co, Is.EqualTo(new Vector(5, 1, 0)));
            Assert.That(p.Vertices[1].Co, Is.EqualTo(new Vector(10, -1, 0)));
            Assert.That(p.Vertices[2].Co, Is.EqualTo(new Vector(-2, -1, 0)));
            Assert.That(p.Vertices[3].Co, Is.EqualTo(new Vector(-7, 1, 0)));
        }
    }
}