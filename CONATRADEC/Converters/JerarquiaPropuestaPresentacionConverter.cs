using CONATRADEC.Models;
using Microsoft.Maui.Controls;
using System.Globalization;

namespace CONATRADEC.Converters
{
    /// <summary>
    /// Presenta la clasificación simplificada del Álbum Botánico. La ficha
    /// histórica se muestra al usuario como subcategoría específica y el nivel
    /// técnico intermedio deja de formar parte de la experiencia visible.
    /// </summary>
    public sealed class JerarquiaPropuestaPresentacionConverter : IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if (value is not InspeccionFotoResultadoIAV2 resultado)
                return string.Empty;

            string opcion = parameter?.ToString()?.Trim() ?? string.Empty;

            bool categoriaExiste =
                resultado.CategoriaAlbumBotanicoIdSugerida is > 0;
            bool subcategoriaEspecificaExiste =
                resultado.AlbumBotanicoCafeIdSugerido is > 0;

            return opcion switch
            {
                "CategoriaValor" => ObtenerCategoria(resultado),
                "CategoriaEstado" => categoriaExiste
                    ? "Categoría existente"
                    : TieneTexto(resultado.CategoriaAlbumPropuesta)
                        ? "Categoría propuesta"
                        : "Pendiente de definir",

                // Compatibilidad con XAML anterior. Ambos parámetros exponen
                // ahora la subcategoría específica visible.
                "SubcategoriaValor" or "FichaValor" =>
                    ObtenerSubcategoria(resultado),
                "SubcategoriaEstado" or "FichaEstado" =>
                    subcategoriaEspecificaExiste
                        ? "Subcategoría existente"
                        : TieneTexto(resultado.ClasificacionAlbumPropuesta)
                            ? "Subcategoría propuesta"
                            : "Pendiente de definir",

                "AvisoPendiente" => ConstruirAviso(
                    categoriaExiste,
                    subcategoriaEspecificaExiste),

                _ => string.Empty
            };
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture) =>
            throw new NotSupportedException();

        private static string ObtenerCategoria(
            InspeccionFotoResultadoIAV2 resultado) =>
            TieneTexto(resultado.CategoriaAlbumPropuesta)
                ? resultado.CategoriaAlbumPropuesta.Trim()
                : "Pendiente de definir";

        private static string ObtenerSubcategoria(
            InspeccionFotoResultadoIAV2 resultado) =>
            TieneTexto(resultado.ClasificacionAlbumPropuesta)
                ? resultado.ClasificacionAlbumPropuesta.Trim()
                : "Pendiente de definir";

        private static string ConstruirAviso(
            bool categoriaExiste,
            bool subcategoriaExiste)
        {
            if (categoriaExiste && subcategoriaExiste)
            {
                return "La fotografía coincide con una categoría y una subcategoría existentes del Álbum Botánico.";
            }

            if (!categoriaExiste && !subcategoriaExiste)
            {
                return "Falta aprobar la categoría y la subcategoría propuestas.";
            }

            return !categoriaExiste
                ? "Falta aprobar la categoría propuesta."
                : "Falta aprobar la subcategoría propuesta.";
        }

        private static bool TieneTexto(string? texto) =>
            !string.IsNullOrWhiteSpace(texto);
    }
}
