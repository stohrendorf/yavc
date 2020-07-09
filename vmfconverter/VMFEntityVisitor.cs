using System;
using System.Collections.Generic;
using utility;
using VMFIO;

namespace VMFConverter
{
    public class VMFEntity
    {
        public string Color = null!;
        public string Name = null!;
        public Vector Origin;
        public Vector Rotation;
    }

    public class VMFEntityVisitor : EntityVisitor
    {
        public readonly List<VMFEntity> Entities = new List<VMFEntity>();

        private static Vector ParseVector(string data)
        {
            var cols = data.Split(" ");
            if (cols.Length != 3)
                throw new ArgumentException();

            return new Vector(
                ParserUtil.ParseDouble(cols[0]),
                ParserUtil.ParseDouble(cols[1]),
                ParserUtil.ParseDouble(cols[2])
            );
        }

        public override void Visit(Entity entity)
        {
            if (entity.Typename == "entity" &&
                (entity.Classname == "prop_static" || entity.Classname == "prop_dynamic"))
                Entities.Add(new VMFEntity
                {
                    Origin = ParseVector(entity.GetValue("origin")),
                    Rotation = ParseVector(entity.GetValue("angles")) * Math.PI / 180.0,
                    Color = entity.GetValue("rendercolor"),
                    Name = entity.GetValue("model") + "-skin" + entity.GetValue("skin")
                });

            entity.Accept(this);
        }
    }
}
