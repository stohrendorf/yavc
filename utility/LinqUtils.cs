using System;
using System.Collections.Generic;

namespace utility
{
    public static class LinqUtils
    {
        public static T RequireNotNull<T>(this T? elem) where T:class
        {
            if(ReferenceEquals(elem, null))
                throw new NullReferenceException();

            return elem!;
        }
    }
}
