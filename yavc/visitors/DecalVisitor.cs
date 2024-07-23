using System.Collections.Generic;
using geometry.entities;
using geometry.materials;
using geometry.utils;
using NLog;
using utility;
using VMFIO;

namespace yavc.visitors;

internal sealed class DecalVisitor(string _root) : EntityVisitor
{
    private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

    private readonly List<Decal> _decals = [];

    public IReadOnlyList<Decal> Decals => _decals;

    public override void Visit(Entity entity, bool skipTools)
    {
        if (entity.Classname == "infodecal")
        {
            var vmt = VMT.GetCached(_root, entity["texture"]);
            if (vmt is null)
            {
                logger.Warn($"Material {entity["texture"]} in infodecal {entity["id"]} not found");
            }
            else
            {
                _decals.Add(new Decal(
                    StringUtil.ParseInt(entity["id"]),
                    entity["origin"].ParseToVector(),
                    vmt
                ));
            }
        }

        entity.Accept(this, skipTools);
    }
}
