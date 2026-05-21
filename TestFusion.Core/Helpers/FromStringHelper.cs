using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TestFusion.Core.Helpers
{
    public static class FromStringHelper
    {
        public static int ToInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            return int.TryParse(value, out var result)
                ? result
                : 0;
        }

        public static decimal ToDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0;
        }
    }
}
