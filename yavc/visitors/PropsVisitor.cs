using System.Collections.Generic;
using System.Linq;
using geometry.components;
using geometry.utils;
using utility;
using VMFIO;

namespace yavc.visitors;

internal sealed class VmfProp
{
    internal string Color = null!;
    internal string Model = null!;
    internal Vector Origin;
    internal Vector Rotation;
    internal int Skin;
}

internal sealed class VmfInstance
{
    internal Vector Angles;
    internal string File = null!;
    internal Vector Origin;
}

internal sealed class VmfEnvCubemap
{
    internal Vector Origin;
    internal IList<int> Sides = null!;
}

internal sealed class VmfLight
{
    internal Vector Color;
    internal double? Distance;
    internal Vector Origin;
    internal double Strength;
}

internal sealed class VmfSpotLight
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
    public readonly List<VmfEnvCubemap> EnvCubemaps = [];
    public readonly List<VmfInstance> Instances = [];
    public readonly List<VmfLight> Lights = [];
    public readonly List<VmfProp> Props = [];
    public readonly List<VmfSpotLight> SpotLights = [];

    public override void Visit(Entity entity, bool skipTools)
    {
        switch (entity.Typename)
        {
            case "entity" when entity.Classname is "prop_static" or "prop_dynamic" or "prop_physics_override"
                or "prop_physics_multiplayer" or "prop_dynamic_override" or "prop_detail" or "prop_physics"
                or "prop_ragdoll":
                Props.Add(new VmfProp
                {
                    Origin = entity["origin"].ParseToVector(),
                    Rotation = entity["angles"].ParseToVector().ToRad(),
                    Color = entity.GetOptionalValue("rendercolor") ?? "255 255 255",
                    Model = DropExtension(entity["model"].ToLower()),
                    Skin = StringUtil.ParseInt(entity.GetOptionalValue("skin") ?? "0"),
                });
                break;
            case "entity" when entity.Classname == "func_instance":
                Instances.Add(new VmfInstance
                {
                    File = entity["file"].RequireNotNull(),
                    Angles = entity["angles"].ParseToVector().ToRad(),
                    Origin = entity["origin"].ParseToVector(),
                });
                break;
            case "entity" when entity.Classname == "env_cubemap":
                EnvCubemaps.Add(new VmfEnvCubemap
                {
                    Origin = entity["origin"].ParseToVector(),
                    Sides = (entity.GetOptionalValue("sides") ?? "").Split(' ')
                        .Where(static sideId => !string.IsNullOrWhiteSpace(sideId)).Select(int.Parse).ToList(),
                });
                break;
            case "entity" when entity.Classname == "light":
            {
                var cols = entity["_light"].Split(" ").Select(double.Parse).ToArray();

                Lights.Add(new VmfLight
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

                SpotLights.Add(new VmfSpotLight
                {
                    Origin = entity["origin"].ParseToVector(),
                    Color = new Vector(cols[0], cols[1], cols[2]),
                    Strength = cols[3],
                    Distance = entity.GetOptionalValue("_distance")?.ParseToDouble(),
                    Angles = entity["angles"].ParseToVector().ToRad(),
                    Cone = entity["_cone"].ParseToDouble().ToRad(),
                    InnerCone = entity["_inner_cone"].ParseToDouble().ToRad(),
                    Exponent = entity["_exponent"].ParseToDouble(),
                    Pitch = entity["pitch"].ParseToDouble().ToRad(),
                });
                break;
            }
        }

        entity.Accept(this, skipTools);
        return;

        static string DropExtension(string p)
        {
            var dotIdx = p.LastIndexOf('.');
            return dotIdx < 0 ? p : p[..dotIdx];
        }
    }
}
