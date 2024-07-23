using System;
using Assimp;
using geometry.materials;
using NLog;

namespace yavc;

internal static class SceneUtils
{
    private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

    public static int FindOrCreateMaterial(this Scene scene, VMT material)
    {
        for (var i = 0; i < scene.Materials.Count; ++i)
        {
            if (scene.Materials[i].Name == material.MaterialName)
            {
                return i;
            }
        }

        var mat = new Material { Name = material.MaterialName, IsTwoSided = true };
        var texIndex = 0;

        if (material.BaseTexture is not null)
        {
            TryAddTexture(material.BaseTexture, TextureType.Diffuse);
        }

        if (material.NormalMap is not null)
        {
            TryAddTexture(material.NormalMap, TextureType.Normals);
        }

        if (material.BaseTexture2 is not null)
        {
            TryAddTexture(material.BaseTexture2, TextureType.Diffuse);
        }

        if (material.NormalMap2 is not null)
        {
            TryAddTexture(material.NormalMap2, TextureType.Normals);
        }

        if (texIndex == 0)
        {
            logger.Warn($"Material {material.Basename} has no textures");
        }

        var matProp = mat.GetProperty("$mat.refracti,0,0");
        if (matProp is null)
        {
            matProp = new MaterialProperty("$mat.refracti", 1.45f);
            mat.AddProperty(matProp);
        }

        matProp.SetFloatValue(1.45f);
        scene.Materials.Add(mat);
        return scene.Materials.Count - 1;

        void TryAddTexture(string filePath, TextureType type)
        {
            var texture = new TextureSlot(filePath, type, texIndex++,
                TextureMapping.FromUV, 0,
                1,
                TextureOperation.Add, TextureWrapMode.Wrap, TextureWrapMode.Wrap, 0);
            if (!mat.AddMaterialTexture(ref texture))
            {
                throw new Exception($"Failed to add texture {filePath}");
            }
        }
    }
}