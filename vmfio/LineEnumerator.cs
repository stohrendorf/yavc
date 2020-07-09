using System;
using System.Collections.Generic;

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
            return _enumerator.MoveNext();
        }

        public string Take()
        {
            if (!_enumerator.MoveNext())
                throw new Exception();
            ++Line;
            return _enumerator.Current.Trim();
        }
    }
}
