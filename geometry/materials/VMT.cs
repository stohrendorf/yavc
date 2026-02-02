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

namespace geometry.materials;

public sealed partial class VMT : IEquatable<VMT>
{
  private static readonly Logger logger = LogManager.GetCurrentClassLogger();
  private static readonly Dictionary<string, VMT?> cache = new();

  private static readonly bool isCaseSensitiveFilesystem = CheckCaseSensitiveFilesystem();

  public static readonly VMT Empty = new();
  private readonly string _absolutePath;
  private readonly double _decalScale;

  private readonly VTFFile? _vtf;
  public readonly string Basename;
  public readonly string? BaseTexture;
  public readonly string? BaseTexture2;

  internal readonly TextureTransform BaseTextureTransform;
  public readonly string? NormalMap;
  public readonly string? NormalMap2;
  public readonly bool SsBump;

  private VMT()
  {
    _absolutePath = string.Empty;
    Basename = string.Empty;
    _vtf = null;
  }

  public VMT(string root, string subPath, bool ignoreMissingVTF = false)
  {
    subPath = subPath.Replace('\\', '/').ToLower();
    Basename = subPath;

    _absolutePath = FindSubPath(root, EnsureExtension(subPath, "vmt")) ??
                    throw new FileNotFoundException($"Material {subPath} not found");
    var e = Parser.ParseFile(_absolutePath);
    Debug.Assert(e.Children.Count == 1);
    var rootChild = e.Children[0];
    BaseTexture = FindTexture(rootChild.GetOptionalValue("$basetexture"));
    BaseTexture2 = FindTexture(rootChild.GetOptionalValue("$basetexture2"));
    var flowMap = FindTexture(rootChild.GetOptionalValue("$flowmap"));
    NormalMap = FindTexture(rootChild.GetOptionalValue("$normalmap") ?? rootChild.GetOptionalValue("$bumpmap"));
    NormalMap2 =
      FindTexture(rootChild.GetOptionalValue("$normalmap2") ?? rootChild.GetOptionalValue("$bumpmap2"));
    SsBump = int.Parse(rootChild.GetOptionalValue("$ssbump") ?? "0") != 0;
    _decalScale = StringUtil.ParseDouble(rootChild.GetOptionalValue("$decalscale") ?? "0.25");
    BaseTextureTransform = new TextureTransform(rootChild.GetOptionalValue("$basetexturetransform"));

    var refTexture = flowMap ?? BaseTexture ?? NormalMap;
    if (refTexture is null && !ignoreMissingVTF)
    {
      throw new FileNotFoundException($"Material {subPath} contains no reference texture");
    }

    try
    {
      if (refTexture is not null)
      {
        _vtf = VTFInfoCache.Get(Path.Join(root, refTexture));
      }
    }
    catch (FileNotFoundException ex)
    {
      logger.Error($"Texture file {ex.FileName} not found");
      if (!ignoreMissingVTF)
      {
        throw;
      }
    }
    catch (DirectoryNotFoundException)
    {
      logger.Error($"Texture file directory {Path.Join(root, refTexture)} not found");
      if (!ignoreMissingVTF)
      {
        throw;
      }
    }

    return;

    string? FindTexture(string? p)
    {
      if (p is null)
      {
        return null;
      }

      var resolved = FindSubPath(root, EnsureExtension(p, "vtf"));
      if (resolved is not null)
      {
        return Path.GetRelativePath(root, resolved);
      }

      logger.Warn($"Texture file {p} in material {subPath} not found");
      return null;
    }
  }

  public string MaterialName
  {
    get
    {
      var dotIdx = Basename.LastIndexOf('.');
      var withoutExtension = dotIdx < 0 ? Basename : Basename[..dotIdx];
      return withoutExtension.Replace('/', ':');
    }
  }

  internal double DecalWidth => Width * _decalScale;
  internal double Width => _vtf?.Width ?? 1;

