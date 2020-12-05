using System.Collections.Generic;
using System.Linq;
using geometry.components;
using geometry.entities;
using geometry.materials;
using geometry.utils;
using NLog;
using utility;
using VMFIO;

namespace yavc.visitors
{
    public class OverlayVisitor : EntityVisitor
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly List<Overlay> _overlays = new List<Overlay>();
        private readonly string _root;
        private readonly IReadOnlyDictionary<int, Side> _sides;

        public OverlayVisitor(string root, IReadOnlyDictionary<int, Side> sides)
        {
            _root = root;
            _sides = sides;
        }

        public IList<Overlay> Overlays => _overlays;

        public override void Visit(Entity entity)
        {
            var classname = entity.Classname;
            if (classname == "info_overlay")
            {
                var sides = entity["sides"].Split(' ').Select(_ => _.ParseInt()).ToList();
                var presentSides = new List<int>();

                foreach (var side in sides)
                    if (!_sides.ContainsKey(side))
                        logger.Warn($"Overlay {entity["id"]} references side {side}, which does not exist");
                    else
                        presentSides.Add(side);
                var o = new Overlay
                {
                    BasisNormal = entity["BasisNormal"].ParseVector(),
                    BasisOrigin = entity["BasisOrigin"].ParseVector(),
                    BasisU = entity["BasisU"].ParseVector(),
                    BasisV = entity["BasisV"].ParseVector(),
                    TextureU = new Vector2(entity["StartU"].ParseDouble(), entity["EndU"].ParseDouble()),
                    TextureV = new Vector2(entity["StartV"].ParseDouble(), entity["EndV"].ParseDouble()),
                    Material = VMT.GetCached(_root, entity["material"]).RequireNotNull(),
                    Sides = presentSides.Select(sideId => _sides[sideId]).ToArray(),
                    UVs = new[]
                    {
                        entity["uv0"].ParseVector().AsVector2(), entity["uv1"].ParseVector().AsVector2(),
                        entity["uv2"].ParseVector().AsVector2(), entity["uv3"].ParseVector().AsVector2()
                    }
                };
                _overlays.Add(o);
            }

            entity.Accept(this);
        }
    }
}
