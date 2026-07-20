using System.Globalization;

namespace CONATRADEC.Services
{
    internal static class NumeroFormularioHelper
    {
        public static bool TryParseDecimal(string? value, out decimal result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            string clean = value.Trim();

            if (decimal.TryParse(
                    clean,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out result))
            {
                return true;
            }

            if (decimal.TryParse(
                    clean,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return true;
            }

            clean = clean.Replace(',', '.');

            return decimal.TryParse(
                clean,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out result);
        }

        public static string ToText(decimal value) =>
            value.ToString("0.####", CultureInfo.InvariantCulture);
    }
}
