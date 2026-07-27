using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Completa los encabezados de análisis creados localmente utilizando el
    /// catálogo de terrenos que forma parte de Descargar todo.
    ///
    /// No realiza peticiones HTTP. Si el terreno no existe en la copia local,
    /// conserva los textos de respaldo que ya utiliza la interfaz.
    /// </summary>
    public static class AnalisisReporteLocalEnrichmentService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public static async Task<AnalisisGuardadoResumen>
            CrearResumenAsync(
                int terrenoId,
                int analisisSueloId,
                int analisisSueloCalculoId,
                string? identificador = null)
        {
            TerrenoAnalisisResponse? terreno =
                await ObtenerTerrenoAsync(terrenoId);

            return new AnalisisGuardadoResumen
            {
                AnalisisSueloId = analisisSueloId,
                AnalisisSueloCalculoId = analisisSueloCalculoId,
                IdentificadorAnalisisSuelo =
                    identificador ?? string.Empty,
                TerrenoId = terrenoId,
                CodigoTerreno = terreno?.CodigoTerreno,
                NombreTerreno = terreno?.NombreTerreno ??
                    $"Terreno #{terrenoId}",
                NombreCliente = terreno?.NombreCliente ??
                    "No disponible"
            };
        }

        public static async Task EnriquecerResumenesAsync(
            IEnumerable<AnalisisGuardadoResumen> resumenes)
        {
            List<AnalisisGuardadoResumen> items =
                resumenes?.ToList() ?? new();

            if (items.Count == 0)
                return;

            Dictionary<int, TerrenoAnalisisResponse> terrenos =
                await ObtenerTerrenosAsync();

            foreach (AnalisisGuardadoResumen item in items)
            {
                if (!terrenos.TryGetValue(
                        item.TerrenoId,
                        out TerrenoAnalisisResponse? terreno))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(terreno.CodigoTerreno) &&
                    (string.IsNullOrWhiteSpace(item.CodigoTerreno) ||
                     item.CodigoTerreno.StartsWith(
                         "LOCAL-",
                         StringComparison.OrdinalIgnoreCase)))
                {
                    item.CodigoTerreno = terreno.CodigoTerreno;
                }

                if (!string.IsNullOrWhiteSpace(terreno.NombreTerreno) &&
                    (string.IsNullOrWhiteSpace(item.NombreTerreno) ||
                     item.NombreTerreno.StartsWith(
                         "Terreno #",
                         StringComparison.OrdinalIgnoreCase)))
                {
                    item.NombreTerreno = terreno.NombreTerreno;
                }

                if (!string.IsNullOrWhiteSpace(terreno.NombreCliente) &&
                    (string.IsNullOrWhiteSpace(item.NombreCliente) ||
                     item.NombreCliente.Contains(
                         "sincronización",
                         StringComparison.OrdinalIgnoreCase) ||
                     item.NombreCliente.Equals(
                         "Disponible en el dispositivo",
                         StringComparison.OrdinalIgnoreCase)))
                {
                    item.NombreCliente = terreno.NombreCliente;
                }
            }
        }

        private static async Task<TerrenoAnalisisResponse?>
            ObtenerTerrenoAsync(int terrenoId)
        {
            Dictionary<int, TerrenoAnalisisResponse> terrenos =
                await ObtenerTerrenosAsync();

            return terrenos.TryGetValue(
                terrenoId,
                out TerrenoAnalisisResponse? terreno)
                    ? terreno
                    : null;
        }

        private static async Task<Dictionary<int,
            TerrenoAnalisisResponse>> ObtenerTerrenosAsync()
        {
            string usuarioId = Preferences.Get(
                SessionKeys.KeyUserId,
                "0");

            if (string.IsNullOrWhiteSpace(usuarioId) ||
                usuarioId == "0")
            {
                return new Dictionary<int,
                    TerrenoAnalisisResponse>();
            }

            CatalogoOfflineSeccionEntity? seccion =
                await ContenidoLocalDatabaseService.Instance
                    .ObtenerSeccionPaqueteActivoAsync(
                        usuarioId,
                        "terrenos");

            if (seccion == null ||
                string.IsNullOrWhiteSpace(seccion.Json))
            {
                return new Dictionary<int,
                    TerrenoAnalisisResponse>();
            }

            try
            {
                List<TerrenoAnalisisResponse> items =
                    JsonSerializer.Deserialize<List<
                        TerrenoAnalisisResponse>>(
                            seccion.Json,
                            JsonOptions) ?? new();

                return items
                    .Where(item =>
                        item.TerrenoId.HasValue &&
                        item.TerrenoId.Value > 0)
                    .GroupBy(item => item.TerrenoId!.Value)
                    .ToDictionary(
                        group => group.Key,
                        group => group.First());
            }
            catch
            {
                return new Dictionary<int,
                    TerrenoAnalisisResponse>();
            }
        }
    }
}
