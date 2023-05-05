using Assimp;
using geometry.components;

namespace yavc;

internal static class OutputUtil
{
  public static Vector3D ToAssimp(this Vector v)
  {
    return new Vector3D((float)v.X, (float)v.Z, -(float)v.Y);
  }

  public static Vector3D ToAssimpUV(this Vector2 v)
  {
    return new Vector3D((float)v.X, 1 - (float)v.Y, 0);
  }
}
