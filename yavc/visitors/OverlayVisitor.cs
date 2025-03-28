using System.Collections.Generic;
using System.Linq;
using geometry.components;
using geometry.entities;
using geometry.materials;
using geometry.utils;
using NLog;
using VMFIO;

namespace yavc.visitors;

internal sealed class OverlayVisitor(string _root, IReadOnlyDictionary<int, Side> _sides) : EntityVisitor
{
    private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
    private readonly List<Overlay> _overlays = [];

    public IList<Overlay> Overlays => _overlays;

    public override void Visit(Entity entity, bool skipTools)
    {
        var classname = entity.Classname;
        if (classname == "info_overlay" && entity["sides"] != "")
        {
            var sides1 = entity["sides"].Split(' ').Select(static sideId => sideId.ParseToInt()).ToList();
            var presentSides = new List<int>();

            foreach (var side in sides1)
            {
                if (!_sides.ContainsKey(side))
                {
                    logger.Warn($"Overlay {entity["id"]} references side {side}, which does not exist");
                }
                else
                {
                    presentSides.Add(side);
                }
            }

            var vmt = VMT.GetCached(_root, entity["material"]);
            if (vmt is null)
            {
                logger.Warn($"Material {entity["material"]} in info_overlay {entity["id"]} not found");
            }
            else
            {
                var o = new Overlay
                {
                    BasisNormal = entity["BasisNormal"].ParseToVector(),
                    BasisOrigin = entity["BasisOrigin"].ParseToVector(),
                    BasisU = entity["BasisU"].ParseToVector(),
                    BasisV = entity["BasisV"].ParseToVector(),
                    TextureU = new Vector2(entity["StartU"].ParseToDouble(), entity["EndU"].ParseToDouble()),
                    TextureV = new Vector2(entity["StartV"].ParseToDouble(), entity["EndV"].ParseToDouble()),
                    Material = vmt,
                    Sides = presentSides.Select(sideId => _sides[sideId]).ToArray(),
                    UVs =
                    [
                        entity["uv0"].ParseToVector().AsVector2(), entity["uv1"].ParseToVector().AsVector2(),
                        entity["uv2"].ParseToVector().AsVector2(), entity["uv3"].ParseToVector().AsVector2(),
                    ],
                };
                _overlays.Add(o);
            }
        }

        entity.Accept(this, skipTools);
    }
}
