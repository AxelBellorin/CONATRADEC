using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace CONATRADEC.Converters
{
    /// <summary>
    /// Convierte códigos internos como PLANTA_COMPLETA en textos legibles
    /// para el usuario, sin modificar el valor original utilizado por la API.
    /// </summary>
    public sealed class CodigoLegibleConverter : IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            string prefijo = parameter?.ToString() ?? string.Empty;
            string codigo = value?.ToString()?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(codigo))
                return prefijo + "No especificado";

            string texto = codigo
                .Replace('_', ' ')
                .ToLower(culture);

            texto = char.ToUpper(texto[0], culture) + texto[1..];

            return prefijo + texto;
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
