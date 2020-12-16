using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using CommandLine;
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

        private static IList<string> CollectVTFFiles(string basePath)
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

                    if (Path.GetExtension(subPath).ToLower() != ".vtf")
                        continue;

                    files.Add(subPath);
                }

            return files;
        }

        private static void Main(string[] args)
        {
            if (!(Parser.Default.ParseArguments<Options>(args) is Parsed<Options> parsed))
                return;

            LogManager.ReconfigExistingLoggers();
            var files = CollectVTFFiles(parsed.Value.In);

            for (var i = 0; i < files.Count; i++)
            {
                var vtfPath = files[i];
                var vtf = new VTFFile(vtfPath);
                var images = vtf.Images.Where(_ => _.Mipmap == 0).ToList();
                if (images.Count == 0)
                {
                    logger.Warn($"{vtfPath} does not contain any image with mipmap level 0");
                    continue;
                }

                for (var frame = 0; frame < images.Count; frame++)
                {
                    var pngPath = Path.Join(parsed.Value.Out, Path.GetRelativePath(parsed.Value.In, vtfPath));
                    pngPath = Path.Join(Path.GetDirectoryName(pngPath),
                        Path.GetFileNameWithoutExtension(pngPath) +
                        (images.Count == 1 ? ".png" : $"-frame{frame}.png"));

                    if (File.Exists(pngPath))
                    {
                        logger.Info($"Skipping (already exists): {pngPath}");
                        continue;
                    }

                    logger.Info($"({i + 1} of {files.Count}) {images[frame].Format} {vtfPath} -> {pngPath}");
                    Directory.CreateDirectory(Path.GetDirectoryName(pngPath));
                    var img = images[frame];
                    if (img.FormatInfo == null)
                        throw new NullReferenceException();
                    if (img.FormatInfo.AlphaBitsPerPixel > 0)
                        Image.LoadPixelData<Bgra32>(img.GetBGRA32Data(), img.Width, img.Height).SaveAsPng(pngPath);
                    else
                        Image.LoadPixelData<Bgra32>(img.GetBGRA32Data(), img.Width, img.Height).CloneAs<Rgb24>()
                            .SaveAsPng(pngPath);
                }
            }
        }

        [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
        private class Options
        {
            [Option('i', "in", Required = true, HelpText = "Input Folder")]
            public string In { get; set; } = null!;

            [Option('o', "out", Required = true, HelpText = "Output Folder")]
            public string Out { get; set; } = null!;
        }
    }
}
