using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace VMFIO
{
    public static class Parser
    {
        private static readonly Regex kvPattern = new Regex("^\"([^\"]+)\" \"([^\"]*)\"$", RegexOptions.Compiled);
        private static readonly Regex typenamePattern = new Regex("^[a-z_][a-z0-9_]+$", RegexOptions.Compiled);

        private static Entity ReadEntity(string typename, LineEnumerator lineEnumerator)
        {
            if (!typenamePattern.Match(typename).Success)
                throw new ArgumentException("Invalid typename", nameof(typename));

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
                    kvs.Add(new KeyValue(kvm.Groups[1].Value, kvm.Groups[2].Value));
                else if (typenamePattern.Match(currentLine).Success)
                    children.Add(ReadEntity(currentLine, lineEnumerator));
                else
                    throw new Exception($"Failed to parse line {lineEnumerator.Line} '{currentLine}'");
            }
        }

        public static Entity ReadVmf(string filename)
        {
            var result = new List<Entity>();
            var lineEnumerator = new LineEnumerator(File.ReadLines(filename).GetEnumerator());
            while (lineEnumerator.MoveNext())
                result.Add(ReadEntity(lineEnumerator.Current, lineEnumerator));
            return new Entity("<vmf>", new List<KeyValue>(), result);
        }
    }
}
