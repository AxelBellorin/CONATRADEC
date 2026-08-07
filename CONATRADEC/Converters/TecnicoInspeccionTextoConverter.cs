using CONATRADEC.Models;
using Microsoft.Maui.Controls;
using System.Globalization;

namespace CONATRADEC.Converters
{
    /// <summary>
    /// Muestra el técnico responsable a partir del identificador de la
    /// inspección. La relación se carga en bloque al abrir la bandeja.
    /// </summary>
    public sealed class TecnicoInspeccionTextoConverter : IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            return int.TryParse(value?.ToString(), out int inspeccionId) &&
                   inspeccionId > 0
                ? TecnicoInspeccionCacheService.ObtenerTexto(inspeccionId)
                : "Usuario no disponible";
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
