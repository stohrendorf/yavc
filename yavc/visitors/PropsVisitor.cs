using System;
using System.Collections.Generic;
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

    public class PropsVisitor : EntityVisitor
    {
        public readonly List<VMFProp> Props = new List<VMFProp>();

        public override void Visit(Entity entity)
        {
            static string dropExtension(string p)
            {
                var dotIdx = p.LastIndexOf('.');
                return dotIdx < 0 ? p : p.Substring(0, dotIdx);
            }

            if (entity.Typename == "entity" &&
                (entity.Classname == "prop_static" || entity.Classname == "prop_dynamic"))
                Props.Add(new VMFProp
                {
                    Origin = entity["origin"].ParseVector(),
                    Rotation = entity["angles"].ParseVector() * Math.PI / 180.0,
                    Color = entity["rendercolor"],
                    Model = dropExtension(entity["model"].ToLower()),
                    Skin = StringUtil.ParseInt(entity["skin"])
                });

            entity.Accept(this);
        }
    }
}
