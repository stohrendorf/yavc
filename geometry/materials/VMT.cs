using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using geometry.components;
using geometry.materials.image;
using geometry.utils;
using NLog;
using utility;
using VMFIO;

namespace geometry.materials
{
    public class VMT : IEquatable<VMT>
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private static readonly Dictionary<string, VMT?> cache = new Dictionary<string, VMT?>();

        private static readonly bool isCaseSensitiveFilesystem = CheckCaseSensitiveFilesystem();

        public static readonly VMT Empty = new VMT();
        private readonly string _absolutePath;
        private readonly double _decalScale;
        public readonly string Basename;
        public readonly string? BaseTexture;
        public readonly string? BaseTexture2;
        public readonly TextureTransform BaseTexture2Transform;

        public readonly TextureTransform BaseTextureTransform;
        public readonly TextureTransform BlendMaskTransform;
        public readonly TextureTransform BumpMapTransform;
        public readonly string? FlowMap;
        public readonly string? NormalMap;
        public readonly string? NormalMap2;
        public readonly bool SsBump;

        public readonly string Type;
        public readonly VTFFile? VTF;

        private VMT()
        {
            Type = string.Empty;
            _absolutePath = string.Empty;
            Basename = string.Empty;
            VTF = null;
        }

        public VMT(string root, string subPath, bool ignoreMissingVTF = false)
        {
            subPath = subPath.Replace('\\', '/').ToLower();
            Basename = subPath;

            string? FindTexture(string? p)
            {
                if (p == null)
                    return null;

                var resolved = FindSubPath(root, EnsureExtension(p, "vtf"));
                if (resolved != null)
                    return Path.GetRelativePath(root, resolved);

                logger.Warn($"Texture file {p} in material {subPath} not found");
                return null;
            }

            _absolutePath = FindSubPath(root, EnsureExtension(subPath, "vmt")) ??
                            throw new FileNotFoundException($"Material {subPath} not found");
            var e = Parser.ParseFile(_absolutePath);
            Debug.Assert(e.Children.Count == 1);
            var rootChild = e.Children[0];
            Type = rootChild.Typename;
            BaseTexture = FindTexture(rootChild.GetOptionalValue("$basetexture"));
            BaseTexture2 = FindTexture(rootChild.GetOptionalValue("$basetexture2"));
            FlowMap = FindTexture(rootChild.GetOptionalValue("$flowmap"));
            NormalMap = FindTexture(rootChild.GetOptionalValue("$normalmap") ?? rootChild.GetOptionalValue("$bumpmap"));
            NormalMap2 =
                FindTexture(rootChild.GetOptionalValue("$normalmap2") ?? rootChild.GetOptionalValue("$bumpmap2"));
            SsBump = int.Parse(rootChild.GetOptionalValue("$ssbump") ?? "0") != 0;
            _decalScale = StringUtil.ParseDouble(rootChild.GetOptionalValue("$decalscale") ?? "0.25");
            BaseTextureTransform = new TextureTransform(rootChild.GetOptionalValue("$basetexturetransform"));
            BaseTexture2Transform = new TextureTransform(rootChild.GetOptionalValue("$texture2transform"));
            BumpMapTransform = new TextureTransform(rootChild.GetOptionalValue("$bumpmaptransform"));
            BlendMaskTransform = new TextureTransform(rootChild.GetOptionalValue("$blendmasktransform"));

            var refTexture = FlowMap ?? BaseTexture ?? NormalMap;
            if (refTexture == null && !ignoreMissingVTF)
                throw new FileNotFoundException($"Material {subPath} contains no reference texture");
            try
            {
                if(refTexture != null)
                    VTF = VTFInfoCache.Get(Path.Join(root, refTexture));
            }
            catch (FileNotFoundException) when (ignoreMissingVTF)
            {
            }
            catch (DirectoryNotFoundException) when (ignoreMissingVTF)
            {
            }
        }

        public string MaterialName
        {
            get
            {
                var dotIdx = Basename.LastIndexOf('.');
                var withoutExtension = dotIdx < 0 ? Basename : Basename.Substring(0, dotIdx);
                return withoutExtension.Replace('/', ':');
            }
        }

        public double DecalWidth => Width * _decalScale;
        public double Width => VTF?.Width ?? 1;

        public double DecalHeight => Height * _decalScale;
        public double Height => VTF?.Height ?? 1;

        public bool IsBlending => BaseTexture2 != null;

        public bool Equals(VMT? other)
        {
            if (ReferenceEquals(null, other)) return false;
            return ReferenceEquals(this, other) ||
                   string.Equals(_absolutePath, other._absolutePath, StringComparison.OrdinalIgnoreCase);
        }

        private static bool CheckCaseSensitiveFilesystem()
        {
            string file = Path.GetTempPath() + "yavc_case_test";
            File.CreateText(file).Close();
            var isCaseInsensitive = File.Exists(file.ToUpper());
            File.Delete(file);
            return !isCaseInsensitive;
        }

        private static string? FindSubPath(string root, string subPath)
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
                if (string.IsNullOrEmpty(found))
                    return null;
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

        public static VMT? TryGetCached(string root, string vmtPath)
        {
            try
            {
                return GetCached(root, vmtPath);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public static VMT? GetCached(string root, string vmtPath)
        {
            if (cache.TryGetValue(vmtPath, out var vmt))
                return vmt;

            try
            {
                vmt = new VMT(root, vmtPath);
            }
            catch (FileNotFoundException)
            {
                logger.Warn($"Could not find material {vmtPath}");
                cache.Add(vmtPath, null);
                return null;
            }

            return vmt;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((VMT) obj);
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

        public readonly struct TextureTransform
        {
            private static readonly Regex transformRe =
                new Regex(
                    @"^center (?<center>.+? .+?) scale (?<scale>.+? .+?) rotate (?<rotate>.+?) translate (?<translate>.+? .+?)$");

            private readonly bool _isIdentity;
            public readonly Vector2 Center;
            public readonly double Rotate;
            public readonly Vector2 Scale;
            public readonly Vector2 Translate;

            public TextureTransform(string? transformString)
            {
                if (transformString != null)
                {
                    var transformMatch = transformRe.Match(transformString);
                    if (!transformMatch.Success)
                        throw new ArgumentException($"Invalid texture transform '{transformString}'",
                            nameof(transformString));

                    Center = transformMatch.Groups["center"].Value.ParseVector2();
                    Rotate = transformMatch.Groups["rotate"].Value.ParseDouble() / 90 * Math.PI;
                    Scale = transformMatch.Groups["scale"].Value.ParseVector2();
                    Translate = transformMatch.Groups["translate"].Value.ParseVector2();
                    _isIdentity = Math.Abs(Rotate) < 1e-4 && Translate == Vector2.Zero && Scale == Vector2.One;
                }
                else
                {
                    _isIdentity = true;
                    Center = Vector2.Zero;
                    Rotate = 0;
                    Scale = Vector2.One;
                    Translate = Vector2.Zero;
                }
            }

            public Vector2 Apply(Vector2 uv)
            {
                if (_isIdentity)
                    return uv;

                uv -= Center;
                var c = Math.Cos(Rotate);
                var s = Math.Sin(Rotate);
                uv = new Vector2(
                    (uv.X * c - uv.Y * s) * Scale.X,
                    (uv.X * s + uv.Y * c) * Scale.Y
                );
                uv += Center + Translate;
                return uv;
            }
        }
    }
}
