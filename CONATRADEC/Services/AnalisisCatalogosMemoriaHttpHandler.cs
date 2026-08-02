using Microsoft.Maui.Storage;
using System.Collections.Concurrent;
using System.Net;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Mantiene durante unos minutos los catálogos usados por los cálculos
    /// complementarios. Solo funciona en sesiones en línea y únicamente para
    /// solicitudes GET conocidas. No almacena cálculos ni respuestas de
    /// guardado, por lo que no modifica la lógica del análisis.
    /// </summary>
    internal sealed class AnalisisCatalogosMemoriaHttpHandler :
        DelegatingHandler
    {
        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(5);

        private static readonly ConcurrentDictionary<string, SemaphoreSlim>
            LocksPorClave = new(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, EntradaCache>
            Cache = new(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> RutasCacheables =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "/api/fuente-nutriente/listar",
                "/api/fuente-nutriente/enmiendas-calcareas",
                "/api/fuente-nutriente/listar-fertilizacion-mixta"
            };

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string ruta = ObtenerRuta(request.RequestUri);

            if (!ModoSesionService.EsEnLinea)
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            if (request.Method != HttpMethod.Get)
            {
                if (EsMutacionCatalogo(request.Method, ruta))
                    LimpiarCache();

                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            if (!RutasCacheables.Contains(ruta))
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            string clave = ConstruirClave(request, ruta);

            if (IntentarObtener(clave, out EntradaCache? entrada))
                return CrearRespuesta(entrada!, request);

            SemaphoreSlim bloqueo =
                LocksPorClave.GetOrAdd(
                    clave,
                    _ => new SemaphoreSlim(1, 1));

            await bloqueo.WaitAsync(cancellationToken);

            try
            {
                if (IntentarObtener(clave, out entrada))
                    return CrearRespuesta(entrada!, request);

                using HttpResponseMessage respuestaOrigen =
                    await base.SendAsync(
                        request,
                        cancellationToken);

                EntradaCache respuestaCapturada =
                    await CapturarAsync(
                        respuestaOrigen,
                        cancellationToken);

                if (respuestaOrigen.IsSuccessStatusCode)
                    Cache[clave] = respuestaCapturada;

                return CrearRespuesta(
                    respuestaCapturada,
                    request);
            }
            finally
            {
                bloqueo.Release();
            }
        }

        public static void LimpiarCache()
        {
            Cache.Clear();
        }

        private static bool IntentarObtener(
            string clave,
            out EntradaCache? entrada)
        {
            if (!Cache.TryGetValue(clave, out entrada))
                return false;

            if (DateTime.UtcNow - entrada.FechaUtc <= DuracionCache)
                return true;

            Cache.TryRemove(clave, out _);
            entrada = null;
            return false;
        }

        private static string ConstruirClave(
            HttpRequestMessage request,
            string ruta)
        {
            string usuarioId = Preferences.Get(
                SessionKeys.KeyUserId,
                string.Empty);

            string consulta =
                request.RequestUri?.Query ??
                string.Empty;

            return $"{usuarioId}|{ruta}|{consulta}";
        }

        private static string ObtenerRuta(Uri? uri)
        {
            string ruta =
                uri?.IsAbsoluteUri == true
                    ? uri.AbsolutePath
                    : uri?.OriginalString ?? string.Empty;

            int indiceConsulta = ruta.IndexOf('?');

            if (indiceConsulta >= 0)
                ruta = ruta[..indiceConsulta];

            if (!ruta.StartsWith('/'))
                ruta = "/" + ruta;

            return ruta.TrimEnd('/');
        }

        private static bool EsMutacionCatalogo(
            HttpMethod metodo,
            string ruta)
        {
            if (metodo == HttpMethod.Get)
                return false;

            return ruta.StartsWith(
                "/api/fuente-nutriente",
                StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<EntradaCache> CapturarAsync(
            HttpResponseMessage respuesta,
            CancellationToken cancellationToken)
        {
            byte[] contenido =
                respuesta.Content == null
                    ? Array.Empty<byte>()
                    : await respuesta.Content.ReadAsByteArrayAsync(
                        cancellationToken);

            Dictionary<string, string[]> encabezados =
                respuesta.Headers.ToDictionary(
                    item => item.Key,
                    item => item.Value.ToArray(),
                    StringComparer.OrdinalIgnoreCase);

            Dictionary<string, string[]> encabezadosContenido =
                respuesta.Content?.Headers.ToDictionary(
                    item => item.Key,
                    item => item.Value.ToArray(),
                    StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, string[]>(
                    StringComparer.OrdinalIgnoreCase);

            return new EntradaCache(
                DateTime.UtcNow,
                respuesta.StatusCode,
                respuesta.ReasonPhrase,
                respuesta.Version,
                contenido,
                encabezados,
                encabezadosContenido);
        }

        private static HttpResponseMessage CrearRespuesta(
            EntradaCache entrada,
            HttpRequestMessage request)
        {
            var respuesta = new HttpResponseMessage(
                entrada.StatusCode)
            {
                ReasonPhrase = entrada.ReasonPhrase,
                Version = entrada.Version,
                RequestMessage = request,
                Content = new ByteArrayContent(
                    entrada.Contenido.ToArray())
            };

            foreach ((string nombre, string[] valores) in
                     entrada.Encabezados)
            {
                respuesta.Headers.TryAddWithoutValidation(
                    nombre,
                    valores);
            }

            foreach ((string nombre, string[] valores) in
                     entrada.EncabezadosContenido)
            {
                respuesta.Content.Headers.TryAddWithoutValidation(
                    nombre,
                    valores);
            }

            return respuesta;
        }

        private sealed record EntradaCache(
            DateTime FechaUtc,
            HttpStatusCode StatusCode,
            string? ReasonPhrase,
            Version Version,
            byte[] Contenido,
            Dictionary<string, string[]> Encabezados,
            Dictionary<string, string[]> EncabezadosContenido);
    }
}
