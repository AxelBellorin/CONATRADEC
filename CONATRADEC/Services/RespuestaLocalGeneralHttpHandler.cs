using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Respaldo oportunista de respuestas JSON consultadas durante una sesión
    /// online. Complementa Descargar todo sin sustituirlo.
    ///
    /// En modo offline entrega la respuesta exacta si existe y, si no existe,
    /// permite que los manejadores especializados intenten resolverla.
    /// </summary>
    public sealed class RespuestaLocalGeneralHttpHandler :
        DelegatingHandler
    {
        private readonly ContenidoLocalDatabaseService database =
            ContenidoLocalDatabaseService.Instance;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Get ||
                !DatosSinConexionPermisos.TienePermiso)
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            string usuarioId = Preferences.Get(
                SessionKeys.KeyUserId,
                "0");

            if (usuarioId == "0")
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            string route = ObtenerPathYQuery(request);
            string key = CalcularHash(
                $"{usuarioId}|general|{route}");

            if (ModoSesionService.EsOffline)
            {
                ContenidoRespuestaCacheEntity? local =
                    await database.ObtenerRespuestaAsync(key);

                if (local == null)
                {
                    return await base.SendAsync(
                        request,
                        cancellationToken);
                }

                await database.MarcarUsoRespuestaAsync(
                    key,
                    DateTime.UtcNow);

                var localResponse = new HttpResponseMessage(
                    (HttpStatusCode)local.StatusCode)
                {
                    RequestMessage = request,
                    Content = new StringContent(
                        local.Json,
                        Encoding.UTF8,
                        string.IsNullOrWhiteSpace(local.ContentType)
                            ? "application/json"
                            : local.ContentType)
                };

                localResponse.Headers.TryAddWithoutValidation(
                    "X-CONATRADEC-Origen",
                    "CACHE-GENERAL");

                return localResponse;
            }

            HttpResponseMessage response =
                await base.SendAsync(
                    request,
                    cancellationToken);

            if (!response.IsSuccessStatusCode ||
                response.Content == null ||
                !EsJson(response))
            {
                return response;
            }

            string json = await response.Content
                .ReadAsStringAsync(cancellationToken);

            string contentType =
                response.Content.Headers.ContentType?.MediaType ??
                "application/json";

            response.Content.Dispose();
            response.Content = new StringContent(
                json,
                Encoding.UTF8,
                contentType);

            if (!DescargaOfflineContext.Activa)
            {
                _ = GuardarSilenciosamenteAsync(
                    key,
                    usuarioId,
                    route,
                    contentType,
                    json,
                    (int)response.StatusCode);
            }

            return response;
        }

        private async Task GuardarSilenciosamenteAsync(
            string key,
            string usuarioId,
            string route,
            string contentType,
            string json,
            int statusCode)
        {
            try
            {
                DateTime now = DateTime.UtcNow;

                await database.GuardarRespuestaAsync(
                    new ContenidoRespuestaCacheEntity
                    {
                        CacheKey = key,
                        UsuarioId = usuarioId,
                        Modulo = "general",
                        Ruta = route,
                        Version = CalcularHash(json),
                        StatusCode = statusCode,
                        ContentType = contentType,
                        Json = json,
                        GuardadoUtc = now,
                        UltimoUsoUtc = now
                    });
            }
            catch
            {
                /* El respaldo nunca bloquea la respuesta de la API. */
            }
        }

        private static bool EsJson(
            HttpResponseMessage response)
        {
            string mediaType =
                response.Content.Headers.ContentType?.MediaType ??
                string.Empty;

            return mediaType.Contains(
                       "json",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.IsNullOrWhiteSpace(mediaType);
        }

        private static string ObtenerPathYQuery(
            HttpRequestMessage request)
        {
            Uri? uri = request.RequestUri;
            if (uri == null)
                return string.Empty;

            return uri.IsAbsoluteUri
                ? uri.PathAndQuery
                : "/" + uri.OriginalString.TrimStart('/');
        }

        private static string CalcularHash(string value)
        {
            byte[] hash = SHA256.HashData(
                Encoding.UTF8.GetBytes(value));

            return Convert.ToHexString(hash)
                .ToLowerInvariant();
        }
    }
}
