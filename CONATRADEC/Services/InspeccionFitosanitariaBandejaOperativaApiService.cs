using CONATRADEC.Models;
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

        public async Task<InspeccionFitosanitariaBandejaPaginaV2>
            ObtenerPaginaAsync(
                string modo,
                int? tecnicoId,
                DateTime? ultimaFechaUtc,
                int? ultimoId,
                int tamanoPagina = 20,
                CancellationToken cancellationToken = default)
        {
            tamanoPagina = Math.Clamp(tamanoPagina, 10, 50);

            var parametros = new List<string>
            {
                "modo=" + Uri.EscapeDataString(
                    string.IsNullOrWhiteSpace(modo)
                        ? DiagnosticoIARoutes.ModoAnalizador
                        : modo.Trim().ToLowerInvariant()),
                "tamanoPagina=" + tamanoPagina,
                "desfaseHorarioMinutos=" +
                    (int)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)
                        .TotalMinutes
            };

            if (tecnicoId is > 0)
            {
                parametros.Add(
                    "tecnicoId=" + tecnicoId.Value);
            }

            if (ultimaFechaUtc.HasValue && ultimoId is > 0)
            {
                parametros.Add(
                    "ultimaFechaUtc=" + Uri.EscapeDataString(
                        ultimaFechaUtc.Value.ToUniversalTime()
                            .ToString("O")));
                parametros.Add("ultimoId=" + ultimoId.Value);
            }

            string ruta =
                "api/inspecciones-fitosanitarias/bandeja-paginada?" +
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

        private sealed class RespuestaApi<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }
}
