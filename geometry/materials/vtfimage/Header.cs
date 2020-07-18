using geometry.components;

namespace geometry.materials.vtfimage
{
    public class Header
    {
        public decimal Version { get; set; }
        public ImageFlags Flags { get; set; }
        public Vector Reflectivity { get; set; }
        public float BumpmapScale { get; set; }
    }
}
