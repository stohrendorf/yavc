using NUnit.Framework;

namespace geometry.utils;

[TestFixture]
public static class ParserUtilTest
{
  [Test]
  public static void TestDeterministicParsing()
  {
    var p1 = "(0 1 0) (0 0 0) (0 0 1)".ParsePlaneString();
    var p2 = "(0 10 0) (0 0 0) (0 0 1)".ParsePlaneString();
    var p3 = "(0 1 0) (0 0 0) (0 0 10)".ParsePlaneString();
    Assert.That(p1, Is.EqualTo(p2));
    Assert.That(p1, Is.EqualTo(p3));
  }
}