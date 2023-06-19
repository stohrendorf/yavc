using System;
using System.Collections.Generic;
using System.Linq;
using geometry.components;
using geometry.utils;
using utility;
using VMFIO;

namespace yavc.visitors;

internal sealed class VMFProp
{
  internal string Color = null!;
  internal string Model = null!;
  internal Vector Origin;
  internal Vector Rotation;
  internal int Skin;
}

internal sealed class VMFInstance
{
  internal Vector Angles;
  internal string File = null!;
  internal Vector Origin;
}

internal sealed class VMFEnvCubemap
{
  internal Vector Origin;
  internal IList<int> Sides = null!;
}

internal sealed class VMFLight
{
  internal Vector Color;
  internal double? Distance;
  internal Vector Origin;
  internal double Strength;
}

internal sealed class VMFSpotLight
{
  internal Vector Angles;
  internal Vector Color;
  internal double Cone;
  internal double? Distance;
  internal double Exponent;
  internal double InnerCone;
  internal Vector Origin;
  internal double Pitch;
  internal double Strength;
}

internal sealed class PropsVisitor : EntityVisitor
{
  public readonly List<VMFEnvCubemap> EnvCubemaps = new();
  public readonly List<VMFInstance> Instances = new();
  public readonly List<VMFLight> Lights = new();
  public readonly List<VMFProp> Props = new();
  public readonly List<VMFSpotLight> SpotLights = new();

  public override void Visit(Entity entity, bool skipTools)
  {
    static string DropExtension(string p)
    {
      var dotIdx = p.LastIndexOf('.');
      return dotIdx < 0 ? p : p[..dotIdx];
    }

    switch (entity.Typename)
    {
      case "entity" when entity.Classname is "prop_static" or "prop_dynamic" or "prop_physics_override"
        or "prop_physics_multiplayer" or "prop_dynamic_override" or "prop_detail" or "prop_physics" or "prop_ragdoll":
        Props.Add(new VMFProp
        {
          Origin = entity["origin"].ParseToVector(),
          Rotation = entity["angles"].ParseToVector() * Math.PI / 180.0,
          Color = entity.GetOptionalValue("rendercolor") ?? "255 255 255",
          Model = DropExtension(entity["model"].ToLower()),
          Skin = StringUtil.ParseInt(entity.GetOptionalValue("skin") ?? "0"),
        });
        break;
      case "entity" when entity.Classname == "func_instance":
        Instances.Add(new VMFInstance
        {
          File = entity["file"].RequireNotNull(),
          Angles = entity["angles"].ParseToVector() * Math.PI / 180.0,
          Origin = entity["origin"].ParseToVector(),
        });
        break;
      case "entity" when entity.Classname == "env_cubemap":
        EnvCubemaps.Add(new VMFEnvCubemap
        {
          Origin = entity["origin"].ParseToVector(),
          Sides = (entity.GetOptionalValue("sides") ?? "").Split(' ')
            .Where(static sideId => !string.IsNullOrWhiteSpace(sideId)).Select(int.Parse).ToList(),
        });
        break;
      case "entity" when entity.Classname == "light":
      {
        var cols = entity["_light"].Split(" ").Select(double.Parse).ToArray();

        Lights.Add(new VMFLight
        {
          Origin = entity["origin"].ParseToVector(),
          Color = new Vector(cols[0], cols[1], cols[2]),
          Strength = cols[3],
          Distance = entity.GetOptionalValue("_distance")?.ParseToDouble(),
        });
        break;
      }
      case "entity" when entity.Classname == "light_spot":
      {
        var cols = entity["_light"].Split(" ").Select(double.Parse).ToArray();

        SpotLights.Add(new VMFSpotLight
        {
          Origin = entity["origin"].ParseToVector(),
          Color = new Vector(cols[0], cols[1], cols[2]),
          Strength = cols[3],
          Distance = entity.GetOptionalValue("_distance")?.ParseToDouble(),
          Angles = entity["angles"].ParseToVector() * Math.PI / 180.0,
          Cone = entity["_cone"].ParseToDouble() * Math.PI / 180.0,
          InnerCone = entity["_inner_cone"].ParseToDouble() * Math.PI / 180.0,
          Exponent = entity["_exponent"].ParseToDouble(),
          Pitch = entity["pitch"].ParseToDouble() * Math.PI / 180.0,
        });
        break;
      }
    }

    entity.Accept(this, skipTools);
  }
}
