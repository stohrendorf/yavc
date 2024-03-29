using System.Linq;
using geometry.components;
using geometry.materials;

namespace geometry.entities;

public sealed class Decal
{
    public readonly int Id;
    public readonly VMT Material;
    public readonly Vector Origin;

    public Decal(int id, Vector origin, VMT material)
    {
        Origin = origin;
        Material = material;
        Id = id;
    }

    public Polygon? TryConvert(Solid solid)
    {
        return solid.Sides
            .Select(f => (f.Plane.DistanceTo(Origin), f))
            .Where(static df => df.Item1 is <= DecalComputation.Eps and >= -1e-4)
            .Select(df => DecalComputation.CreateClippedPoly(this, df.Item2))
            .FirstOrDefault(static poly => poly is not null);
    }
}