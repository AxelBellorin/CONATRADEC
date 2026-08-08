using CONATRADEC.Models;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente paginado para las bandejas del analizador y aprobador. Utiliza
    /// cursor por fecha e identificador, por lo que no vuelve a leer las páginas
    /// anteriores al solicitar más registros.
    /// </summary>
    public sealed class InspeccionFitosanitariaBandejaOperativaApiService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient client = ApiClientService.Client;

        public Task<InspeccionFitosanitariaBandejaPaginaV2>
            ObtenerPaginaAsync(
                string modo,
                int? tecnicoId,
                DateTime? ultimaFechaUtc,
                int? ultimoId,
                int tamanoPagina = 20,
                CancellationToken cancellationToken = default) =>
            ObtenerPaginaAsync(
                new InspeccionFitosanitariaBandejaFiltroV2
                {
                    Modo = modo,
                    TecnicoId = tecnicoId,
                    UltimaFechaUtc = ultimaFechaUtc,
                    UltimoId = ultimoId,
                    TamanoPagina = tamanoPagina,
                    DesfaseHorarioMinutos = (int)TimeZoneInfo.Local
                        .GetUtcOffset(DateTime.Now)
                        .TotalMinutes
                },
                cancellationToken);

        public async Task<InspeccionFitosanitariaBandejaPaginaV2>
            ObtenerPaginaAsync(
                InspeccionFitosanitariaBandejaFiltroV2 filtro,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filtro);

            int tamanoPagina = Math.Clamp(filtro.TamanoPagina, 10, 50);
            var parametros = new List<string>();

            Agregar(parametros, "modo", filtro.Modo);
            Agregar(parametros, "buscar", filtro.Buscar);
            Agregar(parametros, "propietario", filtro.Propietario);
            Agregar(parametros, "departamento", filtro.Departamento);
            Agregar(parametros, "tipoFotografia", filtro.TipoFotografia);
            Agregar(parametros, "estado", filtro.Estado);

            if (filtro.TecnicoId is > 0)
            {
                Agregar(
                    parametros,
                    "tecnicoId",
                    filtro.TecnicoId.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (filtro.FechaDesde.HasValue)
            {
                Agregar(
                    parametros,
                    "fechaDesde",
                    filtro.FechaDesde.Value.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture));
            }

            if (filtro.FechaHasta.HasValue)
            {
                Agregar(
                    parametros,
                    "fechaHasta",
                    filtro.FechaHasta.Value.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture));
            }

            int desfase = filtro.DesfaseHorarioMinutos;
            if (desfase == 0)
            {
                desfase = (int)TimeZoneInfo.Local
                    .GetUtcOffset(DateTime.Now)
                    .TotalMinutes;
            }

            Agregar(
                parametros,
                "desfaseHorarioMinutos",
                Math.Clamp(desfase, -840, 840)
                    .ToString(CultureInfo.InvariantCulture));

            if (filtro.UltimaFechaUtc.HasValue && filtro.UltimoId is > 0)
            {
                Agregar(
                    parametros,
                    "ultimaFechaUtc",
                    filtro.UltimaFechaUtc.Value.ToUniversalTime()
                        .ToString("O", CultureInfo.InvariantCulture));
                Agregar(
                    parametros,
                    "ultimoId",
                    filtro.UltimoId.Value.ToString(CultureInfo.InvariantCulture));
            }

            Agregar(
                parametros,
                "tamanoPagina",
                tamanoPagina.ToString(CultureInfo.InvariantCulture));

            string ruta =
                "api/revision-fitosanitaria/bandeja-paginada?" +
                string.Join("&", parametros);

            SesionInactividadService.Instance.RegistrarActividad();

            using HttpRequestMessage request = new(HttpMethod.Get, ruta);
            using HttpResponseMessage response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            string contenido = await response.Content.ReadAsStringAsync(
                cancellationToken);

            RespuestaApi<InspeccionFitosanitariaBandejaPaginaV2>? envelope =
                null;

            if (!string.IsNullOrWhiteSpace(contenido))
            {
                try
                {
                    envelope = JsonSerializer.Deserialize<
                        RespuestaApi<InspeccionFitosanitariaBandejaPaginaV2>>(
                        contenido,
                        JsonOptions);
                }
                catch (JsonException)
                {
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new InspeccionFitosanitariaApiException(
                    response.StatusCode,
                    string.IsNullOrWhiteSpace(envelope?.Message)
                        ? "No fue posible cargar la bandeja de inspecciones."
                        : envelope.Message);
            }

            if (envelope?.Data != null)
                return envelope.Data;

            throw new InspeccionFitosanitariaApiException(
                HttpStatusCode.BadGateway,
                "El servidor devolvió una página de bandeja incompleta.");
        }

        private static void Agregar(
            ICollection<string> parametros,
            string nombre,
            string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;

            parametros.Add(
                Uri.EscapeDataString(nombre) + "=" +
                Uri.EscapeDataString(valor.Trim()));
        }

        private sealed class RespuestaApi<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }
}
