using System.Collections.Generic;
using System.IO;
using geometry.entities;
using geometry.utils;
using NLog;
using VMFIO;

namespace yavc.visitors;

internal sealed class AmbientGenericVisitor(string _root) : EntityVisitor
{
    private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
    private readonly List<AmbientGeneric> _ambientGenerics = [];

    public IList<AmbientGeneric> AmbientGenerics => _ambientGenerics;

    public override void Visit(Entity entity, bool skipTools)
    {
        var classname = entity.Classname;
        if (classname == "ambient_generic")
        {
            if (entity.GetOptionalValue("message") is null)
            {
                logger.Warn($"ambient_generic {entity["id"]} has no message");
            }
            else
            {
                var ag = new AmbientGeneric(Path.Join(_root, entity["message"]), entity["pitch"].ParseToDouble(),
                    entity["radius"].ParseToDouble(), entity["origin"].ParseToVector());
                _ambientGenerics.Add(ag);
            }
        }

        entity.Accept(this, skipTools);
    }
}
