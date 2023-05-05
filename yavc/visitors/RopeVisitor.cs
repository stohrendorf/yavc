using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using geometry.components;
using geometry.utils;
using NLog;
using VMFIO;

namespace yavc.visitors;

internal class RopeVisitor : EntityVisitor
{
  private const int RopeSegmentFactor = 4;
  private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
  private readonly Dictionary<string, RopeKeyPoint> _keyPoints = new();

  private readonly List<RopeKeyPoint> _starts = new();

  public IEnumerable<(string, Vector, List<Vector>)> Chains
  {
    get
    {
      var allReferences = _keyPoints.Values.Where(static _ => _.Next != null).Select(static _ => _.Next!)
        .Concat(_starts.Where(static s => s.Next != null).Select(static s => s.Next!)).ToHashSet();
      var namedStarts = _keyPoints.Where(kv => !allReferences.Contains(kv.Key)).Select(static kv => kv.Value);
      foreach (var start in _starts.Where(static _ => _.Next != null).Concat(namedStarts))
      {
        if (start.Next == null)
        {
          logger.Warn($"Rope start {start.ID} at {start.Origin} has no following keypoint");
          continue;
        }

        var currentChain = new List<Vector>();
        var pt = start;
        var visited = new HashSet<int>();
        Vector? p0 = null;
        while (pt.Next != null)
        {
          if (!_keyPoints.TryGetValue(pt.Next, out var next))
          {
            logger.Warn($"Rope keypoint {pt.Next} not found");
            break;
          }

          if (visited.Contains(next.ID))
          {
            logger.Warn($"Circular rope detected (containing rope keypoint {pt.ID})");
            break;
          }

          visited.Add(next.ID);

          var points = Catenary.Calculate(pt.Origin, next.Origin, pt.AdditionalLength,
            (pt.Subdivision + 1) * RopeSegmentFactor);

          if (!ReferenceEquals(pt, start))
            points = points
              .Skip(1); // the first point of any segment equals the last point of the previous segments
          else
            p0 = start.Origin;

          Debug.Assert(p0 != null);

          currentChain.AddRange(points.Select(_ => _ - p0.Value));
          pt = next;
        }

        Debug.Assert(p0 != null);
        yield return (start.Name ?? $"rope-{start.ID}", p0.Value, currentChain);
      }
    }
  }

  public object Count => _starts.Count + _keyPoints.Count;

  public override void Visit(Entity entity)
  {
    var classname = entity.Classname;
    if (classname is "move_rope" or "keyframe_rope")
    {
      var name = entity.GetOptionalValue("targetname");
      var nextName = entity.GetOptionalValue("NextKey");
      var origin = entity["origin"].ParseVector();
      var additionalLength = entity["Slack"].ParseDouble() / 16.0;
      var subdivision = entity["Subdiv"].ParseInt();
      var id = entity["id"].ParseInt();
      var keyPoint = new RopeKeyPoint(id, origin, name, nextName == name ? null : nextName, additionalLength,
        subdivision);
      if (name == null)
        _starts.Add(keyPoint);
      else
        _keyPoints.Add(name, keyPoint);
    }

    entity.Accept(this);
  }

  private sealed class RopeKeyPoint
  {
    public readonly double AdditionalLength;
    public readonly int ID;
    public readonly string? Name;
    public readonly string? Next;
    public readonly Vector Origin;
    public readonly int Subdivision;

    public RopeKeyPoint(int id, Vector origin, string? name, string? next, double additionalLength,
      int subdivision)
    {
      ID = id;
      Origin = origin;
      Name = name;
      Next = next;
      AdditionalLength = additionalLength;
      Subdivision = subdivision;
    }
  }
}
