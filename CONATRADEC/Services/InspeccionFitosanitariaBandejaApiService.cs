using CONATRADEC.Models;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente exclusivo de la bandeja paginada. Se mantiene separado del
    /// servicio operativo para no alterar los endpoints usados por analizador,
    /// aprobador y detalle de la inspección.
    /// </summary>
    public sealed class InspeccionFitosanitariaBandejaApiService
    {
        private static readonly Lazy<InspeccionFitosanitariaBandejaApiService>
            lazy = new(() => new InspeccionFitosanitariaBandejaApiService());

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient client;

        public static InspeccionFitosanitariaBandejaApiService Instance =>
            lazy.Value;

        private InspeccionFitosanitariaBandejaApiService()
        {
            client = ApiClientService.Client;
        }

        public async Task<InspeccionFitosanitariaBandejaPaginaV2>
            ObtenerAsync(
                InspeccionFitosanitariaBandejaFiltroV2 filtro,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filtro);

            string ruta = ConstruirRuta(filtro);
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
                string mensaje = envelope?.Message ??
                    ExtraerMensaje(contenido);

                throw new InspeccionFitosanitariaApiException(
                    response.StatusCode,
                    string.IsNullOrWhiteSpace(mensaje)
                        ? "El servidor rechazó la búsqueda de inspecciones."
                        : mensaje);
            }

            if (envelope?.Data != null)
                return envelope.Data;

            throw new InspeccionFitosanitariaApiException(
                HttpStatusCode.BadGateway,
                "El servidor devolvió una página de inspecciones incompleta.");
        }

        private static string ConstruirRuta(
            InspeccionFitosanitariaBandejaFiltroV2 filtro)
        {
            var parametros = new List<string>();

            Agregar(parametros, "modo", filtro.Modo);
            Agregar(parametros, "buscar", filtro.Buscar);
            Agregar(parametros, "propietario", filtro.Propietario);
            Agregar(parametros, "departamento", filtro.Departamento);
            Agregar(parametros, "tipoFotografia", filtro.TipoFotografia);
            Agregar(parametros, "estado", filtro.Estado);

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

            Agregar(
                parametros,
                "desfaseHorarioMinutos",
                Math.Clamp(
                    filtro.DesfaseHorarioMinutos,
                    -840,
                    840).ToString(CultureInfo.InvariantCulture));

            if (filtro.UltimaFechaUtc.HasValue)
            {
                Agregar(
                    parametros,
                    "ultimaFechaUtc",
                    filtro.UltimaFechaUtc.Value.ToUniversalTime().ToString(
                        "O",
                        CultureInfo.InvariantCulture));
            }

            if (filtro.UltimoId.HasValue)
            {
                Agregar(
                    parametros,
                    "ultimoId",
                    filtro.UltimoId.Value.ToString(
                        CultureInfo.InvariantCulture));
            }

            Agregar(
                parametros,
                "tamanoPagina",
                Math.Clamp(filtro.TamanoPagina, 10, 50).ToString(
                    CultureInfo.InvariantCulture));

            return "api/inspecciones-fitosanitarias/bandeja-paginada?" +
                   string.Join("&", parametros);
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

        private static string ExtraerMensaje(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return string.Empty;

            try
            {
                using JsonDocument document = JsonDocument.Parse(contenido);

                if (document.RootElement.TryGetProperty(
                        "message",
                        out JsonElement message))
                {
                    return message.GetString() ?? string.Empty;
                }

                if (document.RootElement.TryGetProperty(
                        "title",
                        out JsonElement title))
                {
                    return title.GetString() ?? string.Empty;
                }
            }
            catch (JsonException)
            {
            }

            return contenido.Length <= 600
                ? contenido
                : contenido[..600];
        }

        private sealed class RespuestaApi<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }
}
