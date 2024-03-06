using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace VMFIO;

public static class Parser
{
    private static readonly Parser<char, Unit> lineComment =
        Try(String("//"))
            .Then(static _ => Any.SkipUntil(
                OneOf(
                    End,
                    EndOfLine.IgnoreResult()
                )
            ));

    public static readonly Parser<char, Unit> WhitespaceOrLineComment =
        OneOf(
                Whitespace.IgnoreResult(),
                lineComment
            )
            .SkipAtLeastOnce();

    private static readonly Parser<char, string> escapeSequence =
        Char('\\')
            .Then(
                Any.Map(static c => c switch
                {
                    'n' => "\n",
                    '"' => "\"",
                    _ => "\\" + c,
                })
            );

    private static readonly Parser<char, string> quotedStringCharacter =
        OneOf(
            AnyCharExcept("\\\"\r\n").Select(static c => c.ToString()),
            escapeSequence
        );

    private static readonly Parser<char, Token> quotedStringSingle =
        from content in quotedStringCharacter
            .ManyString()
            .Between(Char('"'))
            .Labelled("quoted string")
        select new Token(content, true);

    public static readonly Parser<char, Token> QuotedString =
        quotedStringSingle.Before(WhitespaceOrLineComment.Optional())
            .SeparatedAtLeastOnce(
                Char('+').Before(WhitespaceOrLineComment.Optional()).Labelled("concatenation operator")
            )
            .Labelled("quoted string concatenation")
            .Map(static str => new Token(string.Concat(str.Select(static t => t.Value)), true));

    public static readonly Parser<char, Token> UnquotedString =
        from content in Any
            .AtLeastOnceUntil(
                OneOf(
                    End,
                    WhitespaceOrLineComment
                )
            )
            .Labelled("unquoted string")
        select new Token(string.Concat(content), false);

    private static readonly Parser<char, Token> token =
        from ws1 in WhitespaceOrLineComment.SkipMany()
        from result in OneOf(QuotedString, UnquotedString)
        from ws2 in WhitespaceOrLineComment.SkipMany()
        select result;

    public static readonly Parser<char, IEnumerable<Token>> Grammar =
        token.Many().Before(End);

    private static Token? Consume(this IEnumerator<Token> tokens)
    {
        return !tokens.MoveNext() ? null : tokens.Current;
    }

    private static Token ConsumeRequired(this IEnumerator<Token> tokens)
    {
        var result = tokens.Consume();
        if (result is null) throw new NullReferenceException();

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
                if (first.Quoted) throw new Exception("Quoted '}' found");

                return new Entity(typename, kvs, children);
            }

            var next = tokens.ConsumeRequired();

            if (next.Value == "{")
            {
                if (next.Quoted) throw new Exception("Quoted '{' found");

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
            {
                parsed = Grammar.Parse(f);
            }

            if (!parsed.Success) throw new IOException($"Failed to parse {filename}: {parsed.Error!}");

            using var tokens = parsed.Value.GetEnumerator();
            while (true)
            {
                var typename = tokens.Consume();
                if (typename is null) break;

                var open = tokens.ConsumeRequired();
                if (open.Value != "{" || open.Quoted) throw new Exception();

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

        return new Entity("<root>", [], result);
    }

    public sealed class Token
    {
        public readonly bool Quoted;
        public readonly string Value;

        internal Token(string value, bool quoted)
        {
            Value = value;
            Quoted = quoted;
        }
    }
}
