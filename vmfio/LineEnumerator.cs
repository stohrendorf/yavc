using System.Collections.Generic;
using System.IO;

namespace VMFIO
{
    public class LineEnumerator
    {
        private readonly IEnumerator<string> _enumerator;

        public LineEnumerator(IEnumerator<string> enumerator)
        {
            _enumerator = enumerator;
        }

        public int Line { get; private set; }

        public string Current => _enumerator.Current.Trim();

        public bool MoveNext()
        {
            do
            {
                if (!_enumerator.MoveNext())
                    return false;
                ++Line;
            } while (Current == string.Empty || Current.StartsWith("//"));

            return true;
        }

        public string Take()
        {
            if (!MoveNext())
                throw new EndOfStreamException($"Unexpected end of file at line {Line}");

            return Current;
        }
    }
}
