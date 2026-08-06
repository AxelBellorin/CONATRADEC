using CONATRADEC.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CONATRADEC.Converters
{
    /// <summary>
    /// Construye los textos de presentación de la jerarquía propuesta según
    /// los identificadores que realmente existen en el Álbum Botánico.
    /// No modifica los valores internos ni la información enviada a la API.
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
            bool subcategoriaExiste =
                resultado.SubcategoriaAlbumBotanicoIdSugerida is > 0;
            bool fichaExiste =
                resultado.AlbumBotanicoCafeIdSugerido is > 0;

            return opcion switch
            {
                "CategoriaValor" => ObtenerCategoria(resultado),
                "CategoriaEstado" => categoriaExiste
                    ? "Categoría existente"
                    : TieneTexto(resultado.CategoriaAlbumPropuesta)
                        ? "Categoría propuesta"
                        : "Pendiente de definir",

                "SubcategoriaValor" => subcategoriaExiste
                    ? "Subcategoría identificada"
                    : "Pendiente de definir",
                "SubcategoriaEstado" => subcategoriaExiste
                    ? "Subcategoría existente"
                    : "Requiere analizador",

                "FichaValor" => ObtenerFicha(resultado),
                "FichaEstado" => fichaExiste
                    ? "Ficha existente"
                    : TieneTexto(resultado.ClasificacionAlbumPropuesta)
                        ? "Ficha propuesta"
                        : "Pendiente de definir",

                "AvisoPendiente" => ConstruirAviso(
                    categoriaExiste,
                    subcategoriaExiste,
                    fichaExiste),

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

        private static string ObtenerFicha(
            InspeccionFotoResultadoIAV2 resultado) =>
            TieneTexto(resultado.ClasificacionAlbumPropuesta)
                ? resultado.ClasificacionAlbumPropuesta.Trim()
                : "Pendiente de definir";

        private static string ConstruirAviso(
            bool categoriaExiste,
            bool subcategoriaExiste,
            bool fichaExiste)
        {
            if (categoriaExiste && subcategoriaExiste && fichaExiste)
            {
                return "La fotografía coincide con una clasificación existente del Álbum Botánico.";
            }

            var pendientes = new List<string>();

            if (!categoriaExiste)
                pendientes.Add("aprobar la categoría propuesta");

            if (!subcategoriaExiste)
                pendientes.Add("definir la subcategoría");

            if (!fichaExiste)
                pendientes.Add("aprobar la creación de la ficha propuesta");

            return $"Falta {UnirPendientes(pendientes)}.";
        }

        private static string UnirPendientes(
            IReadOnlyList<string> pendientes)
        {
            return pendientes.Count switch
            {
                0 => "completar la clasificación jerárquica",
                1 => pendientes[0],
                2 => $"{pendientes[0]} y {pendientes[1]}",
                _ => string.Join(", ", pendientes.Take(pendientes.Count - 1)) +
                     $" y {pendientes[^1]}"
            };
        }

        private static bool TieneTexto(string? texto) =>
            !string.IsNullOrWhiteSpace(texto);
    }
}
