using System;
using System.Globalization;

namespace CRS.Core
{
    /// <summary>Same number format on every device and language: $12 400, 145 761.</summary>
    public static class MoneyFormat
    {
        public static string Number(long value) =>
            value.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ');

        public static string Format(long amount) =>
            (amount < 0 ? "-$" : "$") + Number(Math.Abs(amount));
    }
}
