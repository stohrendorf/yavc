using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sprache;

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

        private static readonly Parser<char> lineComment =
            Parse.String("//")
                .Then(_ => Parse.AnyChar.Until(Parse.LineTerminator))
                .Return('/')
                .Named("line comment");

        private static readonly Parser<char> whitespaceOrLineComments =
            Parse.WhiteSpace
                .XOr(lineComment)
                .Many()
                .Return(' ')
                .Named("whitespace or comment");

        private static readonly Parser<char> escapeSequence =
            from _ in Parse.Char('\\')
            from chr in Parse.AnyChar
                .Except(Parse.Char('n'))
                .Or(Parse.Char('n').Return('\n'))
                .Named("escape sequence")
            select chr;

        private static readonly Parser<char> sChar =
            Parse.AnyChar.Except(Parse.Chars("\\\"\r\n")).Or(escapeSequence);

        private static readonly Parser<string> sCharSequence =
            sChar.Many().Named("string content").Text();

        private static readonly Parser<Token> quotedStringLiteralSingle =
            from start in Parse.Char('"')
            from content in sCharSequence
            from end in Parse.Char('"')
            from ws in whitespaceOrLineComments
            select new Token(content, true);

        private static readonly Parser<Token> quotedStringLiteralMultiTail =
            from start in Parse.Char('+')
            from content in quotedStringLiteralSingle
            select content;

        private static readonly Parser<Token> quotedStringLiteral =
            from head in quotedStringLiteralSingle
            from tail in quotedStringLiteralMultiTail.Many()
            select new Token(string.Concat(Enumerable.Repeat(head.Value, 1).Concat(tail.Select(_ => _.Value))), true);

        private static readonly Parser<Token> @operator =
            from chr in Parse.Chars("@,!+&*$.=:[](){}\\").Named("operator")
            select new Token(chr.ToString(), false);

        private static readonly Parser<Token> integer =
            from minus in Parse.Char('-').Optional()
            from digits in Parse.Digit.AtLeastOnce().Named("integer digits").Text()
            from valid in Parse.Letter.Or(Parse.Char('_')).Not()
            select new Token(minus.IsDefined ? $"-{digits}" : digits, false);

        private static readonly Parser<Token> floatingPoint =
            from minus in Parse.Char('-').Optional()
            from whole in Parse.Digit.AtLeastOnce().Text().Optional()
            from dot in Parse.Char('.')
            from fract in Parse.Digit.AtLeastOnce().Named("fractional digits").Text()
            from valid in Parse.Letter.XOr(Parse.Char('_')).Not()
            select new Token(minus.IsDefined
                ? $"-{whole.GetOrElse("")}.{fract}"
                : $"{whole.GetOrElse("")}.{fract}", false);

        private static readonly Parser<Token> number =
            floatingPoint.Or(integer).Named("number");

        private static readonly Parser<Token> identifier = (
            from first in Parse.Letter.XOr(Parse.Chars("_$"))
            from tail in Parse.AnyChar.Except(Parse.WhiteSpace).Many().Text()
            select new Token(first + tail, false)
        ).Named("identifier");

        private static readonly Parser<Token> token =
            from ws in whitespaceOrLineComments.Optional()
            from token in
                quotedStringLiteral
                    .XOr(identifier)
                    .XOr(number)
                    .XOr(@operator)
            select token;

        private static readonly Parser<IEnumerable<Token>> grammar = (
            from tokens in token.Many()
            from ws in whitespaceOrLineComments.Optional()
            select tokens
        ).End();

        [Test]
        public static void TestIdentifier()
        {
            var parsed = identifier.End().Parse("water");
            Assert.That(parsed.Value, Is.EqualTo("water"));
            Assert.That(parsed.Quoted, Is.False);
        }

        [Test]
        public static void TestWhitespace()
        {
            whitespaceOrLineComments.End().Parse("");
            whitespaceOrLineComments.End().Parse("  \t \n\t");
            whitespaceOrLineComments.End().Parse("  \t // comment123...\n   ");
            whitespaceOrLineComments.End().Parse("// comment123...");
        }

        [Test]
        public static void TestStringLiteral()
        {
            var parsed = quotedStringLiteral.End().Parse("\"abc 123...\"\n  ");
            Assert.That(parsed.Value, Is.EqualTo("abc 123..."));
            Assert.That(parsed.Quoted, Is.True);

            parsed = quotedStringLiteral.End().Parse("\"abc\"\n  +\"\\n123\\\"\\a\"");
            Assert.That(parsed.Value, Is.EqualTo("abc\n123\"a"));
            Assert.That(parsed.Quoted, Is.True);
        }

        [Test]
        public static void TestComplex()
        {
            token.End().Parse("water");
            var parsed = grammar.Parse("water{}").ToList();
            Assert.That(parsed.Count, Is.EqualTo(1));
            Assert.That(parsed[0].Value, Is.EqualTo("water{}"));
            
            parsed = grammar.Parse("water {}").ToList();
            Assert.That(parsed.Count, Is.EqualTo(3));
            Assert.That(parsed[0].Value, Is.EqualTo("water"));
            Assert.That(parsed[1].Value, Is.EqualTo("{"));
            Assert.That(parsed[2].Value, Is.EqualTo("}"));

            parsed = grammar.Parse("hello world { }").ToList();
            Assert.That(parsed.Count, Is.EqualTo(4));
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
                using var tokens = grammar.Parse(File.ReadAllText(filename)).GetEnumerator();
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
            catch (Exception e)
            {
                throw new Exception($"Exception while parsing {filename}", e);
            }

            return new Entity("<root>", new List<KeyValue>(), result);
        }
    }
}
