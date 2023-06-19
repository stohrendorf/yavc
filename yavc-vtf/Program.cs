using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CommandLine;
using geometry.materials;
using geometry.materials.image;
using NLog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using VTFImage = geometry.materials.image.Image;

namespace yavc_vtf;

file static class Program
{
  private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

  private static readonly Vector3 baseB = new(-0.40824829f, -0.70710678f, 0.57735027f);
  private static readonly Vector3 baseG = new(-0.40824829f, 0.70710678f, 0.57735027f);
  private static readonly Vector3 baseR = new(0.81649658f, 0, 0.57735027f);

  [SuppressMessage("ReSharper", "InconsistentNaming")]
  private static Rgba32 SSBumpToNormal(Bgra32 p)
  {
    var rgba = p.ToVector4();

    var M = baseG.Y - baseR.Y * baseG.X / baseR.X;
    var N = (baseG.Z * baseR.X - baseR.Z * baseG.X) / (M * baseR.X);
    var P = (rgba.Y * baseR.X - rgba.X * baseG.X) / (M * baseR.X);
    var Q = baseB.Y - baseR.Y * baseB.X / baseR.X;
    var S = baseB.Z - baseR.Z * baseB.X / baseR.X - N * Q;
    var T = (rgba.Z - P * Q) / S - rgba.X * baseB.X / (S * baseR.X);
    var U = (rgba.X - T * baseR.Z) / baseR.X;

    Vector3 n;
    n.X = U - baseR.Y / baseR.X * (P - T * N);
    n.Y = P - T * N;
    n.Z = T;
    n = (Vector3.Normalize(n) + Vector3.One) / 2.0f;

    return new Rgba32(new Vector4(n, rgba.W));
  }

  private static IList<string> CollectFiles(string basePath, string extension)
  {
    logger.Info("Collecting VTFs...");

    var queue = new Queue<string>();
    queue.Enqueue(basePath);
    var files = new List<string>();

    while (queue.TryDequeue(out var p))
    {
      foreach (var subPath in Directory.EnumerateFileSystemEntries(p))
      {
        if (Directory.Exists(subPath))
        {
          queue.Enqueue(subPath);
          continue;
        }

        if (Path.GetExtension(subPath).ToLower() != extension)
        {
          continue;
        }

        files.Add(subPath);
      }
    }

    return files;
  }

  private static async Task ConvertVTF(string vtfPath, string materialsDir, string destination,
    bool convertSsBump = false)
  {
    logger.Info($"Processing {vtfPath}");
    VTFFile vtf;
    try
    {
      vtf = new VTFFile(Path.Join(materialsDir, vtfPath));
    }
    catch (FileNotFoundException)
    {
      logger.Warn($"{vtfPath} does not exist");
      return;
    }
    catch (DirectoryNotFoundException)
    {
      logger.Warn($"{vtfPath} does not exist");
      return;
    }

    var images = vtf.Images.Where(static image => image.Mipmap == 0).ToList();
    if (images.Count == 0)
    {
      logger.Warn($"{vtfPath} does not contain any image with mipmap level 0");
      return;
    }

    for (var frame = 0; frame < images.Count; frame++)
    {
      var pngPath = Path.Join(destination, vtfPath);
      pngPath = Path.Join(Path.GetDirectoryName(pngPath),
        Path.GetFileNameWithoutExtension(pngPath) +
        (images.Count == 1 ? ".png" : $"-frame{frame}.png"));

      if (File.Exists(pngPath))
      {
        logger.Info($"Skipping (already exists): {pngPath}");
        continue;
      }

      logger.Info($"{images[frame].Format} {vtfPath} -> {pngPath}");
      Directory.CreateDirectory(Path.GetDirectoryName(pngPath) ??
                                throw new InvalidOperationException(
                                  $"Could not extract directory name from {pngPath}"));
      var img = images[frame];
      if (img.FormatInfo is null)
      {
        throw new NullReferenceException();
      }

      var data = Image.LoadPixelData<Bgra32>(img.GetBGRA32Data(), img.Width, img.Height);
      if (convertSsBump)
      {
        logger.Info("SSBump conversion");
        data = await Task.Run(() =>
        {
          var converted = new Image<Rgba32>(img.Width, img.Height);
          for (var y = 0; y < img.Height; ++y)
          for (var x = 0; x < img.Width; ++x)
          {
            converted[x, y] = SSBumpToNormal(data[x, y]);
          }

          return converted.CloneAs<Bgra32>();
        });
      }

      if (img.FormatInfo.AlphaBitsPerPixel > 0)
      {
        await data.SaveAsPngAsync(pngPath);
      }
      else
      {
        await data.CloneAs<Rgb24>().SaveAsPngAsync(pngPath);
      }
    }
  }

  private static async Task Main(string[] args)
  {
    if (Parser.Default.ParseArguments<Options>(args) is not Parsed<Options> parsed)
    {
      return;
    }

    LogManager.ReconfigExistingLoggers();
    var vmts = CollectFiles(parsed.Value.In, ".vmt");

    for (var i = 0; i < vmts.Count; i++)
    {
      var vmtPath = vmts[i];
      logger.Info($"({i + 1} of {vmts.Count}) {vmtPath}");
      var vmt = new VMT(parsed.Value.In, Path.GetRelativePath(parsed.Value.In, vmtPath), true);

      if (vmt.BaseTexture is not null)
      {
        await ConvertVTF(vmt.BaseTexture, parsed.Value.In, parsed.Value.Out);
      }

      if (vmt.BaseTexture2 is not null)
      {
        await ConvertVTF(vmt.BaseTexture2, parsed.Value.In, parsed.Value.Out);
      }

      if (vmt.NormalMap is not null)
      {
        await ConvertVTF(vmt.NormalMap, parsed.Value.In, parsed.Value.Out);
      }

      if (vmt.NormalMap2 is not null)
      {
        await ConvertVTF(vmt.NormalMap2, parsed.Value.In, parsed.Value.Out);
      }

      if (vmt.NormalMap is not null)
      {
        await ConvertVTF(vmt.NormalMap, parsed.Value.In, parsed.Value.Out, vmt.SsBump && parsed.Value.Normalize);
      }

      if (vmt.NormalMap2 is not null)
      {
        await ConvertVTF(vmt.NormalMap2, parsed.Value.In, parsed.Value.Out, vmt.SsBump && parsed.Value.Normalize);
      }
    }
  }

  [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
  [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
  private class Options
  {
    [Option('i', "in", Required = true, HelpText = "Materials Folder")]
    public string In { get; set; } = null!;

    [Option('o', "out", Required = true, HelpText = "Output Folder")]
    public string Out { get; set; } = null!;

    [Option('n', "normalize", HelpText = "Convert SSBump textures to normal maps")]
    public bool Normalize { get; set; } = false;
  }
}
