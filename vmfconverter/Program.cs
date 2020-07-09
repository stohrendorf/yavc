using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Assimp;
using CommandLine;
using NLog;

namespace VMFConverter
{
    internal static class Program
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private static void Main(string[] args)
        {
            var parsed = Parser.Default.ParseArguments<Options>(args) as Parsed<Options>;

            LogManager.ReconfigExistingLoggers();

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            logger.Info("Reading VMF");
            var data = VMFIO.Parser.ReadVmf(parsed!.Value.VMF);

            if (parsed.Value.DAE != null)
            {
                if (parsed.Value.Materials == null)
                {
                    logger.Error("Provide materials folder for geometry output");
                    return;
                }

                logger.Info("Converting VMF geometry");
                var converter = new VMFConvertVisitor(parsed.Value.Materials);
                converter.Visit(data);

                logger.Info("Building export scene");

                var scene = new Scene {RootNode = new Node(Path.GetFileName(parsed.Value.VMF))};

                foreach (var node in converter.Vmf.Solids
                    .Select(solid => solid.Export(scene, material => material.ToLower().StartsWith("tools/"))).Where(
                        _ =>
                            _.HasMeshes))
                    scene.RootNode.Children.Add(node);

                logger.Info("Writing DAE");

                using (var ctx = new AssimpContext())
                {
                    ctx.ExportFile(scene, parsed.Value.DAE, "collada");
                }

                var totalFaces = scene.Meshes.Sum(_ => _.FaceCount);
                var totalVertices = scene.Meshes.Sum(_ => _.VertexCount);

                logger.Info($"Wrote {converter.Vmf.Solids.Count} solids, {totalVertices} vertices, {totalFaces} faces");
            }

            if (parsed.Value.Entities != null)
            {
                logger.Info("Converting VMF entities");
                var converter = new VMFEntityVisitor();
                converter.Visit(data);

                using (var f = File.CreateText(parsed.Value.Entities))
                {
                    foreach (var entity in converter.Entities)
                        f.WriteLine(
                            $"{entity.Name} {entity.Color} {entity.Origin.X:F} {entity.Origin.Y:F} {entity.Origin.Z:F} {entity.Rotation.Z:F} {entity.Rotation.X:F} {entity.Rotation.Y:F}");
                }
            }
        }

        [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
        private class Options
        {
            [Option('v', "vmf", Required = true, HelpText = "Input VMF")]
            public string VMF { get; set; } = null!;

            [Option('d', "dae", Required = false, HelpText = "Output DAE")]
            public string DAE { get; set; } = null;

            [Option('e', "entities", Required = false, HelpText = "Output Entities for Blender")]
            public string? Entities { get; set; } = null;

            [Option('m', "materials", Required = false, HelpText = "Materials base folder")]
            public string? Materials { get; set; } = null;
        }
    }
}
