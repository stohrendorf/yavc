using System.Diagnostics;
using NUnit.Framework;

namespace geometry
{
    public class Face
    {
        private readonly TextureAxis _uAxis;
        private readonly TextureAxis _vAxis;
        public readonly Displacement? Displacement;
        public readonly VMT? Material;
        public readonly Plane Plane;
        public readonly Polygon Polygon;

        public Face(Plane plane, VMT? material, TextureAxis uAxis, TextureAxis vAxis, Displacement? displacement)
        {
            Plane = plane;
            Material = material;
            _uAxis = uAxis;
            _vAxis = vAxis;
            Displacement = displacement;

            Polygon = Plane.ToPolygon(this);
        }

        public Vector2 CalcUV(Vector vec)
        {
            if (Material == null)
                return new Vector2(0.0, 0.0);

            var u = 1 - (vec.Dot(_uAxis.ScaledAxis) + _uAxis.Shift) / Material.Width;
            Debug.Assert(!double.IsNaN(u));
            var v = 1 - (vec.Dot(_vAxis.ScaledAxis) + _vAxis.Shift) / Material.Height;
            Debug.Assert(!double.IsNaN(v));
            return new Vector2(u, v);
        }
    }

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
