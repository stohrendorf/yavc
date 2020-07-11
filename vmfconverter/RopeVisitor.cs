using System.Collections.Generic;
using System.Linq;
using utility;
using VMFIO;

namespace VMFConverter
{
    public class RopeVisitor : EntityVisitor
    {
        private readonly Dictionary<string, RopeKeypoint> _keyPoints = new Dictionary<string, RopeKeypoint>();

        private readonly List<RopeKeypoint> _starts = new List<RopeKeypoint>();

        public IEnumerable<(RopeKeypoint, RopeKeypoint)> Segments
        {
            get
            {
                foreach (var kp in _starts.Where(_ => _.Next != null)) yield return (kp, _keyPoints[kp.Next!]);

                foreach (var kp in _keyPoints.Values.Where(_ => _.Next != null))
                    yield return (kp, _keyPoints[kp.Next!]);
            }
        }

        public override void Visit(Entity entity)
        {
            var classname = entity.Classname;
            if (classname == "move_rope" || classname == "keyframe_rope")
            {
                var name = entity.GetOptionalValue("targetname");
                var nextName = entity.GetOptionalValue("NextKey");
                var origin = ParserUtil.ParseVector(entity.GetValue("origin"));
                var slack = ParserUtil.ParseDouble(entity.GetValue("Slack"));
                var subdiv = ParserUtil.ParseInt(entity.GetValue("Subdiv"));
                var id = ParserUtil.ParseInt(entity.GetValue("id"));
                var keypoint = new RopeKeypoint(id, origin, nextName == name ? null : nextName, slack, subdiv);
                if (name == null)
                    _starts.Add(keypoint);
                else
                    _keyPoints.Add(name, keypoint);
            }

            entity.Accept(this);
        }

        public class RopeKeypoint
        {
            public readonly int ID;
            public readonly string? Next;
            public readonly Vector Origin;
            public readonly double Slack;
            public readonly int Subdiv;

            public RopeKeypoint(int id, Vector origin, string? next, double slack, int subdiv)
            {
                ID = id;
                Origin = origin;
                Next = next;
                Slack = slack;
                Subdiv = subdiv;
            }
        }
    }
}
