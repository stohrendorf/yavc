using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using CommandLine;
using geometry.materials;
using geometry.materials.image;
using NLog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using VTFImage = geometry.materials.image.Image;

namespace yavc_vtf
{
    internal static class Program
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private static readonly Vector3 baseB = new Vector3(-0.40824829f, -0.70710678f, 0.57735027f);
        private static readonly Vector3 baseG = new Vector3(-0.40824829f, 0.70710678f, 0.57735027f);
        private static readonly Vector3 baseR = new Vector3(0.81649658f, 0, 0.57735027f);

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static Rgba32 SSBumpToNormal(Bgra32 p)
        {
            var R = p.R / 255.0f;
            var G = p.G / 255.0f;
            var B = p.B / 255.0f;

            var N = (baseG.Z * baseR.X - baseR.Z * baseG.X) / (baseG.Y * baseR.X);
            var P = (G * baseR.X - R * baseG.X) / (baseG.Y * baseR.X);
            var S = baseB.Z - baseR.Z * baseB.X / baseR.X - N * baseB.Y;
            var T = (B - P * baseB.Y) / S - (R * baseB.X) / (S * baseR.X);
            var U = (R - T * baseR.Z) / baseR.X;

            var n = Vector3.Normalize(new Vector3(U, P - T * N, T)) + Vector3.One;
            n /= 2.0f;

            return new Rgba32(
                checked((byte) Math.Round(n.X * 255)),
                checked((byte) Math.Round(n.Y * 255)),
                checked((byte) Math.Round(n.Z * 255)),
                p.A);
        }

        private static IList<string> CollectFiles(string basePath, string extension)
        {
            logger.Info("Collecting VTFs...");

            var queue = new Queue<string>();
            queue.Enqueue(basePath);
            var files = new List<string>();

            while (queue.TryDequeue(out var p))
                foreach (var subPath in Directory.EnumerateFileSystemEntries(p))
                {
                    if (Directory.Exists(subPath))
                    {
                        queue.Enqueue(subPath);
                        continue;
                    }

                    if (Path.GetExtension(subPath).ToLower() != extension)
                        continue;

                    files.Add(subPath);
                }

            return files;
        }

        private static void ConvertVTF(string vtfPath, string materialsDir, string destination,
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

            var images = vtf.Images.Where(_ => _.Mipmap == 0).ToList();
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
                Directory.CreateDirectory(Path.GetDirectoryName(pngPath));
                var img = images[frame];
                if (img.FormatInfo == null)
                    throw new NullReferenceException();

                var data = Image.LoadPixelData<Bgra32>(img.GetBGRA32Data(), img.Width, img.Height);
                if (convertSsBump)
                {
                    logger.Info("SSBump conversion");
                    var converted = new Image<Rgba32>(img.Width, img.Height);
                    for (int y = 0; y < img.Height; ++y)
                    for (int x = 0; x < img.Width; ++x)
                        converted[x, y] = SSBumpToNormal(data[x, y]);
                    converted.SaveAsPng(pngPath);
                }
                else
                {
                    if (img.FormatInfo.AlphaBitsPerPixel > 0)
                        data.SaveAsPng(pngPath);
                    else
                        data.CloneAs<Rgb24>().SaveAsPng(pngPath);
                }
            }
        }

        private static void Main(string[] args)
        {
            if (!(Parser.Default.ParseArguments<Options>(args) is Parsed<Options> parsed))
                return;

            LogManager.ReconfigExistingLoggers();
            var vmts = CollectFiles(parsed.Value.In, ".vmt");

            for (var i = 0; i < vmts.Count; i++)
            {
                var vmtPath = vmts[i];
                logger.Info($"({i + 1} of {vmts.Count}) {vmtPath}");
                var vmt = new VMT(parsed.Value.In, Path.GetRelativePath(parsed.Value.In, vmtPath), true);

                if (vmt.BaseTexture != null)
                    ConvertVTF(vmt.BaseTexture, parsed.Value.In, parsed.Value.Out);
                if (vmt.BaseTexture2 != null)
                    ConvertVTF(vmt.BaseTexture2, parsed.Value.In, parsed.Value.Out);
                if (vmt.NormalMap != null)
                    ConvertVTF(vmt.NormalMap, parsed.Value.In, parsed.Value.Out);
                if (vmt.NormalMap2 != null)
                    ConvertVTF(vmt.NormalMap2, parsed.Value.In, parsed.Value.Out);
                if (vmt.NormalMap != null)
                    ConvertVTF(vmt.NormalMap, parsed.Value.In, parsed.Value.Out, vmt.SsBump && parsed.Value.Normalize);
                if (vmt.NormalMap2 != null)
                    ConvertVTF(vmt.NormalMap2, parsed.Value.In, parsed.Value.Out, vmt.SsBump && parsed.Value.Normalize);
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
}
