using System;
using System.Linq;
using Assimp;
using geometry.components;
using geometry.entities;
using geometry.materials;

namespace yavc;

internal static class Exporter
{
  public static Node Export(this Solid solid, Scene scene, Predicate<string> materialSkipPredicate)
  {
    var node = new Node($"solid:{solid.ID}");

    foreach (var (material, polygons) in solid.PolygonIndicesByMaterial)
    {
      if (!polygons.Any(static _ => _.Count > 0))
      {
        continue;
      }

      if (materialSkipPredicate(material.Basename))
      {
        continue;
      }

      var mesh = new Mesh($"solid:{solid.ID}-{scene.Meshes.Count}", PrimitiveType.Polygon)
      {
        MaterialIndex = scene.FindOrCreateMaterial(material),
      };

      mesh.Vertices.AddRange(solid.Vertices
        .Select(static vertex => vertex.Co.ToAssimp()));
      mesh.VertexColorChannels[0]
        .AddRange(solid.Vertices.Select(static vertex => new Color4D((float)vertex.Alpha / 255, 1, 1, 1)));
      mesh.TextureCoordinateChannels[0].AddRange(solid.Vertices
        .Select(static vertex => vertex.UV.ToAssimpUV()));

      foreach (var indices in polygons)
      {
        mesh.Faces.Add(new Face(indices.ToArray()));
      }

      scene.Meshes.Add(mesh);
      node.MeshIndices.Add(scene.Meshes.Count - 1);
    }

    return node;
  }

  public static Node ExportDecal(this Polygon polygon, VMT material, Scene scene)
  {
    var node = new Node($"decal:{material.MaterialName}");
    var mesh = new Mesh($"decal:{material.MaterialName}-{scene.Meshes.Count}", PrimitiveType.Polygon)
    {
      MaterialIndex = scene.FindOrCreateMaterial(material),
    };
    mesh.Vertices.AddRange(polygon.Vertices.Co.Select(static _ => _.ToAssimp()));
    mesh.TextureCoordinateChannels[0].AddRange(polygon.Vertices.UV.Select(static _ => _.ToAssimpUV()));
    mesh.Faces.Add(new Face(Enumerable.Range(0, polygon.Vertices.Count).ToArray()));

    scene.Meshes.Add(mesh);
    node.MeshIndices.Add(scene.Meshes.Count - 1);
    return node;
  }

  public static Node Export(this Overlay overlay, Scene scene)
  {
    var node = new Node($"overlay:{overlay.Material.MaterialName}");
    var mesh = new Mesh($"overlay:{overlay.Material.MaterialName}-{scene.Meshes.Count}", PrimitiveType.Polygon)
    {
      MaterialIndex = scene.FindOrCreateMaterial(overlay.Material),
    };

    foreach (var polygon in overlay.Polygons)
    {
      var i0 = mesh.Vertices.Count;
      mesh.Vertices.AddRange(polygon.Vertices.Co.Select(static _ => _.ToAssimp()));
      mesh.TextureCoordinateChannels[0].AddRange(polygon.Vertices.UV.Select(static _ => _.ToAssimpUV()));
      mesh.Faces.Add(new Face(Enumerable.Range(i0, polygon.Vertices.Count).ToArray()));
    }

    scene.Meshes.Add(mesh);
    node.MeshIndices.Add(scene.Meshes.Count - 1);
    return node;
  }
}
