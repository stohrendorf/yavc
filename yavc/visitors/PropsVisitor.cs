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
        public IList<int> Sides;
    }

    public class PropsVisitor : EntityVisitor
    {
        public readonly List<VMFProp> Props = new List<VMFProp>();
        public readonly List<VMFInstance> Instances = new List<VMFInstance>();
        public readonly List<VMFEnvCubemap> EnvCubemaps = new List<VMFEnvCubemap>();

        public override void Visit(Entity entity)
        {
            static string dropExtension(string p)
            {
                var dotIdx = p.LastIndexOf('.');
                return dotIdx < 0 ? p : p.Substring(0, dotIdx);
            }

            if (entity.Typename == "entity" &&
                (entity.Classname == "prop_static" || entity.Classname == "prop_dynamic"))
            {
                Props.Add(new VMFProp
                {
                    Origin = entity["origin"].ParseVector(),
                    Rotation = entity["angles"].ParseVector() * Math.PI / 180.0,
                    Color = entity["rendercolor"],
                    Model = dropExtension(entity["model"].ToLower()),
                    Skin = StringUtil.ParseInt(entity.GetOptionalValue("skin") ?? "0")
                });
            }
            else if (entity.Typename == "entity" && entity.Classname == "func_instance")
            {
                Instances.Add(new VMFInstance
                {
                    File = entity["file"].RequireNotNull(),
                    Angles = entity["angles"].ParseVector() * Math.PI / 180.0,
                    Origin = entity["origin"].ParseVector()
                });
            }
            else if (entity.Typename == "entity" && entity.Classname == "env_cubemap")
            {
                EnvCubemaps.Add(new VMFEnvCubemap
                {
                    Origin = entity["origin"].ParseVector(),
                    Sides = (entity.GetOptionalValue("sides") ?? "").Split(' ')
                        .Where(_ => !string.IsNullOrWhiteSpace(_)).Select(int.Parse).ToList()
                });
            }

            entity.Accept(this);
        }
    }
}
