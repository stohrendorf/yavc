using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using utility;
using VMFIO;

namespace geometry.materials
{
    public class VMT : IEquatable<VMT>
    {
        private static readonly Dictionary<string, VMT> cache = new Dictionary<string, VMT>();

        private static readonly bool isCaseSensitiveFilesystem = CheckCaseSensitiveFilesystem();

        public static readonly VMT Empty = new VMT();

        private readonly string _absolutePath;
        public readonly string Basename;
        public readonly string? BaseTexture;
        public readonly string? BaseTexture2;
        public readonly double DecalScale;
        public readonly int Height;
        public readonly string? NormalMap;
        public readonly string? NormalMap2;
        public readonly int Width;

        private VMT()
        {
            _absolutePath = string.Empty;
            Basename = string.Empty;
            Width = 1;
            Height = 1;
        }

        private VMT(string root, string subPath)
        {
            Basename = subPath;

            string? findTexture(string? p)
            {
                if (p == null)
                    return null;

                var resolved = FindSubPath(root, EnsureExtension(p, "vtf"));
                return resolved.Substring(root.Length);
            }

            _absolutePath = FindSubPath(root, EnsureExtension(subPath, "vmt"));
            var e = Parser.Parse(_absolutePath);
            Debug.Assert(e.Children.Count == 1);
            var rootChild = e.Children[0];
            BaseTexture = findTexture(rootChild.GetOptionalValue("$basetexture"));
            BaseTexture2 = findTexture(rootChild.GetOptionalValue("$basetexture2"));
            NormalMap = findTexture(rootChild.GetOptionalValue("$normalmap"));
            NormalMap2 = findTexture(rootChild.GetOptionalValue("$normalmap2"));
            DecalScale = StringUtil.ParseDouble(rootChild.GetOptionalValue("$decalscale") ?? "0.25");
            if ((BaseTexture ?? NormalMap) == null)
                throw new Exception($"Material {subPath} contains neither $basetexture nor $normalmap");
            (Width, Height) = VTF.GetSize(Path.Join(root, (BaseTexture ?? NormalMap)!));
        }

        public double DecalWidth => Width * DecalScale;

        public double DecalHeight => Height * DecalScale;

        public bool IsBlending => BaseTexture2 != null;

        public bool Equals(VMT? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(_absolutePath, other._absolutePath, StringComparison.OrdinalIgnoreCase);
        }

        private static bool CheckCaseSensitiveFilesystem()
        {
            string file = Path.GetTempPath() + "yavc_case_test";
            File.CreateText(file).Close();
            var isCaseInsensitive = File.Exists(file.ToUpper());
            File.Delete(file);
            return !isCaseInsensitive;
        }

        private static string FindSubPath(string root, string subPath)
        {
            if (!isCaseSensitiveFilesystem)
                return Path.Join(root, subPath);

            var components = subPath.Replace("\\", "/").TrimEnd('/').Split("/");
            var realPath = root;
            foreach (var component in components)
            {
                var absComponent = Path.Join(realPath, component);
                var found = Directory.EnumerateFileSystemEntries(realPath).SingleOrDefault(_ =>
                    string.Equals(_, absComponent, StringComparison.CurrentCultureIgnoreCase));
                if (found == string.Empty)
                    throw new ArgumentException($"Path {component} not found in {realPath}");
                realPath = found;
            }

            return realPath;
        }

        private static string EnsureExtension(string path, string ext)
        {
            if (path.ToLower().EndsWith("." + ext))
                return path;

            return path + "." + ext;
        }

        public static VMT GetCached(string root, string vmtPath)
        {
            if (cache.TryGetValue(vmtPath, out var vmt))
                return vmt;

            vmt = new VMT(root, vmtPath);
            cache.Add(vmtPath, vmt);
            return vmt;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((VMT) obj);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(_absolutePath);
        }

        public static bool operator ==(VMT? left, VMT? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(VMT? left, VMT? right)
        {
            return !Equals(left, right);
        }
    }
}
