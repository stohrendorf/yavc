using System.Collections.Generic;
using geometry.materials.image;

namespace geometry.materials
{
    public static class VTFInfoCache
    {
        private static readonly Dictionary<string, VTFFile> cache = new Dictionary<string, VTFFile>();

        public static VTFFile Get(string filename)
        {
            if (!cache.TryGetValue(filename, out var file))
                file = cache[filename] = new VTFFile(filename, true);

            return file;
        }
    }
}
