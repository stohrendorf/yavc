using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;
using NLog;
using VMFIO;
using Parser = VMFIO.Parser;

namespace vmf_db;

file static class Program
{
  private static readonly Logger logger = LogManager.GetCurrentClassLogger();

  private static void Main(string[] args)
  {
    var parsed = CommandLine.Parser.Default.ParseArguments<Options>(args) as Parsed<Options>;
    if (parsed == null)
    {
      return;
    }

    LogManager.ReconfigExistingLoggers();

    var options = parsed.Value;

    logger.Info("Building VMF/VMT reference database");

    var database = new ReferenceDatabase();

    if (options.MaterialsFolder != null)
    {
      logger.Info($"Processing VMT files from {options.MaterialsFolder}");
      ProcessVmts(options.MaterialsFolder, database);
    }

    if (options.MapsFolder != null)
    {
      logger.Info($"Processing VMF files from {options.MapsFolder}");
      ProcessVmfs(options.MapsFolder, database);
    }

    // Write JSON output
    logger.Info($"Writing reference database to {options.Output}");
    var json = JsonConvert.SerializeObject(database, Formatting.Indented);
    File.WriteAllText(options.Output, json);

    logger.Info("Done!");
    logger.Info($"Processed {database.Materials.Count} materials, {database.Maps.Count} maps");
  }

  private static void ProcessVmts(string materialsFolder, ReferenceDatabase database)
  {
    var vmtFiles = CollectFiles(materialsFolder, ".vmt");
    logger.Info($"Found {vmtFiles.Count} VMT files");

    var materials = new ConcurrentDictionary<string, MaterialInfo>();
    var processedCount = 0;

    Parallel.ForEach(vmtFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, vmtPath =>
    {
      var relativePath = NormalizePath(Path.GetRelativePath(materialsFolder, vmtPath));

      try
      {
        var materialInfo = ExtractMaterialInfo(vmtPath, relativePath);
        materials[relativePath] = materialInfo;

        var count = Interlocked.Increment(ref processedCount);
        if (count % 100 == 0 || count == vmtFiles.Count)
        {
          logger.Info($"Processing VMT {count}/{vmtFiles.Count}");
        }
      }
      catch (Exception ex)
      {
        logger.Warn($"Failed to process {relativePath}: {ex.Message}");
      }
    });

    foreach (var kvp in materials)
    {
      database.Materials[kvp.Key] = kvp.Value;
    }
  }

  private static void ProcessVmfs(string mapsFolder, ReferenceDatabase database)
  {
    var vmfFiles = CollectFiles(mapsFolder, ".vmf");
    logger.Info($"Found {vmfFiles.Count} VMF files");

    var maps = new ConcurrentDictionary<string, MapInfo>();
    var processedCount = 0;

    Parallel.ForEach(vmfFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, vmfPath =>
    {
      var relativePath = NormalizePath(Path.GetRelativePath(mapsFolder, vmfPath));

      try
      {
        var mapInfo = ExtractMapInfo(vmfPath, relativePath);
        maps[relativePath] = mapInfo;

        var count = Interlocked.Increment(ref processedCount);
        logger.Info($"Processing VMF {count}/{vmfFiles.Count}: {relativePath}");
      }
      catch (Exception ex)
      {
        logger.Warn($"Failed to process {relativePath}: {ex.Message}");
      }
    });

    foreach (var kvp in maps)
    {
      database.Maps[kvp.Key] = kvp.Value;
    }
  }

  private static MaterialInfo ExtractMaterialInfo(string vmtPath, string relativePath)
  {
    var entity = Parser.ParseFile(vmtPath);
    if (entity.Children.Count == 0)
    {
      return new MaterialInfo { MaterialPath = relativePath, Textures = [] };
    }

    var rootChild = entity.Children[0];
    var textures = new HashSet<string>();

    TryAddTexture("$basetexture");
    TryAddTexture("$basetexture2");
    TryAddTexture("$flowmap");
    TryAddTexture("$normalmap");
    TryAddTexture("$normalmap2");
    TryAddTexture("$bumpmap");
    TryAddTexture("$bumpmap2");

    return new MaterialInfo
    {
      MaterialPath = relativePath,
      ShaderType = rootChild.Typename,
      Textures = textures.ToList(),
    };

    void TryAddTexture(string key)
    {
      var textureName = rootChild.GetOptionalValue(key);
      if (textureName == null)
      {
        return;
      }

      textureName = NormalizePath(textureName);
      if (!textureName.EndsWith(".vtf"))
      {
        textureName += ".vtf";
      }

      textures.Add(textureName);
    }
  }

  private static MapInfo ExtractMapInfo(string vmfPath, string relativePath)
  {
    var entity = Parser.ParseFile(vmfPath);
    var entityGroups = new Dictionary<string, EntityInfo>();
    var materials = new HashSet<string>();
    var decalGroups = new Dictionary<string, DecalInfo>();
    var overlayGroups = new Dictionary<string, OverlayInfo>();

    ExtractEntities(entity, entityGroups, materials, decalGroups, overlayGroups);

    return new MapInfo
    {
      MapPath = relativePath,
      Entities = entityGroups.Values.ToList(),
      Materials = materials.ToList(),
      Decals = decalGroups.Values.ToList(),
      Overlays = overlayGroups.Values.ToList(),
    };
  }

  private static void ExtractEntities(Entity entity, Dictionary<string, EntityInfo> entityGroups,
    HashSet<string> materials, Dictionary<string, DecalInfo> decalGroups, Dictionary<string, OverlayInfo> overlayGroups)
  {
    if (entity.Typename == "entity")
    {
      var classname = entity.GetOptionalValue("classname");
      if (classname != null)
      {
        // Handle decals
        if (classname == "infodecal")
        {
          var material = entity.GetOptionalValue("texture");
          if (material != null)
          {
            var normalizedMaterial = NormalizePath(material + ".vmt");
            if (!decalGroups.TryGetValue(normalizedMaterial, out var decalInfo))
            {
              decalInfo = new DecalInfo
              {
                Material = normalizedMaterial,
                Count = 0,
              };
              decalGroups[normalizedMaterial] = decalInfo;
            }

            decalInfo.Count++;
          }
        }
        // Handle overlays
        else if (classname == "info_overlay")
        {
          var material = entity.GetOptionalValue("material");
          if (material != null)
          {
            var normalizedMaterial = NormalizePath(material + ".vmt");
            if (!overlayGroups.TryGetValue(normalizedMaterial, out var overlayInfo))
            {
              overlayInfo = new OverlayInfo
              {
                Material = normalizedMaterial,
                Count = 0,
              };
              overlayGroups[normalizedMaterial] = overlayInfo;
            }

            overlayInfo.Count++;
          }
        }
        // Handle regular entities
        else
        {
          var model = entity.GetOptionalValue("model");
          var skin = ParseIntOrNull(entity.GetOptionalValue("skin"));
          var material = entity.GetOptionalValue("material");

          // Create unique key for grouping
          var key = $"{classname}|{model ?? ""}|{skin ?? 0}|{material ?? ""}";

          if (!entityGroups.TryGetValue(key, out var entityInfo))
          {
            entityInfo = new EntityInfo
            {
              Classname = classname,
              Model = model,
              Skin = skin,
              Material = material,
              Count = 0,
            };
            entityGroups[key] = entityInfo;
          }

          entityInfo.Count++;
        }
      }
    }
    else if (entity.Typename == "side")
    {
      // Extract material from brush side
      var material = entity.GetOptionalValue("material");
      if (material != null)
      {
        materials.Add(NormalizePath(material + ".vmt"));
      }
    }

    foreach (var child in entity.Children)
    {
      ExtractEntities(child, entityGroups, materials, decalGroups, overlayGroups);
    }
  }

  private static int? ParseIntOrNull(string? value)
  {
    if (value == null || !int.TryParse(value, out var result))
    {
      return null;
    }

    return result;
  }

  private static List<string> CollectFiles(string basePath, string extension)
  {
    var queue = new Queue<string>();
    queue.Enqueue(basePath);
    var files = new List<string>();

    while (queue.TryDequeue(out var p))
    {
      foreach (var subPath in Directory.EnumerateFileSystemEntries(p))
      {
        if (Directory.Exists(subPath))
        {
          queue.Enqueue(subPath);
          continue;
        }

        if (Path.GetExtension(subPath).Equals(extension, StringComparison.OrdinalIgnoreCase))
        {
          files.Add(subPath);
        }
      }
    }

    return files;
  }

  private static string NormalizePath(string path)
  {
    return path.ToLower().Replace('\\', '/').Trim('/');
  }

  [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
  [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
  private class Options
  {
    [Option('m', "materials", Required = false, HelpText = "Folder containing VMT files")]
    public string? MaterialsFolder { get; set; }

    [Option('f', "maps", Required = false, HelpText = "Folder containing VMF files")]
    public string? MapsFolder { get; set; }

    [Option('o', "output", Required = true, HelpText = "Output JSON file path")]
    public string Output { get; set; } = null!;
  }
}

public class ReferenceDatabase
{
  public Dictionary<string, MaterialInfo> Materials { get; set; } = new();
  public Dictionary<string, MapInfo> Maps { get; set; } = new();
}

public class MaterialInfo
{
  public string MaterialPath { get; set; } = null!;
  public string? ShaderType { get; set; }
  public List<string> Textures { get; set; } = [];
}

public class MapInfo
{
  public string MapPath { get; set; } = null!;
  public List<EntityInfo> Entities { get; set; } = [];
  public List<string> Materials { get; set; } = [];
  public List<DecalInfo> Decals { get; set; } = [];
  public List<OverlayInfo> Overlays { get; set; } = [];
}

public class EntityInfo
{
  public string Classname { get; set; } = null!;
  public string? Model { get; set; }
  public int? Skin { get; set; }
  public string? Material { get; set; }
  public int Count { get; set; }
}

public class DecalInfo
{
  public string Material { get; set; } = null!;
  public int Count { get; set; }
}

public class OverlayInfo
{
  public string Material { get; set; } = null!;
  public int Count { get; set; }
}
