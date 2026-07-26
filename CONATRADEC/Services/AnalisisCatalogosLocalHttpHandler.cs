using CONATRADEC.Models;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Entrega al formulario de Nuevo análisis los tipos de cultivo y las
    /// unidades de medida incluidos en el mismo paquete del motor local.
    ///
    /// Estas dos rutas históricas no coinciden con las rutas administrativas
    /// reconocidas por CatalogosLocalHttpHandler:
    ///
    /// api/analisis-suelo/tipo-cultivo/listar
    /// api/unidad-medida/listar
    ///
    /// Al trabajar sin conexión se responde directamente desde el motor
    /// descargado, evitando mezclar sus reglas con catálogos de otra versión.
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
                !DatosSinConexionPermisos.TienePermiso ||
                !DebeUtilizarMotorLocal())
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            string path =
                ObtenerPath(request);

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

            MotorCalculoPaquete? paquete =
                await MotorCalculoPaqueteService.Instance
                    .ObtenerPaqueteActivoAsync(
                        cancellationToken);

            if (paquete == null)
            {
                /*
                 * El botón Nuevo análisis ya valida que exista el motor.
                 * Se conserva el flujo normal como respaldo por si el archivo
                 * fue eliminado mientras la página se estaba abriendo.
                 */
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            if (string.Equals(
                    path,
                    RutaTiposCultivo,
                    StringComparison.OrdinalIgnoreCase))
            {
                return CrearRespuestaTiposCultivo(
                    request,
                    paquete);
            }

            return CrearRespuestaUnidades(
                request,
                paquete);
        }

        private static bool DebeUtilizarMotorLocal()
        {
            if (SesionOfflineService.SesionActualEsOffline)
                return true;

            if (!EstadoConexionService.Instance.HayInternet)
                return true;

            return ModoTrabajoAnalisisService
                .Instance
                .EstadoActual
                .Modo ==
                ModoTrabajoAnalisis.SinConexion;
        }

        private static HttpResponseMessage
            CrearRespuestaTiposCultivo(
                HttpRequestMessage request,
                MotorCalculoPaquete paquete)
        {
            var tipos =
                paquete.Contenido.TiposCultivo
                    .Where(item =>
                        item != null &&
                        item.Activo &&
                        item.TipoCultivoId > 0)
                    .OrderBy(item =>
                        item.NombreTipoCultivo)
                    .Select(item => new
                    {
                        tipoCultivoId =
                            item.TipoCultivoId,

                        nombreTipoCultivo =
                            Limpiar(
                                item.NombreTipoCultivo),

                        /*
                         * El formulario todavía admite la propiedad histórica
                         * TipoCultivo para mostrar el nombre.
                         */
                        tipoCultivo =
                            Limpiar(
                                item.NombreTipoCultivo),

                        descripcionTipoCultivo =
                            string.Empty,

                        activo =
                            true,

                        cantidadRangosActivos =
                            paquete.Contenido.RangosCultivo.Count(
                                rango =>
                                    rango.Activo &&
                                    rango.TipoCultivoId ==
                                        item.TipoCultivoId),

                        cantidadAnalisis =
                            0
                    })
                    .ToList();

            return CrearJsonResponse(
                request,
                tipos);
        }

        private static HttpResponseMessage
            CrearRespuestaUnidades(
                HttpRequestMessage request,
                MotorCalculoPaquete paquete)
        {
            var unidades =
                paquete.Contenido.Unidades
                    .Where(item =>
                        item != null &&
                        item.Activo &&
                        item.UnidadMedidaId > 0)
                    .OrderBy(item =>
                        item.NombreUnidadMedida)
                    .Select(item =>
                    {
                        string nombre =
                            Limpiar(
                                item.NombreUnidadMedida);

                        return new
                        {
                            unidadMedidaId =
                                item.UnidadMedidaId,

                            nombreUnidadMedida =
                                nombre,

                            descripcionUnidadMedida =
                                string.Empty,

                            /*
                             * El catálogo base del backend solamente conserva
                             * el nombre. Se replica como símbolo y abreviatura
                             * para que las búsquedas de %, PPM, meq/100g, etc.
                             * continúen funcionando dentro del formulario.
                             */
                            simboloUnidadMedida =
                                nombre,

                            abreviaturaUnidadMedida =
                                nombre,

                            activo =
                                true
                        };
                    })
                    .ToList();

            return CrearJsonResponse(
                request,
                unidades);
        }

        private static HttpResponseMessage CrearJsonResponse<T>(
            HttpRequestMessage request,
            T data)
        {
            string json =
                JsonSerializer.Serialize(
                    data,
                    JsonOptions);

            var response =
                new HttpResponseMessage(
                    HttpStatusCode.OK)
                {
                    RequestMessage =
                        request,

                    Content =
                        new StringContent(
                            json,
                            Encoding.UTF8,
                            "application/json")
                };

            response.Headers.TryAddWithoutValidation(
                "X-Datos-Origen",
                "motor-local");

            return response;
        }

        private static string ObtenerPath(
            HttpRequestMessage request)
        {
            Uri? uri =
                request.RequestUri;

            if (uri == null)
                return string.Empty;

            if (uri.IsAbsoluteUri)
                return uri.AbsolutePath;

            string raw =
                uri.OriginalString;

            int posicionQuery =
                raw.IndexOf(
                    '?');

            if (posicionQuery >= 0)
                raw = raw[..posicionQuery];

            return "/" +
                raw.TrimStart('/');
        }

        private static string Limpiar(
            string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
    }
}
