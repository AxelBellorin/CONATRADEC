using CONATRADEC.Models;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente numerado para la bandeja del analizador. El endpoint histórico
    /// por cursor se mantiene disponible para clientes anteriores; esta versión
    /// solicita una página concreta y nunca acumula registros en memoria.
    /// </summary>
    public sealed class InspeccionFitosanitariaBandejaOperativaNumeradaApiService
    {
        private const string Ruta =
            "api/revision-fitosanitaria/bandeja-operativa-pagina";

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient client;

        public InspeccionFitosanitariaBandejaOperativaNumeradaApiService()
            : this(ApiClientService.Client)
        {
        }

        public InspeccionFitosanitariaBandejaOperativaNumeradaApiService(
            HttpClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<InspeccionFitosanitariaBandejaPaginaNumeradaV2>
            ObtenerPaginaAsync(
                string modo,
                int? tecnicoId,
                int pagina,
                int tamanoPagina = 20,
                CancellationToken cancellationToken = default)
        {
            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Clamp(tamanoPagina, 10, 50);

            var parametros = new List<string>
            {
                "modo=" + Uri.EscapeDataString(modo?.Trim() ?? string.Empty),
                "pagina=" + pagina.ToString(CultureInfo.InvariantCulture),
                "tamanoPagina=" + tamanoPagina.ToString(CultureInfo.InvariantCulture)
            };

            if (tecnicoId is > 0)
            {
                parametros.Add(
                    "tecnicoId=" +
                    tecnicoId.Value.ToString(CultureInfo.InvariantCulture));
            }

            string ruta = Ruta + "?" + string.Join("&", parametros);
            SesionInactividadService.Instance.RegistrarActividad();

            using HttpRequestMessage request = new(HttpMethod.Get, ruta);
            using HttpResponseMessage response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            string contenido = await response.Content.ReadAsStringAsync(
                cancellationToken);

            ApiEnvelopeV2<InspeccionFitosanitariaBandejaPaginaNumeradaV2>?
                envelope = null;

            if (!string.IsNullOrWhiteSpace(contenido))
            {
                try
                {
                    envelope = JsonSerializer.Deserialize<
                        ApiEnvelopeV2<InspeccionFitosanitariaBandejaPaginaNumeradaV2>>(
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
                        ? ExtraerMensaje(contenido)
                        : envelope.Message);
            }

            if (envelope is not null)
            {
                object? data = envelope.Data;
                if (data is InspeccionFitosanitariaBandejaPaginaNumeradaV2 paginaRespuesta)
                    return paginaRespuesta;
            }

            throw new InspeccionFitosanitariaApiException(
                HttpStatusCode.BadGateway,
                "El servidor devolvió una página incompleta para la bandeja del analizador.");
        }

        private static string ExtraerMensaje(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return "No fue posible cargar la bandeja del analizador.";

            try
            {
                using JsonDocument document = JsonDocument.Parse(contenido);
                if (document.RootElement.TryGetProperty(
                        "message",
                        out JsonElement message))
                {
                    string? valor = message.GetString();
                    if (!string.IsNullOrWhiteSpace(valor))
                        return valor;
                }
            }
            catch (JsonException)
            {
            }

            return contenido.Length <= 600
                ? contenido
                : contenido[..600];
        }
    }
}
