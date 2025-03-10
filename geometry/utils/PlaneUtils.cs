﻿using System;
using System.Diagnostics;
using System.Linq;
using geometry.components;
using geometry.entities;

namespace geometry.utils;

internal static class PlaneUtils
{
    public static Polygon ToPolygon(this Plane plane, Side side)
    {
        var n = plane.Normal.MaxAxis() switch
        {
            0 or 1 => Vector.UnitZ,
            2 => Vector.UnitX,
            _ => throw new Exception(),
        };

        // project n onto p.Normal, then subtract that from the normal, giving the first base vector of the plane
        Debug.Assert(Math.Abs(plane.Normal.LengthSquared - 1) < 1e-8);
        var a = (n - n.Dot(plane.Normal) * plane.Normal).Normalized * 16384;
        // form the second base vector
        var b = a.Cross(plane.Normal);

        var origin = plane.Normal * -plane.D;

        var polygon = new Polygon();
        polygon.Add(new Vertex(origin - b + a, Vector2.Zero, 1));
        polygon.Add(new Vertex(origin + b + a, Vector2.Zero, 1));
        polygon.Add(new Vertex(origin + b - a, Vector2.Zero, 1));
        polygon.Add(new Vertex(origin - b - a, Vector2.Zero, 1));

        Debug.Assert(polygon.Vertices.Co.All(v => Math.Abs(plane.DistanceTo(v)) < 1e-3));

        for (var i = 0; i < 4; i++)
        {
            polygon.Vertices.UV[i] = side.CalcUV(polygon.Vertices.Co[i], side.Material?.BaseTextureTransform);
        }

        polygon.Vertices.NormalizeUV();

        return polygon;
    }
}