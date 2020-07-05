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

        private static T Take<T>(this IEnumerator<T> src) where T : class
        {
            if (!src.MoveNext())
                throw new Exception();
            return src.Current;
        }

        private static Entity ReadEntity(string typename, IEnumerator<string> it)
        {
            if (!typenamePattern.Match(typename).Success)
                throw new ArgumentException("Invalid typename", nameof(typename));

            if (it.Take().Trim() != "{")
                throw new Exception();

            var kvs = new List<KeyValue>();
            var children = new List<Entity>();
            while (true)
            {
                var currentLine = it.Take().Trim();
                if (currentLine == "}")
                    return new Entity(typename, kvs, children);

                var kvm = kvPattern.Match(currentLine);
                if (kvm.Success)
                    kvs.Add(new KeyValue(kvm.Groups[1].Value, kvm.Groups[2].Value));
                else if (typenamePattern.Match(currentLine).Success)
                    children.Add(ReadEntity(currentLine, it));
                else
                    throw new Exception($"Failed to parse line {currentLine}");
            }
        }

        public static Entity ReadVmf(string filename)
        {
            var result = new List<Entity>();
            var it = File.ReadLines(filename).GetEnumerator();
            while (it.MoveNext())
                result.Add(ReadEntity(it.Current.Trim(), it));
            return new Entity("<vmf>", new List<KeyValue>(), result);
        }
    }
}
