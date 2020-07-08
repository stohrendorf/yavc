using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Assimp;
using CommandLine;

namespace VMFConverter
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var parsed = Parser.Default.ParseArguments<Options>(args) as Parsed<Options>;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Console.WriteLine("Reading VMF");
            var data = VMFIO.Parser.ReadVmf(parsed!.Value.VMF);
            Console.WriteLine("Converting VMF geometry");
            var v = new VMFConvertVisitor(parsed.Value.Materials);
            v.Visit(data);

            Console.WriteLine("Building export scene");

            var scene = new Scene {RootNode = new Node(Path.GetFileName(parsed.Value.VMF))};

            foreach (var node in v.Vmf.Solids
                .Select(solid => solid.Export(scene, material => material.ToLower().StartsWith("tools/"))).Where(_ =>
                    _.HasMeshes))
                scene.RootNode.Children.Add(node);

            Console.WriteLine("Writing DAE");

            using (var ctx = new AssimpContext())
            {
                ctx.ExportFile(scene, parsed.Value.DAE, "collada");
            }

            var totalFaces = scene.Meshes.Sum(_ => _.FaceCount);
            var totalVertices = scene.Meshes.Sum(_ => _.VertexCount);

            Console.WriteLine($"Wrote {v.Vmf.Solids.Count} solids, {totalVertices} vertices, {totalFaces} faces");
        }

        [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
        private class Options
        {
            [Option('v', "vmf", Required = true, HelpText = "Input VMF")]
            public string VMF { get; set; } = null!;

            [Option('d', "dae", Required = true, HelpText = "Output DAE")]
            public string DAE { get; set; } = null!;

            [Option('m', "materials", Required = true, HelpText = "Materials base folder")]
            public string Materials { get; set; } = null!;
        }
    }
}
