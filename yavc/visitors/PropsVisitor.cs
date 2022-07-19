using System;
using System.Collections.Generic;
using System.Linq;
using geometry.components;
using geometry.utils;
using utility;
using VMFIO;

namespace yavc.visitors
{
    public class VMFProp
    {
        public string Color = null!;
        public string Model = null!;
        public Vector Origin;
        public Vector Rotation;
        public int Skin;
    }

    public class VMFInstance
    {
        public string File = null!;
        public Vector Origin;
        public Vector Angles;
    }

    public class VMFEnvCubemap
    {
        public Vector Origin;
        public IList<int> Sides = null!;
    }

    public class VMFLight
    {
        public Vector Origin;
        public Vector Color;
        public double Strength;
    }

    public class PropsVisitor : EntityVisitor
    {
        public readonly List<VMFProp> Props = new List<VMFProp>();
        public readonly List<VMFInstance> Instances = new List<VMFInstance>();
        public readonly List<VMFEnvCubemap> EnvCubemaps = new List<VMFEnvCubemap>();
        public readonly List<VMFLight> Lights = new List<VMFLight>();

        public override void Visit(Entity entity)
        {
            static string DropExtension(string p)
            {
                var dotIdx = p.LastIndexOf('.');
                return dotIdx < 0 ? p : p.Substring(0, dotIdx);
            }

            switch (entity.Typename)
            {
                case "entity" when entity.Classname == "prop_static" ||
                                   entity.Classname == "prop_dynamic" ||
                                   entity.Classname == "prop_physics_override" ||
                                   entity.Classname == "prop_physics_multiplayer" ||
                                   entity.Classname == "prop_dynamic_override" ||
                                   entity.Classname == "prop_detail" ||
                                   entity.Classname == "prop_physics" ||
                                   entity.Classname == "prop_ragdoll":
                    Props.Add(new VMFProp
                    {
                        Origin = entity["origin"].ParseVector(),
                        Rotation = entity["angles"].ParseVector() * Math.PI / 180.0,
                        Color = entity.GetOptionalValue("rendercolor") ?? "255 255 255",
                        Model = DropExtension(entity["model"].ToLower()),
                        Skin = StringUtil.ParseInt(entity.GetOptionalValue("skin") ?? "0")
                    });
                    break;
                case "entity" when entity.Classname == "func_instance":
                    Instances.Add(new VMFInstance
                    {
                        File = entity["file"].RequireNotNull(),
                        Angles = entity["angles"].ParseVector() * Math.PI / 180.0,
                        Origin = entity["origin"].ParseVector()
                    });
                    break;
                case "entity" when entity.Classname == "env_cubemap":
                    EnvCubemaps.Add(new VMFEnvCubemap
                    {
                        Origin = entity["origin"].ParseVector(),
                        Sides = (entity.GetOptionalValue("sides") ?? "").Split(' ')
                            .Where(_ => !string.IsNullOrWhiteSpace(_)).Select(int.Parse).ToList()
                    });
                    break;
                case "entity" when entity.Classname == "light":
                {
                    var cols = entity["_light"].Split(" ").Select(double.Parse).ToArray();

                    Lights.Add(new VMFLight()
                    {
                        Origin = entity["origin"].ParseVector(),
                        Color = new Vector(cols[0], cols[1], cols[2]),
                        Strength = cols[3]
                    });
                    break;
                }
            }

            entity.Accept(this);
        }
    }
}
