using System;
using System.IO;

namespace geometry.materials
{
    public static class VTF
    {
        private static (short width, short height) GetSize(BinaryReader s)
        {
            var fType = s.ReadBytes(4);
            if (fType[0] != 0x56 || fType[1] != 0x54 || fType[2] != 0x46 || fType[3] != 0)
                throw new Exception("VTF signature mismatch");
            var version1 = s.ReadInt32();
            if (version1 != 7)
                throw new Exception($"Invalid VTF version (expected 7, got {version1})");
            var version2 = s.ReadInt32();
            var hSize = s.ReadInt32();
            var width = s.ReadInt16();
            if (width <= 0)
                throw new Exception($"Invalid non-positive width {width}");
            var height = s.ReadInt16();
            if (height <= 0)
                throw new Exception($"Invalid non-positive height {width}");
            return (width, height);
        }

        public static (short width, short height) GetSize(string filename)
        {
            using var s = new BinaryReader(File.Open(filename, FileMode.Open));
            return GetSize(s);
        }
    }
}
