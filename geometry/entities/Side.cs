using System.Diagnostics;
using geometry.components;
using geometry.materials;
using geometry.utils;

namespace geometry.entities
{
    public class Side
    {
        private readonly TextureAxis _uAxis;
        private readonly TextureAxis _vAxis;
        public readonly Displacement? Displacement;
        public readonly int ID;
        public readonly VMT? Material;
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

        public Vector2 CalcUV(Vector vec)
        {
            if (Material == null)
                return new Vector2(0.0, 0.0);

            var u = (vec.Dot(_uAxis.ScaledAxis) + _uAxis.Shift) / Material.Width;
            Debug.Assert(!double.IsNaN(u));
            var v = (vec.Dot(_vAxis.ScaledAxis) + _vAxis.Shift) / Material.Height;
            Debug.Assert(!double.IsNaN(v));
            return new Vector2(u, v);
        }
    }
}
