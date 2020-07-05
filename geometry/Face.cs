using System.Diagnostics;
using NUnit.Framework;
using utility;

namespace geometry
{
    public class Face
    {
        private readonly int _texHeight;
        private readonly int _texWidth;
        private readonly Vector _uAxis;
        private readonly double _uShift;
        private readonly Vector _vAxis;
        private readonly double _vShift;
        public readonly Displacement? Displacement;
        public readonly string Material;
        public readonly Plane Plane;
        public readonly Polygon Polygon;

        public Face(Plane plane, string material, Vector uAxis, double uShift, Vector vAxis, double vShift,
            Displacement? displacement, int texWidth, int texHeight)
        {
            Plane = plane;
            Material = material;
            _uAxis = uAxis;
            _uShift = uShift;
            _vAxis = vAxis;
            _vShift = vShift;
            Displacement = displacement;
            _texWidth = texWidth;
            _texHeight = texHeight;

            Polygon = Plane.ToPolygon(this);
        }

        public Vector2 CalcUV(Vector vec)
        {
            if (_texWidth == 0 || _texHeight == 0)
                return new Vector2(0.0, 0.0);

            var u = 1 - (vec.Dot(_uAxis) + _uShift) / _texWidth;
            Debug.Assert(!double.IsNaN(u));
            var v = 1 - (vec.Dot(_vAxis) + _vShift) / _texHeight;
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
            var plane = Plane.CreateFromVertices(Vector.Zero, Vector.UnitZ, Vector.UnitY);
            var face = new Face(plane, "material", Vector.One, 1, Vector.One, 1, null, 512, 512);
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
