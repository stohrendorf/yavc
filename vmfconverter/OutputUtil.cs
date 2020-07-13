using Assimp;
using geometry;

namespace VMFConverter
{
    public static class OutputUtil
    {
        public static Vector3D ToAssimp(this Vector v)
        {
            return new Vector3D((float) v.X, (float) v.Z, -(float) v.Y);
        }

        public static Vector3D ToAssimp(this Vector2 v)
        {
            return new Vector3D((float) v.X, (float) v.Y, 0);
        }
    }
}
