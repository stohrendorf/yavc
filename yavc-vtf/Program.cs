using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using geometry.materials.image;
using NLog;

namespace yavc_vtf
{
    internal static class Program
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private static void WriteTGA(string filename, Image img)
        {
            var imgData = img.GetBGRA32Data();
            if (imgData == null)
            {
                logger.Warn($"Could not write {filename}, no image data");
                return;
            }

            var f = new BinaryWriter(File.Create(filename));
            // header:
            f.Write((byte) 0); // no image id
            f.Write((byte) 0); // no palette
            f.Write((byte) 2); // BGR uncompressed
            // palette spec:
            f.Write((ushort) 0); // palette start
            f.Write((ushort) 0); // palette length
            f.Write((byte) 0); // palette bpp
            // image spec:
            f.Write((ushort) 0); // x0
            f.Write((ushort) 0); // y0
            f.Write((ushort) img.Width);
            f.Write((ushort) img.Height);
            f.Write((byte) 32); // bpp
            f.Write((byte) 8); // descriptor (8 bit alpha)
            // no id
            // no palette

            Debug.Assert(f.BaseStream.Position == 18);

            for (var i = 0; i < imgData.Length; i += 4)
            {
                f.Write(imgData[i + 0]);
                f.Write(imgData[i + 1]);
                f.Write(imgData[i + 2]);
                f.Write(imgData[i + 3]);
            }

            // footer
            f.Write((uint) 0); // extension area
            f.Write((uint) 0); // dev directory
            foreach (var b in Encoding.ASCII.GetBytes("TRUEVISION-XFILE.\0")) f.Write((byte) b);
        }

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
            var parsed = Parser.Default.ParseArguments<Options>(args) as Parsed<Options>;
            if (parsed == null)
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
                    var tgaPath = Path.Join(parsed.Value.Out, Path.GetRelativePath(parsed.Value.In, vtfPath));
                    tgaPath = Path.Join(Path.GetDirectoryName(tgaPath),
                        Path.GetFileNameWithoutExtension(tgaPath) +
                        (images.Count == 1 ? ".tga" : $"-frame{frame}.tga"));

                    if (File.Exists(tgaPath))
                    {
                        logger.Info($"Skipping (already exists): {tgaPath}");
                        continue;
                    }

                    logger.Info($"({i + 1} of {files.Count}) {images[frame].Format} {vtfPath} -> {tgaPath}");
                    Directory.CreateDirectory(Path.GetDirectoryName(tgaPath));
                    WriteTGA(tgaPath, images[frame]);
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
