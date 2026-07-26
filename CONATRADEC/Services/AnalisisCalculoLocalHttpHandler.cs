using CONATRADEC.Models;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Intercepta únicamente el cálculo inicial del análisis de suelo.
    ///
    /// - En modo En línea conserva la petición normal a la API.
    /// - En modo Sin conexión responde con el motor local.
    /// - Si el análisis inició en línea y la señal cae, cambia a local cuando
    ///   existe un paquete válido.
    ///
    /// No intercepta operaciones administrativas ni el guardado definitivo.
    /// </summary>
    public sealed class AnalisisCalculoLocalHttpHandler :
        DelegatingHandler
    {
        private const string RutaCalcular =
            "/api/analisis-suelo/calcular";

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!EsCalculoAnalisis(request) ||
                !DatosSinConexionPermisos.TienePermiso)
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            string cuerpo =
                request.Content == null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(
                        cancellationToken);

            ModoTrabajoAnalisisEstado modo =
                ModoTrabajoAnalisisService
                    .Instance
                    .EstadoActual;

            if (modo.Modo ==
                ModoTrabajoAnalisis.SinConexion)
            {
                return await CrearRespuestaLocalAsync(
                    request,
                    cuerpo,
                    cancellationToken);
            }

            if (!EstadoConexionService.Instance.HayInternet)
            {
                ModoTrabajoAnalisisEstado cambiado =
                    await ModoTrabajoAnalisisService
                        .Instance
                        .CambiarAOfflinePorCaidaAsync(
                            cancellationToken);

                if (cambiado.Modo ==
                        ModoTrabajoAnalisis.SinConexion &&
                    cambiado.PaqueteLocalDisponible)
                {
                    return await CrearRespuestaLocalAsync(
                        request,
                        cuerpo,
                        cancellationToken);
                }
            }

            try
            {
                HttpResponseMessage response =
                    await base.SendAsync(
                        request,
                        cancellationToken);

                if (!DebeCambiarALocal(
                        response.StatusCode))
                {
                    return response;
                }

                ModoTrabajoAnalisisEstado cambiado =
                    await ModoTrabajoAnalisisService
                        .Instance
                        .CambiarAOfflinePorCaidaAsync(
                            cancellationToken);

                if (cambiado.Modo !=
                        ModoTrabajoAnalisis.SinConexion ||
                    !cambiado.PaqueteLocalDisponible)
                {
                    return response;
                }

                response.Dispose();

                return await CrearRespuestaLocalAsync(
                    request,
                    cuerpo,
                    cancellationToken);
            }
            catch (HttpRequestException)
            {
                ModoTrabajoAnalisisEstado cambiado =
                    await ModoTrabajoAnalisisService
                        .Instance
                        .CambiarAOfflinePorCaidaAsync(
                            cancellationToken);

                if (cambiado.Modo ==
                        ModoTrabajoAnalisis.SinConexion &&
                    cambiado.PaqueteLocalDisponible)
                {
                    return await CrearRespuestaLocalAsync(
                        request,
                        cuerpo,
                        cancellationToken);
                }

                throw;
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                ModoTrabajoAnalisisEstado cambiado =
                    await ModoTrabajoAnalisisService
                        .Instance
                        .CambiarAOfflinePorCaidaAsync(
                            cancellationToken);

                if (cambiado.Modo ==
                        ModoTrabajoAnalisis.SinConexion &&
                    cambiado.PaqueteLocalDisponible)
                {
                    return await CrearRespuestaLocalAsync(
                        request,
                        cuerpo,
                        cancellationToken);
                }

                throw;
            }
        }

        private static bool EsCalculoAnalisis(
            HttpRequestMessage request)
        {
            if (request.Method != HttpMethod.Post)
                return false;

            Uri? uri =
                request.RequestUri;

            string path =
                uri == null
                    ? string.Empty
                    : uri.IsAbsoluteUri
                        ? uri.AbsolutePath
                        : "/" +
                          uri.OriginalString
                              .TrimStart('/');

            return string.Equals(
                path,
                RutaCalcular,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool DebeCambiarALocal(
            HttpStatusCode statusCode) =>
            statusCode is
                HttpStatusCode.RequestTimeout or
                HttpStatusCode.BadGateway or
                HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout;

        private static async Task<HttpResponseMessage>
            CrearRespuestaLocalAsync(
                HttpRequestMessage request,
                string cuerpo,
                CancellationToken cancellationToken)
        {
            AnalisisSueloCalcularRequest? solicitud;

            try
            {
                solicitud =
                    JsonSerializer.Deserialize<
                        AnalisisSueloCalcularRequest>(
                        cuerpo,
                        JsonOptions);
            }
            catch (JsonException)
            {
                solicitud = null;
            }

            AnalisisSueloCalculoResponse resultado =
                solicitud == null
                    ? new AnalisisSueloCalculoResponse
                    {
                        Success = false,
                        Message =
                            "No fue posible interpretar los datos del análisis para calcularlos localmente."
                    }
                    : await MotorCalculoLocalService.Instance
                        .CalcularRequerimientoAnualAsync(
                            solicitud,
                            cancellationToken);

            string json =
                JsonSerializer.Serialize(
                    resultado,
                    JsonOptions);

            var response =
                new HttpResponseMessage(
                    HttpStatusCode.OK)
                {
                    RequestMessage = request,
                    Content =
                        new StringContent(
                            json,
                            Encoding.UTF8,
                            "application/json")
                };

            response.Headers.TryAddWithoutValidation(
                "X-CONATRADEC-Calculo-Origen",
                "LOCAL");

            return response;
        }
    }
}
