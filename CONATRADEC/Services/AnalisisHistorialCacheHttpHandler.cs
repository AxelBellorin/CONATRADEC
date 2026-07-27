using CONATRADEC.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// En línea siempre deja pasar la petición a la API y después guarda una
    /// copia local silenciosa del detalle o de los datos del reporte.
    ///
    /// Sin conexión responde exclusivamente desde SQLite. Nunca intenta usar el
    /// servidor como fallback.
    /// </summary>
    public sealed class AnalisisHistorialCacheHttpHandler :
        DelegatingHandler
    {
        private const string RutaDetalle =
            "/api/guardar-todo/listardetalle/";

        private const string RutaReporte =
            "/api/reportes/analisis/";

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Get)
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            string path = ObtenerPath(request);

            if (!TryObtenerRutaAnalisis(
                    path,
                    out int id,
                    out TipoRuta tipo))
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            /*
             * Los IDs reservados para operaciones creadas localmente son
             * atendidos por AnalisisOfflineGuardarHttpHandler.
             */
            if (AnalisisOfflineDatabaseService.EsIdLocal(id))
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            if (ModoSesionService.EsOffline)
            {
                return await CrearRespuestaLocalAsync(
                    request,
                    id,
                    tipo,
                    cancellationToken);
            }

            /*
             * El PDF online pertenece completamente al servidor. No se copia
             * ni se reconstruye para conservar sus encabezados y evitar leerlo
             * dos veces en memoria.
             */
            if (tipo == TipoRuta.Pdf)
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            HttpResponseMessage response =
                await base.SendAsync(
                    request,
                    cancellationToken);

            if (!response.IsSuccessStatusCode ||
                response.Content == null)
            {
                return response;
            }

            byte[] bytes = await response.Content
                .ReadAsByteArrayAsync(cancellationToken);

            string mediaType =
                response.Content.Headers.ContentType?.MediaType ??
                "application/json";

            ReemplazarContenido(
                response,
                bytes,
                mediaType);

            if (!DescargaOfflineContext.Activa &&
                tipo == TipoRuta.Detalle)
            {
                string json = Encoding.UTF8.GetString(bytes);

                _ = GuardarSinBloquearAsync(
                    () => AnalisisHistorialLocalService.Instance
                        .GuardarDetalleConsultadoAsync(id, json));
            }
            else if (!DescargaOfflineContext.Activa &&
                     tipo == TipoRuta.DatosReporte)
            {
                string json = Encoding.UTF8.GetString(bytes);

                _ = GuardarSinBloquearAsync(
                    () => AnalisisHistorialLocalService.Instance
                        .GuardarReporteConsultadoAsync(id, json));
            }

            return response;
        }

        private static async Task<HttpResponseMessage>
            CrearRespuestaLocalAsync(
                HttpRequestMessage request,
                int id,
                TipoRuta tipo,
                CancellationToken cancellationToken)
        {
            if (tipo == TipoRuta.Detalle)
            {
                string? json =
                    await AnalisisHistorialLocalService.Instance
                        .ObtenerDetalleJsonAsync(id);

                return string.IsNullOrWhiteSpace(json)
                    ? CrearError(
                        request,
                        "El detalle de este análisis no fue descargado. Inicie una sesión en línea y utilice Descargar todo.")
                    : CrearJsonCrudo(request, json);
            }

            string? reporteJson =
                await AnalisisHistorialLocalService.Instance
                    .ObtenerReporteJsonAsync(id);

            if (string.IsNullOrWhiteSpace(reporteJson))
            {
                return CrearError(
                    request,
                    "Los datos del reporte no fueron descargados para este análisis.");
            }

            if (tipo == TipoRuta.DatosReporte)
                return CrearJsonCrudo(request, reporteJson);

            AnalisisReporte? reporte;

            try
            {
                reporte = JsonSerializer.Deserialize<AnalisisReporte>(
                    reporteJson,
                    JsonOptions);
            }
            catch
            {
                reporte = null;
            }

            if (reporte == null)
            {
                return CrearError(
                    request,
                    "La copia local del reporte no es válida.");
            }

            byte[] pdf = await Task.Run(
                () => AnalisisPdfLocalService.Generar(reporte),
                cancellationToken);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new ByteArrayContent(pdf)
            };

            response.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/pdf");

            response.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment")
                {
                    FileNameStar =
                        reporte.NombreArchivoBase + ".pdf"
                };

            response.Headers.TryAddWithoutValidation(
                "X-CONATRADEC-Reporte-Origen",
                "HISTORIAL-LOCAL");

            return response;
        }

        private static async Task GuardarSinBloquearAsync(
            Func<Task> action)
        {
            try
            {
                await action();
            }
            catch
            {
                /*
                 * Una falla del respaldo silencioso nunca altera la respuesta
                 * que la API ya entregó al usuario.
                 */
            }
        }

        private static bool TryObtenerRutaAnalisis(
            string path,
            out int id,
            out TipoRuta tipo)
        {
            id = 0;
            tipo = TipoRuta.Ninguna;

            if (path.StartsWith(
                    RutaDetalle,
                    StringComparison.OrdinalIgnoreCase))
            {
                id = LeerIdDespuesDePrefijo(
                    path,
                    RutaDetalle);
                tipo = TipoRuta.Detalle;
                return id > 0;
            }

            if (!path.StartsWith(
                    RutaReporte,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string resto = path[RutaReporte.Length..]
                .Trim('/');

            string[] partes = resto.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries);

            if (partes.Length < 2 ||
                !int.TryParse(partes[0], out id))
            {
                return false;
            }

            tipo = partes[1].Equals(
                "pdf",
                StringComparison.OrdinalIgnoreCase)
                    ? TipoRuta.Pdf
                    : partes[1].Equals(
                        "datos",
                        StringComparison.OrdinalIgnoreCase)
                            ? TipoRuta.DatosReporte
                            : TipoRuta.Ninguna;

            return tipo != TipoRuta.Ninguna;
        }

        private static int LeerIdDespuesDePrefijo(
            string path,
            string prefijo)
        {
            string value = path[prefijo.Length..]
                .Trim('/');

            return int.TryParse(value, out int id)
                ? id
                : 0;
        }

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

        private static void ReemplazarContenido(
            HttpResponseMessage response,
            byte[] bytes,
            string mediaType)
        {
            response.Content.Dispose();
            response.Content = new ByteArrayContent(bytes);
            response.Content.Headers.ContentType =
                new MediaTypeHeaderValue(mediaType);
        }

        private static HttpResponseMessage CrearJsonCrudo(
            HttpRequestMessage request,
            string json)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json")
            };

            response.Headers.TryAddWithoutValidation(
                "X-CONATRADEC-Datos-Origen",
                "HISTORIAL-LOCAL");

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

        private enum TipoRuta
        {
            Ninguna,
            Detalle,
            DatosReporte,
            Pdf
        }
    }
}
