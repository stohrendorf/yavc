using System.Collections.Generic;
using System.Linq;

namespace utility;

public static class TestingUtil
{
  public static IEnumerable<double> NotInRange(this IEnumerable<double> source, double min, double max,
    double tolerance = 1e-4)
  {
    return source.Where(v => v < min - tolerance || v > max + tolerance);
  }
}