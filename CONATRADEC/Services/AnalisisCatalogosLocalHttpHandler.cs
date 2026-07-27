using CONATRADEC.Models;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Entrega tipos de cultivo y unidades del motor local únicamente durante
    /// una sesión sin conexión. En línea siempre consulta el backend.
    /// </summary>
    public sealed class AnalisisCatalogosLocalHttpHandler :
        DelegatingHandler
    {
        private const string RutaTiposCultivo =
            "/api/analisis-suelo/tipo-cultivo/listar";

        private const string RutaUnidadesMedida =
            "/api/unidad-medida/listar";

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Get ||
                ModoSesionService.EsEnLinea)
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            string path = ObtenerPath(request);

            if (!string.Equals(
                    path,
                    RutaTiposCultivo,
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    path,
                    RutaUnidadesMedida,
                    StringComparison.OrdinalIgnoreCase))
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            if (!DatosSinConexionPermisos.TienePermiso)
            {
                return CrearError(
                    request,
                    "Su usuario no tiene habilitados los datos sin conexión.");
            }

            MotorCalculoPaquete? paquete =
                await MotorCalculoPaqueteService.Instance
                    .ObtenerPaqueteActivoAsync(
                        cancellationToken);

            if (paquete == null)
            {
                return CrearError(
                    request,
                    "No existe un motor local completo. Inicie una sesión en línea y utilice Descargar todo.");
            }

            return string.Equals(
                    path,
                    RutaTiposCultivo,
                    StringComparison.OrdinalIgnoreCase)
                ? CrearRespuestaTiposCultivo(request, paquete)
                : CrearRespuestaUnidades(request, paquete);
        }

        private static HttpResponseMessage CrearRespuestaTiposCultivo(
            HttpRequestMessage request,
            MotorCalculoPaquete paquete)
        {
            var tipos = paquete.Contenido.TiposCultivo
                .Where(item =>
                    item != null &&
                    item.Activo &&
                    item.TipoCultivoId > 0)
                .OrderBy(item => item.NombreTipoCultivo)
                .Select(item => new
                {
                    tipoCultivoId = item.TipoCultivoId,
                    nombreTipoCultivo = Limpiar(
                        item.NombreTipoCultivo),
                    tipoCultivo = Limpiar(
                        item.NombreTipoCultivo),
                    descripcionTipoCultivo = string.Empty,
                    activo = true,
                    cantidadRangosActivos =
                        paquete.Contenido.RangosCultivo.Count(
                            rango =>
                                rango.Activo &&
                                rango.TipoCultivoId ==
                                    item.TipoCultivoId),
                    cantidadAnalisis = 0
                })
                .ToList();

            return CrearJsonResponse(request, tipos);
        }

        private static HttpResponseMessage CrearRespuestaUnidades(
            HttpRequestMessage request,
            MotorCalculoPaquete paquete)
        {
            var unidades = paquete.Contenido.Unidades
                .Where(item =>
                    item != null &&
                    item.Activo &&
                    item.UnidadMedidaId > 0)
                .OrderBy(item => item.NombreUnidadMedida)
                .Select(item =>
                {
                    string nombre = Limpiar(
                        item.NombreUnidadMedida);

                    return new
                    {
                        unidadMedidaId = item.UnidadMedidaId,
                        nombreUnidadMedida = nombre,
                        descripcionUnidadMedida = string.Empty,
                        simboloUnidadMedida = nombre,
                        abreviaturaUnidadMedida = nombre,
                        activo = true
                    };
                })
                .ToList();

            return CrearJsonResponse(request, unidades);
        }

        private static HttpResponseMessage CrearJsonResponse<T>(
            HttpRequestMessage request,
            T data)
        {
            string json = JsonSerializer.Serialize(
                data,
                JsonOptions);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json")
            };

            response.Headers.TryAddWithoutValidation(
                "X-Datos-Origen",
                "motor-local");

            return response;
        }

        private static HttpResponseMessage CrearError(
            HttpRequestMessage request,
            string message) =>
            new(HttpStatusCode.ServiceUnavailable)
            {
                RequestMessage = request,
                Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        success = false,
                        message
                    }),
                    Encoding.UTF8,
                    "application/json")
            };

        private static string ObtenerPath(
            HttpRequestMessage request)
        {
            Uri? uri = request.RequestUri;
            if (uri == null)
                return string.Empty;

            if (uri.IsAbsoluteUri)
                return uri.AbsolutePath;

            string raw = uri.OriginalString;
            int query = raw.IndexOf('?');
            if (query >= 0)
                raw = raw[..query];

            return "/" + raw.TrimStart('/');
        }

        private static string Limpiar(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
    }
}