  internal double DecalHeight => Height * _decalScale;
  internal double Height => _vtf?.Height ?? 1;

  public bool Equals(VMT? other)
  {
    if (ReferenceEquals(null, other))
    {
      return false;
    }

    return ReferenceEquals(this, other) ||
           string.Equals(_absolutePath, other._absolutePath, StringComparison.OrdinalIgnoreCase);
  }

  private static bool CheckCaseSensitiveFilesystem()
  {
    var file = Path.GetTempPath() + "yavc_case_test";
    File.CreateText(file).Close();
    var isCaseInsensitive = File.Exists(file.ToUpper());
    File.Delete(file);
    return !isCaseInsensitive;
  }

  private static string? FindSubPath(string root, string subPath)
  {
    if (!isCaseSensitiveFilesystem)
    {
      return Path.Join(root, subPath);
    }

    var components = subPath.Replace("\\", "/").TrimEnd('/').Split("/");
    var realPath = root;
    foreach (var found in components.Select(component => Path.Join(realPath, component)).Select(absComponent =>
               Directory.EnumerateFileSystemEntries(realPath).SingleOrDefault(path =>
                 string.Equals(path, absComponent, StringComparison.CurrentCultureIgnoreCase))))
    {
      if (string.IsNullOrEmpty(found))
      {
        return null;
      }

      realPath = found;
    }

    return realPath;
  }

  private static string EnsureExtension(string path, string ext)
  {
    if (path.ToLower().EndsWith("." + ext))
    {
      return path;
    }

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
    {
      return vmt;
    }

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
    catch (DirectoryNotFoundException)
    {
      logger.Warn($"Could not find material {vmtPath}");
      cache.Add(vmtPath, null);
      return null;
    }

    return vmt;
  }

  public override bool Equals(object? obj)
  {
    if (ReferenceEquals(null, obj))
    {
      return false;
    }

    if (ReferenceEquals(this, obj))
    {
      return true;
    }

    return obj.GetType() == GetType() && Equals((VMT)obj);
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

  public readonly partial struct TextureTransform
  {
    private readonly bool _isIdentity;
    private readonly Vector2 _center;
    private readonly double _rotate;
    private readonly Vector2 _scale;
    private readonly Vector2 _translate;

    internal TextureTransform(string? transformString)
    {
      if (transformString is not null)
      {
        var transformMatch = GetTransformRe().Match(transformString);
        if (!transformMatch.Success)
        {
          throw new ArgumentException($"Invalid texture transform '{transformString}'",
            nameof(transformString));
        }

        _center = transformMatch.Groups["center"].Value.ParseToVector2();
        _rotate = transformMatch.Groups["rotate"].Value.ParseToDouble() / 90 * Math.PI;
        _scale = transformMatch.Groups["scale"].Value.ParseToVector2();
        _translate = transformMatch.Groups["translate"].Value.ParseToVector2();
        _isIdentity = Math.Abs(_rotate) < 1e-4 && _translate == Vector2.Zero && _scale == Vector2.One;
      }
      else
      {
        _isIdentity = true;
        _center = Vector2.Zero;
        _rotate = 0;
        _scale = Vector2.One;
        _translate = Vector2.Zero;
      }
    }

    internal Vector2 Apply(Vector2 uv)
    {
      if (_isIdentity)
      {
        return uv;
      }

      uv -= _center;
      var c = Math.Cos(_rotate);
      var s = Math.Sin(_rotate);
      uv = new Vector2(
        (uv.X * c - uv.Y * s) * _scale.X,
        (uv.X * s + uv.Y * c) * _scale.Y
      );
      uv += _center + _translate;
      return uv;
    }

    [GeneratedRegex(
      "^center (?<center>.+? .+?) scale (?<scale>.+? .+?) rotate (?<rotate>.+?) translate (?<translate>.+? .+?)$")]
    private static partial Regex GetTransformRe();
  }
}
