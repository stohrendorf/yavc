using geometry.components;

namespace geometry.materials.image;

public sealed class Header
{
  internal decimal Version { get; set; }
  internal ImageFlags Flags { get; set; }
  internal Vector Reflectivity { get; set; }
  internal float BumpmapScale { get; set; }
}
