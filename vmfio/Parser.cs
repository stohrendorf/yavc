using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace VMFIO
{
    public static class Parser
    {
        private class Token
        {
            public readonly string Value;
            public readonly bool Quoted;

            public Token(string value, bool quoted)
            {
                Value = value;
                Quoted = quoted;
            }
        }

        private static readonly Parser<char, Unit> lineComment =
            String("//")
                .Then(_ => Any.SkipUntil(
                    OneOf(
                        End,
                        EndOfLine.IgnoreResult()
                    )
                ));

        private static readonly Parser<char, Unit> whitespaceOrLineComment =
            OneOf(
                    Whitespace.IgnoreResult(),
                    lineComment
                )
                .SkipAtLeastOnce();

        private static readonly Parser<char, char> escapeSequence =
            Char('\\')
                .Then(
                    Any.Map(c => c == 'n' ? '\n' : c)
                );

        private static readonly Parser<char, char> quotedStringCharacter =
            OneOf(
                AnyCharExcept("\\\"\r\n"),
                escapeSequence
            );

        private static readonly Parser<char, Token> quotedStringSingle =
            from content in quotedStringCharacter
                .ManyString()
                .Between(Char('"'))
                .Labelled("quoted string")
            select new Token(content, true);

        private static readonly Parser<char, Token> quotedString =
            quotedStringSingle.Before(whitespaceOrLineComment.Optional())
                .SeparatedAtLeastOnce(
                    Char('+').Before(whitespaceOrLineComment.Optional()).Labelled("concatenation operator")
                )
                .Labelled("quoted string concatenation")
                .Map(_ => new Token(string.Concat(_.Select(t => t.Value)), true));

        private static readonly Parser<char, Token> unquotedString =
            from content in Any
                .AtLeastOnceUntil(
                    OneOf(
                        End,
                        whitespaceOrLineComment
                    )
                )
                .Labelled("unquoted string")
            select new Token(string.Concat(content), false);

        private static readonly Parser<char, Token> token =
            from ws1 in whitespaceOrLineComment.SkipMany()
            from result in OneOf(quotedString, unquotedString)
            from ws2 in whitespaceOrLineComment.SkipMany()
            select result;

        private static readonly Parser<char, IEnumerable<Token>> grammar =
            token.Many().Before(End);

        [Test]
        public static void TestWhitespaceComments()
        {
            Assert.That(whitespaceOrLineComment.Optional().Before(End).Parse("").Success, Is.True);
            Assert.That(whitespaceOrLineComment.Before(End).Parse("  \t \n\t").Success, Is.True);
            Assert.That(whitespaceOrLineComment.Before(End).Parse("  \t // comment123...\n   ").Success, Is.True);
            Assert.That(whitespaceOrLineComment.Before(End).Parse("// comment123...").Success, Is.True);
            Assert.That(whitespaceOrLineComment.Before(End).Parse("// comment123...").Success, Is.True);
        }

        [Test]
        public static void TestQuotedStrings()
        {
            var parsed = quotedString.Before(End).Parse("\"abc 123...\"");
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Value.Value, Is.EqualTo("abc 123..."));
            Assert.That(parsed.Value.Quoted, Is.True);

            parsed = quotedString.Before(End).Parse("\"abc\"\n  +\"\\n123\\\"\\a\"");
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Value.Value, Is.EqualTo("abc\n123\"a"));
            Assert.That(parsed.Value.Quoted, Is.True);
        }

        [Test]
        public static void TestUnquotedStrings()
        {
            var parsed = unquotedString.Before(End).Parse("abc");
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Value.Value, Is.EqualTo("abc"));
            Assert.That(parsed.Value.Quoted, Is.False);

            parsed = unquotedString.Before(End).Parse("abc def");
            Assert.That(parsed.Success, Is.False);
        }

        [Test]
        public static void TestGrammar()
        {
            var parsed = grammar.Parse("water{}");
            Assert.That(parsed.Success, Is.True);
            var data = parsed.Value.ToList();
            Assert.That(data.Count, Is.EqualTo(1));
            Assert.That(data[0].Value, Is.EqualTo("water{}"));

            parsed = grammar.Parse("water{}");
            Assert.That(parsed.Success, Is.True);
            data = parsed.Value.ToList();
            Assert.That(data.Count, Is.EqualTo(1));

            parsed = grammar.Parse("water {}");
            Assert.That(parsed.Success, Is.True);
            data = parsed.Value.ToList();
            Assert.That(data.Count, Is.EqualTo(2));
            Assert.That(data[0].Value, Is.EqualTo("water"));
            Assert.That(data[1].Value, Is.EqualTo("{}"));

            parsed = grammar.Parse("hello world { }");
            Assert.That(parsed.Success, Is.True);
            data = parsed.Value.ToList();
            Assert.That(data.Count, Is.EqualTo(4));
            Assert.That(data[0].Value, Is.EqualTo("hello"));
            Assert.That(data[1].Value, Is.EqualTo("world"));
            Assert.That(data[2].Value, Is.EqualTo("{"));
            Assert.That(data[3].Value, Is.EqualTo("}"));

            parsed = grammar.Parse("\"abc\" \"def\"");
            Assert.That(parsed.Success, Is.True);
            data = parsed.Value.ToList();
            Assert.That(data.Count, Is.EqualTo(2));
            Assert.That(data[0].Value, Is.EqualTo("abc"));
            Assert.That(data[1].Value, Is.EqualTo("def"));
        }

        private static Token? Consume(this IEnumerator<Token> tokens)
        {
            return !tokens.MoveNext() ? null : tokens.Current;
        }

        private static Token ConsumeRequired(this IEnumerator<Token> tokens)
        {
            var result = tokens.Consume();
            if (result == null)
                throw new NullReferenceException();
            return result;
        }

        private static Entity ReadEntity(string typename, IEnumerator<Token> tokens)
        {
            var kvs = new List<KeyValue>();
            var children = new List<Entity>();
            while (true)
            {
                var first = tokens.ConsumeRequired();
                if (first.Value == "}")
                {
                    if (first.Quoted)
                        throw new Exception("Quoted '}' found");
                    return new Entity(typename, kvs, children);
                }

                var next = tokens.ConsumeRequired();

                if (next.Value == "{")
                {
                    if (next.Quoted)
                        throw new Exception("Quoted '{' found");
                    children.Add(ReadEntity(first.Value, tokens));
                }
                else
                {
                    kvs.Add(new KeyValue(first.Value, next.Value));
                }
            }
        }

        public static Entity ParseFile(string filename)
        {
            var result = new List<Entity>();
            try
            {
                Result<char, IEnumerable<Token>> parsed;
                using (var f = File.OpenText(filename))
                    parsed = grammar.Parse(f);

                if (!parsed.Success)
                {
                    throw new IOException($"Failed to parse {filename}: {parsed.Error!}");
                }

                using var tokens = parsed.Value.GetEnumerator();
                while (true)
                {
                    var typename = tokens.Consume();
                    if (typename == null)
                        break;
                    var open = tokens.ConsumeRequired();
                    if (open.Value != "{" || open.Quoted)
                        throw new Exception();
                    result.Add(ReadEntity(typename.Value, tokens));
                }
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new Exception($"Exception while parsing {filename}", e);
            }

            return new Entity("<root>", new List<KeyValue>(), result);
        }
    }
}
