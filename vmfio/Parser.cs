using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace VMFIO
{
    public static class Parser
    {
        private static readonly Regex kvPattern =
            new Regex(@"^(?<keyq>""?)(?<key>[^""]+)\k<keyq>(\s*""(?<value>.*)""|\s+(?<value>.+))(\s*//.*)?$",
                RegexOptions.Compiled);

        private static readonly Regex
            typenamePattern = new Regex(@"^(?<q>""?)(?<name>[^""]+)\k<q>(\s*//.*)?$", RegexOptions.Compiled);

        private static Entity ReadEntity(string typename, LineEnumerator lineEnumerator)
        {
            var typenameMatch = typenamePattern.Match(typename);
            if (!typenameMatch.Success)
                throw new ArgumentException($"Invalid typename {typename}", nameof(typename));
            typename = typenameMatch.Groups["name"].Value;

            if (lineEnumerator.Take() != "{")
                throw new Exception($"Expected {{ at line {lineEnumerator.Line}, got {lineEnumerator.Current}");

            var kvs = new List<KeyValue>();
            var children = new List<Entity>();
            while (true)
            {
                var currentLine = lineEnumerator.Take();
                if (currentLine == "}")
                    return new Entity(typename, kvs, children);

                var kvm = kvPattern.Match(currentLine);
                if (kvm.Success)
                    kvs.Add(new KeyValue(kvm.Groups["key"].Value, kvm.Groups["value"].Value));
                else if (typenamePattern.Match(currentLine).Success)
                    children.Add(ReadEntity(currentLine, lineEnumerator));
                else
                    throw new Exception($"Failed to parse line {lineEnumerator.Line} '{currentLine}'");
            }
        }

        public static Entity Parse(string filename)
        {
            var result = new List<Entity>();
            var lineEnumerator = new LineEnumerator(File.ReadLines(filename).GetEnumerator());
            try
            {
                while (lineEnumerator.MoveNext())
                    result.Add(ReadEntity(lineEnumerator.Current, lineEnumerator));
            }
            catch (Exception e)
            {
                throw new Exception($"Exception while parsing {filename}", e);
            }

            return new Entity("<root>", new List<KeyValue>(), result);
        }
    }
}
