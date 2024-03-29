using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using geometry.components;
using geometry.materials;

namespace geometry.entities;

public sealed class Overlay
{
    public Vector BasisNormal { get; init; }
    public Vector BasisOrigin { get; init; }
    public Vector BasisU { get; init; }
    public Vector BasisV { get; init; }
    public Vector2 TextureU { get; init; }
    public Vector2 TextureV { get; init; }
    public VMT Material { get; init; } = null!;
    public Side[] Sides { get; init; } = null!;
    public Vector2[] UVs { get; init; } = null!;

    private IEnumerable<Vertex> Vertices
    {
        get
        {
            yield return new Vertex(BasisOrigin + UVs[0].X * BasisU + UVs[0].Y * BasisV,
                new Vector2(TextureU.X, TextureV.X), 255);
            yield return new Vertex(BasisOrigin + UVs[1].X * BasisU + UVs[1].Y * BasisV,
                new Vector2(TextureU.X, TextureV.Y), 255);
            yield return new Vertex(BasisOrigin + UVs[2].X * BasisU + UVs[2].Y * BasisV,
                new Vector2(TextureU.Y, TextureV.Y), 255);
            yield return new Vertex(BasisOrigin + UVs[3].X * BasisU + UVs[3].Y * BasisV,
                new Vector2(TextureU.Y, TextureV.X), 255);
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
                    Debug.Assert(projected is not null);
                    Debug.Assert(side.Plane.DistanceTo(projected.Value) < 1e-6);
                    overlayPolygon.Add(new Vertex(projected.Value,
                        side.Material?.BaseTextureTransform.Apply(v.UV) ?? v.UV, 255));
                }

                // clamp the overlay to the side it's projected onto
                var cutPoly =
                    side.Polygon.EdgePlanes.Aggregate(overlayPolygon,
                        static (current, edgePlane) => current.Cut(edgePlane));
                if (side.Displacement is null)
                {
                    var result = new Polygon();
                    var offset = side.Plane.Normal.Normalized * 0.1;
                    foreach (var matchedVertex in cutPoly.Vertices)
                        result.Add(new Vertex(matchedVertex.Co + offset, matchedVertex.UV, matchedVertex.Alpha));

                    yield return result;
                    continue;
                }

                // split the cut polygon at the displacement edges
                var subdividedPolygons = new List<Polygon> { cutPoly };
                subdividedPolygons = side.Displacement.EdgePlanes
                    .Aggregate(subdividedPolygons,
                        static (current, plane) => current.SelectMany(poly => poly.Split(plane)).ToList());

                Debug.Assert(subdividedPolygons.All(poly =>
                    poly.Vertices.Co.All(co => side.Plane.DistanceTo(co) < 1e-6)));
                Debug.Assert(subdividedPolygons.All(static poly => poly.Count >= 3));

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

            foreach (var overlayVertex in overlayPolygon.Vertices)
            {
                // fast path: most overlay vertices will exactly match with a base vertex
                var displacedMatches = flatPolygon.Vertices.Zip(displacedPolygon.Vertices)
                    .Where(fv => overlayVertex.Co.Distance(fv.First.Co) < VectorUtils.Epsilon)
                    .Select(static vertices => vertices.Second.Co).ToList();
                switch (displacedMatches.Count)
                {
                    case > 1:
                        throw new Exception();
                    case 1:
                        matched.Add(new Vertex(displacedMatches[0], overlayVertex.UV, overlayVertex.Alpha));
                        continue;
                }

                if (TryProjectOnTriangle(overlayVertex, 0, 1, 2)) continue;

                if (TryProjectOnTriangle(overlayVertex, 0, 2, 3)) continue;

                break; // if at least one vertex can't be displaced, we're not using the right polygon
            }

            if (overlayPolygon.Count != matched.Count) continue;

            var result = new Polygon();
            var offset = side.Plane.Normal.Normalized * 0.1;
            foreach (var matchedVertex in matched.Vertices)
                result.Add(new Vertex(matchedVertex.Co + offset, matchedVertex.UV, matchedVertex.Alpha));

            return result;

            bool TryProjectOnTriangle(Vertex overlayVertex, int i0, int i1, int i2)
            {
                var f0 = flatPolygon.Vertices[i0].Co;
                var f1 = flatPolygon.Vertices[i1].Co;
                var f2 = flatPolygon.Vertices[i2].Co;

                // try to find the barycentric values for the displacement coordinates
                if (!VectorUtils.CalcBarycentric(overlayVertex.Co, f0, f1, f2, out var s,
                        out var t, out var u))
                    return false;

                var dp0 = displacedPolygon.Vertices[i0].Co;
                var dp1 = displacedPolygon.Vertices[i1].Co;
                var dp2 = displacedPolygon.Vertices[i2].Co;
                var co = dp0 * s + dp1 * t + dp2 * u;

                matched.Add(new Vertex(co, overlayVertex.UV, overlayVertex.Alpha));
                return true;
            }
        }

        throw new Exception("Did not find a displaced polygon");
    }
}