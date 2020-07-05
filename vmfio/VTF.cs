using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace VMFIO
{
    public static class VTF
    {
        public static (short width, short height) GetSize(BinaryReader s)
        {
            var ftype = s.ReadBytes(4);
            if (ftype[0] != 0x56 || ftype[1] != 0x54 || ftype[2] != 0x46 || ftype[3] != 0)
                throw new Exception("VTF signature mismatch");
            var version1 = s.ReadInt32();
            if (version1 != 7)
                throw new Exception($"Invalid VTF version (expected 7, got {version1})");
            var version2 = s.ReadInt32();
            var hsize = s.ReadInt32();
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

    public static class VMT
    {
        private static readonly Regex textureRe =
            new Regex(@"^\s*""?\$basetexture""?\s+""([^""]+)""$", RegexOptions.Compiled);

        private static readonly Regex normalRe =
            new Regex(@"^\s*""?\$normalmap""?\s+""([^""]+)""$", RegexOptions.Compiled);

        private static readonly Dictionary<string, (short width, short height)> cache =
            new Dictionary<string, (short, short)>();

        public static (short width, short height) GetSize(string root, string filename)
        {
            if (cache.TryGetValue(filename, out var size))
                return size;

            var lines = File.ReadLines(filename).ToList();
            var baseMatches = lines.Select(_ => textureRe.Match(_)).Where(_ => _.Success).ToList();
            if (baseMatches.Count == 0)
            {
                var normalMatches = lines.Select(_ => normalRe.Match(_)).Where(_ => _.Success).ToList();
                if (normalMatches.Count == 1)
                    return VTF.GetSize(Path.Join(root, normalMatches[0].Groups[1].Value + ".vtf"));
                Console.Out.WriteLine($"No basetexture in {filename}");
                cache.Add(filename, (0, 0));
                return (0, 0);
            }

            if (baseMatches.Count > 1)
            {
                Console.Out.WriteLine($"Multiple basetextures in {filename}");
                cache.Add(filename, (0, 0));
                return (0, 0);
            }

            size = VTF.GetSize(Path.Join(root, baseMatches[0].Groups[1].Value + ".vtf"));
            cache.Add(filename, size);
            return size;
        }
    }
}
