using System;
using System.Collections.Generic;
using System.Linq;
using geometry.entities;
using yavc.visitors;

// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace yavc;

internal sealed class ExportEntity
{
  public IList<double> Color;
  public IList<double> Location;

  public string Model;
  public IList<double> Rotation;
  public int Skin;

  public ExportEntity(VMFProp entity)
  {
    var color = entity.Color.Split(' ');
    if (color.Length < 3)
    {
      throw new Exception();
    }

    color = color.Take(3).ToArray();

    Color = color.Take(3).Select(double.Parse).ToList();
    Location = new List<double> { entity.Origin.X, entity.Origin.Y, entity.Origin.Z };
    Rotation = new List<double> { entity.Rotation.Z, entity.Rotation.X, entity.Rotation.Y };
    Skin = entity.Skin;
    Model = entity.Model;
  }
}

internal sealed class ExportInstance
{
  public string File;
  public IList<double> Location;
  public IList<double> Rotation;

  public ExportInstance(VMFInstance instance)
  {
    File = instance.File;
    Location = new List<double> { instance.Origin.X, instance.Origin.Y, instance.Origin.Z };
    Rotation = new List<double> { instance.Angles.Z, instance.Angles.X, instance.Angles.Y };
  }
}

internal sealed class ExportEnvCubemap
{
  public IList<double> Location;
  public IList<int> Sides;

  public ExportEnvCubemap(VMFEnvCubemap cubemap)
  {
    Location = new List<double> { cubemap.Origin.X, cubemap.Origin.Y, cubemap.Origin.Z };
    Sides = new List<int>(cubemap.Sides);
  }
}

internal sealed class ExportLight
{
  public IList<double> Color;

  public IList<double> Location;
  public double Strength;

  public ExportLight(VMFLight light)
  {
    Location = new List<double> { light.Origin.X, light.Origin.Y, light.Origin.Z };
    Color = new List<double> { light.Color.X, light.Color.Y, light.Color.Z };
    Strength = light.Strength;
  }
}

internal sealed class ExportAmbientGeneric
{
  public readonly IList<double> Location;
  public readonly double Pitch;
  public readonly double Radius;
  public string Path;

  public ExportAmbientGeneric(AmbientGeneric ambientGeneric)
  {
    Path = ambientGeneric.Message;
    Location = new List<double> { ambientGeneric.Origin.X, ambientGeneric.Origin.Y, ambientGeneric.Origin.Z };
    Pitch = ambientGeneric.Pitch / 100;
    Radius = ambientGeneric.Radius;
  }
}

internal sealed class ExportData
{
  public readonly IList<ExportAmbientGeneric> Ambients = new List<ExportAmbientGeneric>();
  public readonly IList<ExportEntity> Entities = new List<ExportEntity>();
  public readonly IList<ExportEnvCubemap> EnvCubemaps = new List<ExportEnvCubemap>();
  public readonly IList<ExportInstance> Instances = new List<ExportInstance>();
  public readonly IList<ExportLight> Lights = new List<ExportLight>();
}
