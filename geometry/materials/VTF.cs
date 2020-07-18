using System.Collections.Generic;
using geometry.materials.vtfimage;

namespace geometry.materials
{
    public static class VTF
    {
        private static readonly Dictionary<string, VTFFile> cache = new Dictionary<string, VTFFile>();

        public static VTFFile Get(string filename)
        {
            if (!cache.TryGetValue(filename, out var file))
                file = cache[filename] = new VTFFile(filename);

            return file;
        }

        public static (int width, int height) GetSize(string filename)
        {
            var file = Get(filename);
            return (file.Images[0].Width, file.Images[0].Height);
        }
    }
}
