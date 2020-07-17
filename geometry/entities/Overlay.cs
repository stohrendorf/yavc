using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using geometry.components;
using geometry.materials;

namespace geometry.entities
{
    public class Overlay
    {
        public Vector BasisNormal { get; set; }
        public Vector BasisOrigin { get; set; }
        public Vector BasisU { get; set; }
        public Vector BasisV { get; set; }
        public Vector2 TextureU { get; set; }
        public Vector2 TextureV { get; set; }
        public VMT Material { get; set; } = null!;
        public Side[] Sides { get; set; } = null!;
        public Vector2[] UVs { get; set; } = null!;

        private IEnumerable<Vertex> Vertices
        {
            get
            {
                yield return new Vertex(BasisOrigin + UVs[0].X * BasisU + UVs[0].Y * BasisV,
                    new Vector2(TextureU.X, TextureV.X), 1.0);
                yield return new Vertex(BasisOrigin + UVs[1].X * BasisU + UVs[1].Y * BasisV,
                    new Vector2(TextureU.X, TextureV.Y), 1.0);
                yield return new Vertex(BasisOrigin + UVs[2].X * BasisU + UVs[2].Y * BasisV,
                    new Vector2(TextureU.Y, TextureV.Y), 1.0);
                yield return new Vertex(BasisOrigin + UVs[3].X * BasisU + UVs[3].Y * BasisV,
                    new Vector2(TextureU.Y, TextureV.X), 1.0);
            }
        }

        public IEnumerable<Polygon> Polygons
        {
            get
            {
                foreach (var side in Sides)
                {
                    var p = new Polygon();
                    foreach (var v in Vertices)
                    {
                        // project onto base plane
                        var projected = VectorUtils.RayPlaneIntersection(v.Co, BasisNormal, side.Plane);
                        Debug.Assert(projected != null);
                        p.Add(new Vertex(projected.Value, v.UV, 1.0));
                    }

                    Debug.Assert(p.Count > 0);

                    var result = side.Polygon.EdgePlanes.Aggregate(p, (current, edgePlane) => current.Cut(edgePlane));
                    yield return result;
                }
            }
        }
    }
}
