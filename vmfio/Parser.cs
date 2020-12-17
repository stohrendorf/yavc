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
        public interface IToken
        {
        }

        public class TokenString : IToken
        {
            public readonly string Value;

            public TokenString(string value)
            {
                Value = value;
            }
        }

        public class TokenOperator : IToken
        {
            public readonly char Value;

            public TokenOperator(char value)
            {
                Value = value;
            }
        }

        private static readonly Parser<char> lineComment =
            Parse.String("//")
                .Then(_ => Parse.AnyChar.Until(Parse.LineTerminator))
                .Return('/')
                .Named("line comment");

        private static readonly Parser<char> whitespaceOrLineComments =
            Parse.WhiteSpace
                .Or(lineComment)
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

        private static readonly Parser<TokenString> sCharSequence =
            from content in sChar.Many().Named("string literal").Text()
            select new TokenString(content);

        private static readonly Parser<TokenString> stringLiteralSingle =
            from start in Parse.Char('"')
            from content in sCharSequence
            from end in Parse.Char('"')
            from ws in whitespaceOrLineComments
            select content;

        private static readonly Parser<TokenString> stringLiteralMultiTail =
            from start in Parse.Char('+')
            from content in stringLiteralSingle
            select content;

        private static readonly Parser<IToken> stringLiteral =
            from head in stringLiteralSingle
            from tail in stringLiteralMultiTail.Many()
            select new TokenString(string.Concat(Enumerable.Repeat(head.Value, 1).Concat(tail.Select(_ => _.Value))));

        private static readonly Parser<TokenOperator> @operator =
            from chr in Parse.Chars("@,!+&*$.=:[](){}\\").Named("operator")
            select new TokenOperator(chr);

        private static readonly Parser<TokenString> integer =
            from minus in Parse.Char('-').Optional()
            from digits in Parse.Digit.AtLeastOnce().Named("integer digits").Text()
            from valid in Parse.Letter.Or(Parse.Char('_')).Not()
            select new TokenString(minus.IsDefined ? $"-{digits}" : digits);

        private static readonly Parser<TokenString> floatingPoint =
            from minus in Parse.Char('-').Optional()
            from whole in Parse.Digit.AtLeastOnce().Text().Optional()
            from dot in Parse.Char('.')
            from fract in Parse.Digit.AtLeastOnce().Named("fractional digits").Text()
            from valid in Parse.Letter.Or(Parse.Char('_')).Not()
            select new TokenString(minus.IsDefined
                ? $"-{whole.GetOrElse("")}.{fract}"
                : $"{whole.GetOrElse("")}.{fract}");

        private static readonly Parser<TokenString> number =
            floatingPoint.Or(integer).Named("number");

        private static readonly Parser<IToken> identifier = (
            from first in Parse.Letter.Or(Parse.Chars("_$"))
            from tail in Parse.AnyChar.Except(Parse.WhiteSpace).Many().Text()
            select new TokenString(first + tail)
        ).Named("identifier");

        private static readonly Parser<IToken> token =
            from ws in whitespaceOrLineComments.Optional()
            from token in
                stringLiteral
                    .Or(identifier)
                    .Or(number)
                    .Or(@operator)
            select token;

        private static readonly Parser<IEnumerable<IToken>> grammar = (
            from tokens in token.Many()
            from ws in whitespaceOrLineComments.Optional()
            select tokens
        ).End();

        [Test]
        public static void TestIdentifier()
        {
            var parsed = identifier.End().Parse("water");
            Assert.That(parsed, Is.TypeOf<TokenString>());
            Assert.That(((TokenString) parsed).Value, Is.EqualTo("water"));
            Assert.Throws<ParseException>(() => identifier.End().Parse("_some123Identifier**"));
        }

        [Test]
        public static void TestInteger()
        {
            var parsed = integer.End().Parse("123");
            Assert.That(parsed.Value, Is.EqualTo("123"));
            Assert.Throws<ParseException>(() => integer.End().Parse("123_"));
            parsed = (from i in integer from o in @operator select i).End().Parse("123*");
            Assert.That(parsed.Value, Is.EqualTo("123"));
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
            var parsed = stringLiteral.End().Parse("\"abc 123...\"\n  ");
            Assert.That(parsed, Is.TypeOf<TokenString>());
            Assert.That(((TokenString) parsed).Value, Is.EqualTo("abc 123..."));

            parsed = stringLiteral.End().Parse("\"abc\"\n  +\"\\n123\\\"\\a\"");
            Assert.That(parsed, Is.TypeOf<TokenString>());
            Assert.That(((TokenString) parsed).Value, Is.EqualTo("abc\n123\"a"));
        }

        [Test]
        public static void TestComplex()
        {
            token.End().Parse("water");
            grammar.Parse("water{}");
        }

        private static T? Consume<T>(this IEnumerator<IToken> tokens) where T : class, IToken
        {
            tokens.MoveNext();
            var result = tokens.Current;
            if (result == null)
                return null;

            if (!(result is T casted))
                throw new Exception();

            return casted;
        }

        private static T ConsumeRequired<T>(this IEnumerator<IToken> tokens) where T : class, IToken
        {
            var result = tokens.Consume<T>();
            if (result == null)
                throw new NullReferenceException();
            return result;
        }

        private static Entity ReadEntity(string typename, IEnumerator<IToken> tokens)
        {
            var kvs = new List<KeyValue>();
            var children = new List<Entity>();
            while (true)
            {
                var first = tokens.ConsumeRequired<IToken>();

                switch (first)
                {
                    case TokenOperator {Value: '}'}:
                        return new Entity(typename, kvs, children);
                    case TokenOperator firstOp:
                        throw new Exception($"Invalid operator {firstOp.Value}");
                }

                var next = tokens.ConsumeRequired<IToken>();

                if (next is TokenOperator op)
                {
                    if (op.Value != '{')
                        throw new Exception();
                    children.Add(ReadEntity(((TokenString) first).Value, tokens));
                }
                else
                {
                    kvs.Add(new KeyValue(((TokenString) first).Value, ((TokenString) next).Value));
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
                    var typename = tokens.Consume<TokenString>();
                    if (typename == null)
                        break;
                    var open = tokens.ConsumeRequired<TokenOperator>();
                    if (open.Value != '{')
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
