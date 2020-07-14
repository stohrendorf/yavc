using System;
using System.Linq;
using Assimp;
using geometry.components;
using geometry.entities;
using Face = Assimp.Face;

namespace yavc
{
    public static class Exporter
    {
        public static Node Export(this Solid solid, Scene scene, Predicate<string> materialSkipPredicate)
        {
            var node = new Node($"solid:{solid.ID}");

            foreach (var (material, faces) in solid.PolygonIndicesByMaterial)
            {
                if (!faces.Any(_ => _.Count > 0))
                    continue;

                if (materialSkipPredicate(material.Basename))
                    continue;

                int? matIndex = null;
                for (var i = 0; i < scene.Materials.Count; ++i)
                    if (scene.Materials[i].Name == material.Basename)
                    {
                        matIndex = i;
                        break;
                    }

                if (matIndex == null)
                {
                    var mat = new Material
                    {
                        Name = material.Basename,
                        TextureDiffuse = new TextureSlot(material.BaseTexture, TextureType.Diffuse, 0,
                            TextureMapping.Plane, 0,
                            1,
                            TextureOperation.Add, TextureWrapMode.Wrap, TextureWrapMode.Wrap, 0),
                        IsTwoSided = true
                    };
                    MaterialProperty matProp = mat.GetProperty("$mat.refracti,0,0");
                    if (matProp == null)
                    {
                        matProp = new MaterialProperty("$mat.refracti", 1.45f);
                        mat.AddProperty(matProp);
                    }

                    matProp.SetFloatValue(1.45f);
                    if (material.NormalMap != null)
                        mat.TextureNormal = new TextureSlot(material.NormalMap, TextureType.Normals, 0,
                            TextureMapping.Plane, 0,
                            1,
                            TextureOperation.Add, TextureWrapMode.Wrap, TextureWrapMode.Wrap, 0);
                    scene.Materials.Add(mat);
                    matIndex = scene.Materials.Count - 1;
                }

                var mesh = new Mesh($"solid:{solid.ID}-{scene.Meshes.Count}", PrimitiveType.Polygon);

                mesh.Vertices.AddRange(solid.Vertices
                    .Select(vertex => vertex.Co.ToAssimp()));
                mesh.VertexColorChannels[0]
                    .AddRange(solid.Vertices.Select(vertex => new Color4D((float) vertex.Alpha, 1, 1, 1)));
                mesh.TextureCoordinateChannels[0].AddRange(solid.Vertices
                    .Select(vertex => vertex.UV.ToAssimp()));
                mesh.MaterialIndex = matIndex.Value;

                foreach (var indices in faces)
                    mesh.Faces.Add(new Face(indices.ToArray()));

                scene.Meshes.Add(mesh);
                node.MeshIndices.Add(scene.Meshes.Count - 1);
            }

            return node;
        }

        public static Node Export(this Polygon decal, string material, Scene scene)
        {
            var node = new Node($"decal:{material}");
            int? matIndex = null;
            for (var i = 0; i < scene.Materials.Count; ++i)
                if (scene.Materials[i].Name == material)
                {
                    matIndex = i;
                    break;
                }

            if (matIndex == null)
            {
                var mat = new Material
                {
                    Name = material,
                    TextureDiffuse = new TextureSlot(material, TextureType.Diffuse, 0, TextureMapping.Plane, 0,
                        1,
                        TextureOperation.Add, TextureWrapMode.Wrap, TextureWrapMode.Wrap, 0),
                    IsTwoSided = true
                };
                scene.Materials.Add(mat);
                matIndex = scene.Materials.Count - 1;
            }

            var mesh = new Mesh($"decal:{material}-{scene.Meshes.Count}", PrimitiveType.Polygon);
            mesh.Vertices.AddRange(decal.Vertices
                .Select(vertex => new Vector3D((float) vertex.Co.X, (float) vertex.Co.Z, -(float) vertex.Co.Y)));
            mesh.TextureCoordinateChannels[0].AddRange(decal.Vertices
                .Select(vertex => new Vector3D((float) vertex.UV.X, (float) vertex.UV.Y, 0)));
            mesh.MaterialIndex = matIndex.Value;
            mesh.Faces.Add(new Face(Enumerable.Range(0, decal.Vertices.Count).ToArray()));

            scene.Meshes.Add(mesh);
            node.MeshIndices.Add(scene.Meshes.Count - 1);
            return node;
        }
    }
}
