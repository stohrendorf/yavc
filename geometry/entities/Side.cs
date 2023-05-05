using System.Diagnostics;
using geometry.components;
using geometry.materials;
using geometry.utils;

namespace geometry.entities;

public class Side
{
  private readonly TextureAxis _uAxis;
  private readonly TextureAxis _vAxis;
  internal readonly Displacement? Displacement;
  public readonly int ID;
  internal readonly VMT? Material;
  public readonly Plane Plane;
  public Polygon Polygon;

  public Side(int id, Plane plane, VMT? material, TextureAxis uAxis, TextureAxis vAxis,
    Displacement? displacement)
  {
    ID = id;
    Plane = plane;
    Material = material;
    _uAxis = uAxis;
    _vAxis = vAxis;
    Displacement = displacement;

    Polygon = Plane.ToPolygon(this);
  }

  internal Vector2 CalcUV(Vector vec, VMT.TextureTransform? transform = null)
  {
    if (Material == null)
    {
      return new Vector2(0.0, 0.0);
    }

    var u = (vec.Dot(_uAxis.ScaledAxis) + _uAxis.Shift) / Material.Width;
    Debug.Assert(double.IsFinite(u));
    var v = (vec.Dot(_vAxis.ScaledAxis) + _vAxis.Shift) / Material.Height;
    Debug.Assert(double.IsFinite(v));

    var uv = new Vector2(u, v);
    return transform?.Apply(uv) ?? uv;
  }
}
