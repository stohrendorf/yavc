using System.Collections.Generic;
using geometry.entities;
using geometry.materials;
using geometry.utils;
using utility;
using VMFIO;

namespace yavc.visitors
{
    public class DecalVisitor : EntityVisitor
    {
        private readonly List<Decal> _decals = new List<Decal>();
        private readonly string _root;

        public DecalVisitor(string root)
        {
            _root = root;
        }

        public IReadOnlyList<Decal> Decals => _decals;

        public override void Visit(Entity entity)
        {
            var classname = entity.Classname;
            if (classname == "infodecal")
                _decals.Add(new Decal(
                    StringUtil.ParseInt(entity["id"]),
                    entity["origin"].ParseVector(),
                    VMT.GetCached(_root, entity["texture"]).RequireNotNull()
                ));

            entity.Accept(this);
        }
    }
}
