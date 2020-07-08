using System;
using System.Linq;
using Assimp;
using geometry;
using Face = Assimp.Face;

namespace VMFConverter
{
    public static class Exporter
    {
        public static Node Export(this Solid solid, Scene scene, Predicate<string> materialSkipPredicate)
        {
            var node = new Node($"solid_{solid.ID}");

            foreach (var (materialName, faces) in solid.PolygonIndicesByMaterial)
            {
                if (!faces.Any(_ => _.Count > 0))
                    continue;

                if (materialSkipPredicate(materialName))
                    continue;

                int? matIndex = null;
                for (var i = 0; i < scene.Materials.Count; ++i)
                    if (scene.Materials[i].Name == materialName)
                    {
                        matIndex = i;
                        break;
                    }

                if (matIndex == null)
                {
                    var mat = new Material
                    {
                        Name = materialName,
                        TextureDiffuse = new TextureSlot(materialName, TextureType.Diffuse, 0, TextureMapping.Plane, 0,
                            1,
                            TextureOperation.Add, TextureWrapMode.Wrap, TextureWrapMode.Wrap, 0),
                        IsTwoSided = true
                    };
                    scene.Materials.Add(mat);
                    matIndex = scene.Materials.Count - 1;
                }

                var mesh = new Mesh($"{solid.ID}-{scene.Meshes.Count}", PrimitiveType.Polygon);

                mesh.Vertices.AddRange(solid.Vertices
                    .Select(vertex => new Vector3D((float) vertex.Co.X, (float) vertex.Co.Z, -(float) vertex.Co.Y)));
                mesh.VertexColorChannels[0]
                    .AddRange(solid.Vertices.Select(vertex => new Color4D((float) vertex.Alpha, 1, 1, 1)));
                mesh.TextureCoordinateChannels[0].AddRange(solid.Vertices
                    .Select(vertex => new Vector3D((float) vertex.UV.X, (float) vertex.UV.Y, 0)));
                mesh.MaterialIndex = matIndex.Value;

                foreach (var indices in faces)
                    mesh.Faces.Add(new Face(indices.ToArray()));

                scene.Meshes.Add(mesh);
                node.MeshIndices.Add(scene.Meshes.Count - 1);
            }

            return node;
        }
    }
}
