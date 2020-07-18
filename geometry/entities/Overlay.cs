using System;
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
                    // project overlay vertices onto the infinite base plane
                    var overlayPolygon = new Polygon();
                    foreach (var v in Vertices)
                    {
                        var projected = VectorUtils.RayPlaneIntersection(v.Co, BasisNormal, side.Plane);
                        Debug.Assert(projected != null);
                        Debug.Assert(side.Plane.DistanceTo(projected.Value) < 1e-6);
                        overlayPolygon.Add(new Vertex(projected.Value, v.UV, 1.0));
                    }

                    // clamp the overlay to the side it's projected onto
                    var cutPoly = side.Polygon.EdgePlanes.Aggregate(overlayPolygon,
                        (current, edgePlane) => current.Cut(edgePlane));
                    if (side.Displacement == null)
                    {
                        yield return cutPoly;
                        yield break;
                    }

                    // split the cut polygon at the displacement edges
                    var subdividedPolygons = side.Displacement.EdgePlanes.Aggregate(Enumerable.Repeat(cutPoly, 1),
                        (current, edgePlane) => current.SelectMany(poly => poly.Split(edgePlane)).ToList()).ToList();

                    //var subdividedPolygons = side.Displacement.EdgePlanes.SelectMany(cutPoly.Split).ToList();
                    Debug.Assert(subdividedPolygons.All(poly =>
                        poly.Vertices.Co.All(co => side.Plane.DistanceTo(co) < 1e-6)));
                    Debug.Assert(subdividedPolygons.All(poly => poly.Count >= 3));

                    // now that we have the overlay polygons corresponding to the subdivided base polygons, we
                    // project each one to its displaced polygon.
                    foreach (var subdividedPolygon in subdividedPolygons)
                        yield return CalcDisplacedPoly(subdividedPolygon, side);
                }
            }
        }

        private static Polygon CalcDisplacedPoly(Polygon overlayPolygon, Side side)
        {
            foreach (var (displacedPolygon, flatPolygon) in side.Displacement!.DisplacedPolygons.Zip(side.Displacement
                .FlatPolygons))
            {
                var matched = new Polygon();

                bool tryProjectOnTriangle(Vertex overlayVertex, Vector f0, Vector f1, Vector f2, Vertex dp0, Vertex dp1,
                    Vertex dp2)
                {
                    if (!VectorUtils.CalcBarycentric(overlayVertex.Co, f0, f1, f2, out var s, out var t,
                        out var u)) return false;

                    var co = dp0.Co * s + dp1.Co * t + dp2.Co * u;
                    var uv = dp0.UV * s + dp1.UV * t + dp2.UV * u;
                    var alpha = dp0.Alpha * s + dp1.Alpha * t + dp2.Alpha * u;
                    matched.Add(new Vertex(co, uv, alpha));
                    return true;
                }

                foreach (var overlayVertex in overlayPolygon.Vertices)
                {
                    // fast path: most overlay vertices will exactly match with a base vertex
                    var matches = flatPolygon.Vertices.Zip(displacedPolygon.Vertices)
                        .Where(fv => overlayVertex.Co.Distance(fv.First.Co) < 1e-3).ToList();
                    if (matches.Count > 1)
                        throw new Exception();

                    if (matches.Count == 1)
                    {
                        matched.Add(matches[0].Second);
                        continue;
                    }

                    if (tryProjectOnTriangle(overlayVertex, flatPolygon.Vertices[0].Co, flatPolygon.Vertices[1].Co,
                        flatPolygon.Vertices[2].Co,
                        displacedPolygon.Vertices[0], displacedPolygon.Vertices[1], displacedPolygon.Vertices[2]))
                        continue;

                    if (tryProjectOnTriangle(overlayVertex, flatPolygon.Vertices[0].Co, flatPolygon.Vertices[2].Co,
                        flatPolygon.Vertices[3].Co,
                        displacedPolygon.Vertices[0], displacedPolygon.Vertices[2], displacedPolygon.Vertices[3]))
                        continue;

                    break; // if at least one vertex can't be displaced, we're not using the right polygon
                }

                if (overlayPolygon.Count != matched.Count)
                    continue;

                var result = new Polygon();
                var offset = side.Plane.Normal.Normalized * 0.1;
                foreach (var matchedVertex in matched.Vertices)
                    result.Add(new Vertex(matchedVertex.Co + offset, matchedVertex.UV, matchedVertex.Alpha));

                return result;
            }

            throw new Exception("Did not find a displaced polygon");
        }
    }
}
