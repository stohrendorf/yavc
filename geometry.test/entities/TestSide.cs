using geometry.components;
using geometry.entities;
using NUnit.Framework;

namespace geometry.test.entities;

file static class TestSide
{
  [Test]
  public static void TestConstruction()
  {
    var axis = new TextureAxis(Vector.One, 1, 1);
    var plane = Plane.CreateFromVertices(Vector.Zero, Vector.UnitZ, Vector.UnitY);
    var side = new Side(123, plane, null, axis, axis, null);
    Assert.That(side.Polygon.Count, Is.EqualTo(4));
    Assert.That(side.Polygon.Vertices.Co[0].X, Is.EqualTo(0.0));
    Assert.That(side.Polygon.Vertices.Co[1].X, Is.EqualTo(0.0));
    Assert.That(side.Polygon.Vertices.Co[2].X, Is.EqualTo(0.0));
    Assert.That(side.Polygon.Vertices.Co[3].X, Is.EqualTo(0.0));
    Assert.That(side.Plane.Normal, Is.EqualTo(-Vector.UnitX));
    Assert.That(side.Plane.D, Is.EqualTo(0.0));
  }
}
