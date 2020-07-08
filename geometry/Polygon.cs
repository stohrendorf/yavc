using System;
using System.Collections.Generic;
using NUnit.Framework;
using utility;

namespace geometry
{
    public class Polygon
    {
        public readonly List<Vertex> Vertices = new List<Vertex>();

        public int Count => Vertices.Count;

        public void Add(Vertex v)
        {
            Vertices.Add(v);
        }

        public void Cut(Plane split)
        {
            var result = new Polygon();

            void DoSplit(Vertex p1, Vertex p2)
            {
                var dot1 = split.Dot(p1.Co);
                var dot2 = split.Dot(p2.Co);
                if (dot1 < 0 && dot2 < 0)
                    // the edge is fully behind the plane
                    return;

                if (dot1 >= 0)
                    // keep points in front of the plane
                    result.Add(p1);

                if (dot1 > 0 && dot2 >= 0)
                    // the edge is fully in front of the plane
                    return;

                // plane: dot(p.normal, x) - p.d == 0
                // edge: x = e0 + lambda*eDir
                // -> dot(p.normal, e0 + lambda*eDir) - p.d == 0
                // -> dot(p.normal, e0 + lambda*eDir) == p.d
                // -> dot(p.normal, e0) + lambda*dot(p.normal, eDir) == p.d
                // -> lambda*dot(p.normal, eDir) == p.d - dot(p.normal, e0)
                // -> lambda == (p.d - dot(p.normal, e0)) / dot(p.normal, eDir)

                var d = p2.Co - p1.Co;
                var lambda = -dot1 / split.Normal.Dot(d);
                if (double.IsNaN(lambda))
                    return;

                // as both points are now on opposite sides of the plane, the intersection point must be on the edge
                if (lambda < 0 - 1e-6 || lambda > 1 + 1e-6)
                    throw new Exception($"Lambda not on edge: p1=({p1}) p2=({p2}) lambda={lambda} split={split}");

                var ud = p2.UV - p1.UV;
                result.Add(new Vertex(p1.Co + lambda * d, p1.UV + lambda * ud, 1));
            }

            for (var i = 0; i < Count; i++)
            {
                var i2 = (i + 1) % Count;
                DoSplit(Vertices[i], Vertices[i2]);
            }

            Vertices.Clear();

            Vertex prev = result.Vertices[^1];
            foreach (var vertex in result.Vertices)
                if (!Equals(prev, vertex))
                {
                    prev = vertex;
                    Vertices.Add(vertex);
                }

            Vertices.NormalizeUV();
        }
    }

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
