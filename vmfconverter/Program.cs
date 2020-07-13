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

                logger.Info("Calculating Ropes");
                var ropeVis = new RopeVisitor();
                ropeVis.Visit(data);

                var scene = new Scene {RootNode = new Node(Path.GetFileName(parsed.Value.VMF))};

                var mat = new Material
                {
                    Name = "yavc-rope-material"
                };
                scene.Materials.Add(mat);

                foreach (var (id0, p0, points) in ropeVis.Chains)
                {
                    var node = new Node($"rope_{id0}")
                    {
                        Transform = Matrix4x4.FromTranslation(new Vector3D((float) p0.X, (float) p0.Z,
                            -(float) p0.Y))
                    };
                    var mesh = new Mesh($"rope_{id0}-{scene.Meshes.Count}", PrimitiveType.Line) {MaterialIndex = 0};

                    mesh.Vertices.AddRange(points.Select(v => new Vector3D((float) v.X, (float) v.Z, -(float) v.Y)));

                    for (var i = 0; i < points.Count - 1; ++i) mesh.Faces.Add(new Face(new[] {i, i + 1}));

                    scene.Meshes.Add(mesh);
                    node.MeshIndices.Add(scene.Meshes.Count - 1);
                    scene.RootNode.Children.Add(node);
                }

                logger.Info("Converting VMF geometry");
                var converter = new VMFConvertVisitor(parsed.Value.Materials);
                converter.Visit(data);

                logger.Info("Building export scene");

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

                logger.Info(
                    $"Wrote {converter.Vmf.Solids.Count} solids, {scene.Meshes.Count} meshes, {totalVertices} vertices, {totalFaces} faces");
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
            public string? DAE { get; set; } = null;

            [Option('e', "entities", Required = false, HelpText = "Output Entities for Blender")]
            public string? Entities { get; set; } = null;

            [Option('m', "materials", Required = false, HelpText = "Materials base folder")]
            public string? Materials { get; set; } = null;
        }
    }
}
