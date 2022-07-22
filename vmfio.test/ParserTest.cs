using NUnit.Framework;
using Pidgin;
using Parser = VMFIO.Parser;

namespace vmfio.test;

public static class ParserTest
{
  [Test]
  public static void TestWhitespaceComments()
  {
    Assert.That(Parser.WhitespaceOrLineComment.Optional().Before(Parser<char>.End).Parse("").Success, Is.True);
    Assert.That(Parser.WhitespaceOrLineComment.Before(Parser<char>.End).Parse("  \t \n\t").Success, Is.True);
    Assert.That(Parser.WhitespaceOrLineComment.Before(Parser<char>.End).Parse("  \t // comment123...\n   ").Success, Is.True);
    Assert.That(Parser.WhitespaceOrLineComment.Before(Parser<char>.End).Parse("// comment123...").Success, Is.True);
    Assert.That(Parser.WhitespaceOrLineComment.Before(Parser<char>.End).Parse("// comment123...").Success, Is.True);
  }

  [Test]
  public static void TestQuotedStrings()
  {
    var parsed = Parser.QuotedString.Before(Parser<char>.End).Parse("\"abc 123...\"");
    Assert.That(parsed.Success, Is.True);
    Assert.That(parsed.Value.Value, Is.EqualTo("abc 123..."));
    Assert.That(parsed.Value.Quoted, Is.True);

    parsed = Parser.QuotedString.Before(Parser<char>.End).Parse("\"abc\"\n  +\"\\n123\\\"\\a\"");
    Assert.That(parsed.Success, Is.True);
    Assert.That(parsed.Value.Value, Is.EqualTo("abc\n123\"\\a"));
    Assert.That(parsed.Value.Quoted, Is.True);
  }

  [Test]
  public static void TestUnquotedStrings()
  {
    var parsed = Parser.UnquotedString.Before(Parser<char>.End).Parse("abc");
    Assert.That(parsed.Success, Is.True);
    Assert.That(parsed.Value.Value, Is.EqualTo("abc"));
    Assert.That(parsed.Value.Quoted, Is.False);

    parsed = Parser.UnquotedString.Before(Parser<char>.End).Parse("abc def");
    Assert.That(parsed.Success, Is.False);

    parsed = Parser.UnquotedString.Before(Parser<char>.End).Parse("some_underscore");
    Assert.That(parsed.Success, Is.True);

    parsed = Parser.UnquotedString.Before(Parser<char>.End).Parse("some/slash");
    Assert.That(parsed.Success, Is.True);

    parsed = Parser.UnquotedString.Before(Parser<char>.End).Parse("some\\backslash");
    Assert.That(parsed.Success, Is.True);
    Assert.That(parsed.Value.Value, Is.EqualTo("some\\backslash"));
  }

  [Test]
  public static void TestGrammar()
  {
    var parsed = Parser.Grammar.Parse("water{}");
    Assert.That(parsed.Success, Is.True);
    var data = parsed.Value.ToList();
    Assert.That(data.Count, Is.EqualTo(1));
    Assert.That(data[0].Value, Is.EqualTo("water{}"));

    parsed = Parser.Grammar.Parse("water{}");
    Assert.That(parsed.Success, Is.True);
    data = parsed.Value.ToList();
    Assert.That(data.Count, Is.EqualTo(1));

    parsed = Parser.Grammar.Parse("water {}");
    Assert.That(parsed.Success, Is.True);
    data = parsed.Value.ToList();
    Assert.That(data.Count, Is.EqualTo(2));
    Assert.That(data[0].Value, Is.EqualTo("water"));
    Assert.That(data[1].Value, Is.EqualTo("{}"));

    parsed = Parser.Grammar.Parse("hello world { }");
    Assert.That(parsed.Success, Is.True);
    data = parsed.Value.ToList();
    Assert.That(data.Count, Is.EqualTo(4));
    Assert.That(data[0].Value, Is.EqualTo("hello"));
    Assert.That(data[1].Value, Is.EqualTo("world"));
    Assert.That(data[2].Value, Is.EqualTo("{"));
    Assert.That(data[3].Value, Is.EqualTo("}"));

    parsed = Parser.Grammar.Parse("\"abc\" \"def\"");
    Assert.That(parsed.Success, Is.True);
    data = parsed.Value.ToList();
    Assert.That(data.Count, Is.EqualTo(2));
    Assert.That(data[0].Value, Is.EqualTo("abc"));
    Assert.That(data[1].Value, Is.EqualTo("def"));

    parsed = Parser.Grammar.Parse("$normal path\\with_underscore");
    Assert.That(parsed.Success, Is.True);
    data = parsed.Value.ToList();
    Assert.That(data.Count, Is.EqualTo(2));
    Assert.That(data[0].Value, Is.EqualTo("$normal"));
    Assert.That(data[1].Value, Is.EqualTo("path\\with_underscore"));

    parsed = Parser.Grammar.Parse("$normal \"path\\with_underscore\"");
    Assert.That(parsed.Success, Is.True);
    data = parsed.Value.ToList();
    Assert.That(data.Count, Is.EqualTo(2));
    Assert.That(data[0].Value, Is.EqualTo("$normal"));
    Assert.That(data[1].Value, Is.EqualTo("path\\with_underscore"));
  }
}
