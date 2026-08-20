using CONATRADEC.Models;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente numerado para las bandejas operativas de Analizador y Aprobador.
    /// El endpoint histórico por cursor permanece disponible para clientes
    /// anteriores; esta versión solicita una página concreta y nunca acumula
    /// registros en memoria.
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

        /// <summary>
        /// Sobrecarga conservada para Analizador. Internamente usa el mismo
        /// contrato completo empleado por Aprobador.
        /// </summary>
        public Task<InspeccionFitosanitariaBandejaPaginaNumeradaV2>
            ObtenerPaginaAsync(
                string modo,
                int? tecnicoId,
                int pagina,
                int tamanoPagina = 20,
                CancellationToken cancellationToken = default)
        {
            var filtro = new InspeccionFitosanitariaBandejaFiltroV2
            {
                Modo = modo ?? string.Empty,
                TecnicoId = tecnicoId,
                TamanoPagina = tamanoPagina
            };

            return ObtenerPaginaAsync(
                filtro,
                pagina,
                tamanoPagina,
                cancellationToken);
        }

        /// <summary>
        /// Consulta una página concreta usando únicamente los filtros ya
        /// aplicados por el ViewModel. La edición de controles en pantalla no
        /// alcanza este método hasta que el usuario ejecuta Buscar.
        /// </summary>
        public async Task<InspeccionFitosanitariaBandejaPaginaNumeradaV2>
            ObtenerPaginaAsync(
                InspeccionFitosanitariaBandejaFiltroV2 filtro,
                int pagina,
                int tamanoPagina = 20,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filtro);

            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Clamp(tamanoPagina, 10, 50);

            var parametros = new List<string>
            {
                "modo=" + Uri.EscapeDataString(filtro.Modo?.Trim() ?? string.Empty),
                "pagina=" + pagina.ToString(CultureInfo.InvariantCulture),
                "tamanoPagina=" + tamanoPagina.ToString(CultureInfo.InvariantCulture),
                "desfaseHorarioMinutos=" + filtro.DesfaseHorarioMinutos
                    .ToString(CultureInfo.InvariantCulture)
            };

            AgregarTexto(parametros, "buscar", filtro.Buscar);
            AgregarTexto(parametros, "propietario", filtro.Propietario);
            AgregarTexto(parametros, "departamento", filtro.Departamento);
            AgregarTexto(parametros, "tipoFotografia", filtro.TipoFotografia);
            AgregarTexto(parametros, "estado", filtro.Estado);

            if (filtro.TecnicoId is > 0)
            {
                parametros.Add(
                    "tecnicoId=" +
                    filtro.TecnicoId.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (filtro.FechaDesde.HasValue)
            {
                parametros.Add(
                    "fechaDesde=" + Uri.EscapeDataString(
                        filtro.FechaDesde.Value.Date.ToString(
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture)));
            }

            if (filtro.FechaHasta.HasValue)
            {
                parametros.Add(
                    "fechaHasta=" + Uri.EscapeDataString(
                        filtro.FechaHasta.Value.Date.ToString(
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture)));
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
                "El servidor devolvió una página incompleta para la bandeja operativa.");
        }

        private static void AgregarTexto(
            ICollection<string> parametros,
            string nombre,
            string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;

            parametros.Add(
                nombre + "=" + Uri.EscapeDataString(valor.Trim()));
        }

        private static string ExtraerMensaje(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return "No fue posible cargar la bandeja operativa.";

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
