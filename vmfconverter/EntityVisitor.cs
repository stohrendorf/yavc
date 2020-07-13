using System;
using System.Collections.Generic;
using geometry;
using utility;
using VMFIO;

namespace VMFConverter
{
    public class VMFEntity
    {
        public string Color = null!;
        public string Model = null!;
        public Vector Origin;
        public Vector Rotation;
        public int Skin;
    }

    public class EntityVisitor : VMFIO.EntityVisitor
    {
        public readonly List<VMFEntity> Entities = new List<VMFEntity>();

        public override void Visit(Entity entity)
        {
            static string dropExtension(string p)
            {
                var dotIdx = p.LastIndexOf('.');
                return dotIdx < 0 ? p : p.Substring(0, dotIdx);
            }

            if (entity.Typename == "entity" &&
                (entity.Classname == "prop_static" || entity.Classname == "prop_dynamic"))
                Entities.Add(new VMFEntity
                {
                    Origin = entity.GetValue("origin").ParseVector(),
                    Rotation = entity.GetValue("angles").ParseVector() * Math.PI / 180.0,
                    Color = entity.GetValue("rendercolor"),
                    Model = dropExtension(entity.GetValue("model").ToLower()),
                    Skin = StringUtil.ParseInt(entity.GetValue("skin"))
                });

            entity.Accept(this);
        }
    }
}
