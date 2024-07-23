using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Assimp;
using CommandLine;
using Newtonsoft.Json;
using NLog;
using yavc.visitors;

namespace yavc;

file static class Program
{
    private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

    private static void Main(string[] args)
    {
        var parsed = Parser.Default.ParseArguments<Options>(args) as Parsed<Options>;

        LogManager.ReconfigExistingLoggers();

        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        logger.Info("Reading VMF");
        var data = VMFIO.Parser.ParseFile(parsed!.Value.Vmf);

        if (parsed.Value.DAE is not null)
        {
            if (parsed.Value.Materials is null)
            {
                logger.Error("Provide materials folder for geometry output");
                return;
            }

            var ropeVis = new RopeVisitor();
            ropeVis.Visit(data, parsed.Value.SkipTools);
            logger.Info($"Connecting and calculating {ropeVis.Count} rope keypoints");

            var scene = new Scene { RootNode = new Node(Path.GetFileName(parsed.Value.Vmf)) };

            var mat = new Material
            {
                Name = "yavc-rope-material",
            };
            scene.Materials.Add(mat);

            var numRopes = 0;
            foreach (var (id0, p0, points) in ropeVis.Chains)
            {
                ++numRopes;
                var node = new Node($"rope:{id0}")
                {
                    Transform = Matrix4x4.FromTranslation(p0.ToAssimp()),
                };
                var mesh = new Mesh($"rope:{id0}-{scene.Meshes.Count}", PrimitiveType.Line) { MaterialIndex = 0 };

                mesh.Vertices.AddRange(points.Select(static v => v.ToAssimp()));

                for (var i = 0; i < points.Count - 1; ++i)
                {
                    mesh.Faces.Add(new Face([i, i + 1]));
                }

                scene.Meshes.Add(mesh);
                node.MeshIndices.Add(scene.Meshes.Count - 1);
                scene.RootNode.Children.Add(node);
            }

            logger.Info("Converting VMF geometry");
            var converter = new SolidVisitor(parsed.Value.Materials);
            converter.Visit(data, parsed.Value.SkipTools);

            logger.Info("Processing decals");
            var decalVis = new DecalVisitor(parsed.Value.Materials);
            decalVis.Visit(data, parsed.Value.SkipTools);

            foreach (var decal in decalVis.Decals)
            {
                var candidates = converter.Vmf.Solids.Where(s => s.Contains(decal.Origin)).ToList();
                if (candidates.Count == 0)
                {
                    logger.Warn($"No candidate solids found for {decal.Id}");
                    continue;
                }

                var createdDecals = 0;
                foreach (var poly in candidates.Select(candidate => decal.TryConvert(candidate))
                             .Where(static poly => poly is not null))
                {
                    createdDecals++;
                    scene.RootNode.Children.Add(poly!.ExportDecal(decal.Material, scene));
                }

                if (createdDecals == 0)
                {
                    logger.Warn(
                        $"Could not create decal {decal.Id} at {decal.Origin} (material {decal.Material.Basename})");
                }
            }

            logger.Info("Processing overlays");
            var overlaysVis = new OverlayVisitor(parsed.Value.Materials,
                converter.Vmf.Solids.SelectMany(static solid => solid.Sides)
                    .ToImmutableDictionary(static side => side.Id, static side => side));
            overlaysVis.Visit(data, parsed.Value.SkipTools);

            foreach (var overlay in overlaysVis.Overlays)
            {
                scene.RootNode.Children.Add(overlay.Export(scene));
            }

            logger.Info("Building export scene");

            foreach (var node in converter.Vmf.Solids
                         .Select(solid =>
                             solid.Export(scene,
                                 static material =>
                                     material.StartsWith("tools/", StringComparison.InvariantCultureIgnoreCase)))
                         .Where(static node => node.HasMeshes))
            {
                scene.RootNode.Children.Add(node);
            }

            logger.Info("Writing DAE");

            using (var ctx = new AssimpContext())
            {
                ctx.ExportFile(scene, parsed.Value.DAE, "collada");
            }

            var totalFaces = scene.Meshes.Sum(static mesh => mesh.FaceCount);
            var totalVertices = scene.Meshes.Sum(static mesh => mesh.VertexCount);

            logger.Info(
                $"Wrote {converter.Vmf.Solids.Count} solids, {numRopes} ropes, {decalVis.Decals.Count} decals, {overlaysVis.Overlays.Count} overlays, {scene.Meshes.Count} meshes, {totalVertices} vertices, {totalFaces} faces");
        }

        if (parsed.Value.Entities is not null)
        {
            logger.Info("Exporting VMF entities");

            if (parsed.Value.Sounds is null)
            {
                logger.Error("Provide sounds folder for entities output");
                return;
            }

            var export = new ExportData();

            var propsVisitor = new PropsVisitor();
            propsVisitor.Visit(data, parsed.Value.SkipTools);

            logger.Info("Processing ambient sounds");
            var ambientVis = new AmbientGenericVisitor(parsed.Value.Sounds);
            ambientVis.Visit(data, parsed.Value.SkipTools);

            foreach (var exportEntity in propsVisitor.Props.Select(static prop => new ExportEntity(prop)))
            {
                export.Entities.Add(exportEntity);
            }

            foreach (var exportInstance in propsVisitor.Instances.Select(
                         static instance => new ExportInstance(instance)))
            {
                export.Instances.Add(exportInstance);
            }

            foreach (var exportCubemap in propsVisitor.EnvCubemaps.Select(static envCubemap =>
                         new ExportEnvCubemap(envCubemap)))
            {
                export.EnvCubemaps.Add(exportCubemap);
            }

            foreach (var light in propsVisitor.Lights.Select(static light => new ExportLight(light)))
            {
                export.Lights.Add(light);
            }

            foreach (var light in propsVisitor.SpotLights.Select(static light => new ExportSpotLight(light)))
            {
                export.SpotLights.Add(light);
            }

            foreach (var ambient in ambientVis.AmbientGenerics)
            {
                export.Ambients.Add(new ExportAmbientGeneric(ambient));
            }

            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Include,
#if DEBUG
                Formatting = Formatting.Indented,
#endif
            };

            using var sw = File.CreateText(parsed.Value.Entities);
            using var jw = new JsonTextWriter(sw);
            serializer.Serialize(jw, export);

            logger.Info(
                $"Wrote {propsVisitor.Props.Count} props, {propsVisitor.Instances.Count} instances, {propsVisitor.EnvCubemaps.Count} cubemaps, {propsVisitor.Lights.Count} lights, {propsVisitor.SpotLights.Count} spot lights, {ambientVis.AmbientGenerics.Count} ambient sounds");
        }
    }

    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    private class Options
    {
        [Option('v', "vmf", Required = true, HelpText = "Input VMF")]
        public string Vmf { get; set; } = null!;

        [Option('d', "dae", Required = false, HelpText = "Output DAE")]
        public string? DAE { get; set; } = null;

        [Option('e', "entities", Required = false, HelpText = "Output Entities for Blender")]
        public string? Entities { get; set; } = null;

        [Option('m', "materials", Required = false, HelpText = "Materials base folder")]
        public string? Materials { get; set; } = null;

        [Option('s', "sounds", Required = false, HelpText = "Sounds base folder")]
        public string? Sounds { get; set; } = null;

        [Option('t', "tools", Required = false, HelpText = "Skip tool textures", Default = false)]
        public bool SkipTools { get; set; } = false;
    }
}
