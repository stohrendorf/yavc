using System.Globalization;

namespace utility
{
    public static class StringUtil
    {
        public static double ParseDouble(string value)
        {
            return double.Parse(value,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture);
        }

        public static int ParseInt(string value)
        {
            return int.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        }
    }
}
