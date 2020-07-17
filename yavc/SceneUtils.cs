using Assimp;
using geometry.materials;

namespace yavc
{
    public static class SceneUtils
    {
        public static int FindOrCreateMaterial(this Scene scene, VMT material)
        {
            for (var i = 0; i < scene.Materials.Count; ++i)
                if (scene.Materials[i].Name == material.Basename)
                    return i;

            var mat = new Material
            {
                Name = material.Basename,
                TextureDiffuse = new TextureSlot(material.BaseTexture, TextureType.Diffuse, 0,
                    TextureMapping.Plane, 0,
                    1,
                    TextureOperation.Add, TextureWrapMode.Wrap, TextureWrapMode.Wrap, 0),
                IsTwoSided = true
            };
            if (material.NormalMap != null)
                mat.TextureNormal = new TextureSlot(material.NormalMap, TextureType.Normals, 0,
                    TextureMapping.Plane, 0,
                    1,
                    TextureOperation.Add, TextureWrapMode.Wrap, TextureWrapMode.Wrap, 0);

            MaterialProperty matProp = mat.GetProperty("$mat.refracti,0,0");
            if (matProp == null)
            {
                matProp = new MaterialProperty("$mat.refracti", 1.45f);
                mat.AddProperty(matProp);
            }

            matProp.SetFloatValue(1.45f);
            scene.Materials.Add(mat);
            return scene.Materials.Count - 1;
        }
    }
}
