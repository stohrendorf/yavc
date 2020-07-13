using System.Collections.Generic;
using geometry;
using utility;
using VMFIO;

namespace VMFConverter
{
    public class DecalVisitor : EntityVisitor
    {
        private readonly List<Decal> _decals = new List<Decal>();
        private readonly string _root;

        public DecalVisitor(string root)
        {
            _root = root;
        }

        public IEnumerable<Decal> Decals => _decals;

        public override void Visit(Entity entity)
        {
            var classname = entity.Classname;
            if (classname == "infodecal")
                _decals.Add(new Decal(
                    StringUtil.ParseInt(entity.GetValue("id")),
                    entity.GetValue("origin").ParseVector(),
                    VMT.GetCached(_root, entity.GetValue("texture"))
                ));

            entity.Accept(this);
        }
    }
}
