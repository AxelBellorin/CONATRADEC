using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Adjunta a cada creación/edición la fecha real del dispositivo, el
    /// origen online/offline, la versión visualizada y una fotografía exacta
    /// del reporte que revisó el usuario.
    ///
    /// También conserva el ETag devuelto por la API para impedir que una
    /// edición antigua sobrescriba silenciosamente cambios más recientes.
    /// </summary>
    public sealed class AnalisisHistorialConcurrenciaHttpHandler :
        DelegatingHandler
    {
        private const string RutaGuardar = "/api/guardar-todo";
        private const string RutaEditar = "/api/guardar-todo/editar/";
        private const string RutaDetalle =
            "/api/guardar-todo/listardetalle/";

        private const string RutaSincronizarOffline =
            "/api/analisis-offline/sincronizar";

        private static readonly ConcurrentDictionary<int, ControlLocal>
            Controles = new();

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string path = ObtenerPath(request);

            bool esCrear =
                request.Method == HttpMethod.Post &&
                string.Equals(
                    path,
                    RutaGuardar,
                    StringComparison.OrdinalIgnoreCase);

            bool esEditar =
                request.Method == HttpMethod.Put &&
                path.StartsWith(
                    RutaEditar,
                    StringComparison.OrdinalIgnoreCase);

            int idEdicion = esEditar
                ? ObtenerUltimoEntero(path)
                : 0;

            if (request.Method == HttpMethod.Post &&
                string.Equals(
                    path,
                    RutaSincronizarOffline,
                    StringComparison.OrdinalIgnoreCase) &&
                request.Content != null)
            {
                await PrepararSincronizacionOfflineAsync(
                    request,
                    cancellationToken);
            }

            if ((esCrear || esEditar) && request.Content != null)
            {
                await PrepararSolicitudAsync(
                    request,
                    esEditar,
                    idEdicion,
                    cancellationToken);
            }

            HttpResponseMessage response =
                await base.SendAsync(
                    request,
                    cancellationToken);

            if (request.Method == HttpMethod.Post &&
                string.Equals(
                    path,
                    RutaSincronizarOffline,
                    StringComparison.OrdinalIgnoreCase) &&
                response.IsSuccessStatusCode)
            {
                await CapturarControlSincronizacionAsync(
                    request,
                    response,
                    cancellationToken);
            }

            if (request.Method == HttpMethod.Get &&
                path.StartsWith(
                    RutaDetalle,
                    StringComparison.OrdinalIgnoreCase) &&
                response.IsSuccessStatusCode)
            {
                int id = ObtenerUltimoEntero(path);

                if (id > 0 &&
                    !AnalisisOfflineDatabaseService.EsIdLocal(id))
                {
                    await CapturarControlDetalleAsync(
                        response,
                        id,
                        cancellationToken);
                }
            }
            else if (esEditar &&
                     idEdicion > 0 &&
                     !AnalisisOfflineDatabaseService.EsIdLocal(idEdicion) &&
                     response.IsSuccessStatusCode)
            {
                CapturarControlEncabezados(
                    response,
                    idEdicion);
            }

            return response;
        }

        private static async Task PrepararSincronizacionOfflineAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            byte[] contenido =
                await request.Content!
                    .ReadAsByteArrayAsync(cancellationToken);

            if (contenido.Length == 0)
                return;

            JsonObject? root;

            try
            {
                root = JsonNode.Parse(Encoding.UTF8.GetString(contenido))?.AsObject();
            }
            catch
            {
                return;
            }

            string operacionLocalId =
                LeerTexto(root?["operacionLocalId"]);

            if (root == null ||
                string.IsNullOrWhiteSpace(operacionLocalId))
            {
                return;
            }

            List<AnalisisOfflineLocalEntity> pendientes =
                await AnalisisOfflineDatabaseService.Instance
                    .ListarPendientesAsync();

            AnalisisOfflineLocalEntity? entity =
                pendientes.FirstOrDefault(item =>
                    string.Equals(
                        item.OperacionLocalId,
                        operacionLocalId,
                        StringComparison.OrdinalIgnoreCase));

            if (entity == null ||
                string.IsNullOrWhiteSpace(entity.PayloadJson))
            {
                return;
            }

            try
            {
                JsonNode? solicitud =
                    JsonNode.Parse(entity.PayloadJson);

                if (solicitud == null)
                    return;

                root["solicitud"] = solicitud;

                if (string.IsNullOrWhiteSpace(
                        LeerTexto(root["fechaCalculoLocalUtc"])))
                {
                    root["fechaCalculoLocalUtc"] =
                        entity.FechaCreacionUtc;
                }

                ReemplazarContenido(
                    request,
                    root.ToJsonString(JsonOptions));
            }
            catch
            {
                /*
                 * Se conserva el envelope original. El servidor rechazará una
                 * edición sin versión en lugar de sobrescribir datos.
                 */
            }
        }

        private static async Task PrepararSolicitudAsync(
            HttpRequestMessage request,
            bool esEditar,
            int idEdicion,
            CancellationToken cancellationToken)
        {
            byte[] contenido =
                await request.Content!
                    .ReadAsByteArrayAsync(cancellationToken);

            if (contenido.Length == 0)
                return;

            JsonObject? root;

            try
            {
                root = JsonNode.Parse(Encoding.UTF8.GetString(contenido))?.AsObject();
            }
            catch
            {
                return;
            }

            if (root == null)
                return;

            DateTime ahoraUtc = DateTime.UtcNow;
            ControlLocal? control = null;
            AnalisisOfflineLocalEntity? entidadLocal = null;

            bool esIdLocal =
                esEditar &&
                AnalisisOfflineDatabaseService.EsIdLocal(idEdicion);

            int idServidorEdicion = idEdicion;

            if (esIdLocal)
            {
                entidadLocal =
                    await AnalisisOfflineDatabaseService.Instance
                        .ObtenerPorIdPublicoAsync(idEdicion);

                if (entidadLocal?.AnalisisSueloCalculoIdServidor is > 0)
                {
                    idServidorEdicion =
                        entidadLocal.AnalisisSueloCalculoIdServidor.Value;
                    control = ObtenerControl(idServidorEdicion) ??
                        ObtenerControlPayload(
                            entidadLocal.PayloadJson,
                            idServidorEdicion);
                }
            }
            else if (esEditar)
            {
                control = ObtenerControl(idEdicion);
            }

            DateTime? fechaCreacionUtc =
                LeerFechaUtc(root["fechaCreacionClienteUtc"]) ??
                control?.FechaCreacionClienteUtc ??
                ObtenerFechaCreacionPayload(
                    entidadLocal?.PayloadJson) ??
                ParseFecha(entidadLocal?.FechaCreacionUtc);

            if (!fechaCreacionUtc.HasValue &&
                (!esEditar || esIdLocal))
            {
                fechaCreacionUtc = ahoraUtc;
            }

            string origenExistente =
                LeerTexto(root["origenRegistro"]);

            string origen =
                string.Equals(
                    origenExistente,
                    "OFFLINE",
                    StringComparison.OrdinalIgnoreCase)
                    ? "OFFLINE"
                    : ModoSesionService.EsOffline
                        ? "OFFLINE"
                        : "ONLINE";

            if (fechaCreacionUtc.HasValue)
            {
                root["fechaCreacionClienteUtc"] =
                    fechaCreacionUtc.Value.ToString("O");
            }
            else
            {
                root.Remove("fechaCreacionClienteUtc");
            }

            root["fechaOperacionClienteUtc"] =
                ahoraUtc.ToString("O");
            root["origenRegistro"] = origen;

            if (esEditar && control != null)
            {
                root["versionRegistro"] = control.VersionRegistro;
                root["etagBase"] = control.ETag;

                request.Headers.Remove("If-Match");
                request.Headers.TryAddWithoutValidation(
                    "If-Match",
                    control.ETag);
            }

            await AdjuntarReporteHistoricoAsync(root);

            string json = root.ToJsonString(JsonOptions);
            ReemplazarContenido(request, json);
        }

        private static async Task AdjuntarReporteHistoricoAsync(
            JsonObject root)
        {
            try
            {
                GuardarTodoRequest? solicitud =
                    root.Deserialize<GuardarTodoRequest>(JsonOptions);

                if (solicitud == null)
                    return;

                AnalisisGuardadoResumen resumen =
                    await AnalisisReporteLocalEnrichmentService
                        .CrearResumenAsync(
                            solicitud.DatosAnalisis.TerrenoId,
                            analisisSueloId: 0,
                            analisisSueloCalculoId: 0,
                            solicitud.DatosAnalisis
                                .IdentificadorAnalisisSuelo);

                AnalisisReporte reporte =
                    AnalisisReporteMapper.DesdeSolicitudGuardada(
                        solicitud,
                        resumen);

                string responsable = Preferences.Get(
                    SessionKeys.KeyNombreCompletoUsuario,
                    string.Empty);

                if (!string.IsNullOrWhiteSpace(responsable))
                    reporte.Responsable = responsable.Trim();

                root["reporteHistoricoCliente"] =
                    JsonSerializer.SerializeToNode(
                        reporte,
                        JsonOptions);
            }
            catch
            {
                /*
                 * El snapshot del cliente es una protección adicional.
                 * Un error de mapeo no debe impedir que la API guarde el
                 * análisis; el backend todavía puede construir su snapshot.
                 */
            }
        }

        private static async Task CapturarControlSincronizacionAsync(
            HttpRequestMessage request,
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (response.Content == null)
                return;

            byte[] contenido = await response.Content
                .ReadAsByteArrayAsync(cancellationToken);

            MediaTypeHeaderValue? tipoContenido =
                response.Content.Headers.ContentType;

            List<KeyValuePair<string, IEnumerable<string>>> headers =
                response.Content.Headers
                    .Where(x => !string.Equals(
                        x.Key,
                        "Content-Length",
                        StringComparison.OrdinalIgnoreCase))
                    .Select(x =>
                        new KeyValuePair<string, IEnumerable<string>>(
                            x.Key,
                            x.Value.ToArray()))
                    .ToList();

            try
            {
                JsonObject? root =
                    JsonNode.Parse(Encoding.UTF8.GetString(contenido))?.AsObject();

                int id = LeerEntero(
                    root?["data"]?["analisisSueloCalculoId"]);

                if (id <= 0)
                    return;

                CapturarControlEncabezados(response, id);

                ControlLocal? control = ObtenerControl(id);
                if (control == null || request.Content == null)
                    return;

                string solicitudJson =
                    await request.Content.ReadAsStringAsync(
                        cancellationToken);

                JsonObject? envelope =
                    JsonNode.Parse(solicitudJson)?.AsObject();
                JsonObject? solicitud =
                    envelope?["solicitud"] as JsonObject;

                DateTime? fechaCreacion = LeerFechaUtc(
                    solicitud?["fechaCreacionClienteUtc"]);
                string origen = LeerTexto(
                    solicitud?["origenRegistro"]);

                GuardarControl(
                    id,
                    new ControlLocal
                    {
                        VersionRegistro = control.VersionRegistro,
                        ETag = control.ETag,
                        FechaCreacionClienteUtc =
                            fechaCreacion ??
                            control.FechaCreacionClienteUtc,
                        FechaUltimaModificacionUtc =
                            control.FechaUltimaModificacionUtc,
                        OrigenRegistro = string.IsNullOrWhiteSpace(origen)
                            ? control.OrigenRegistro
                            : origen
                    });
            }
            catch
            {
                // El servidor ya sincronizó; la próxima carga del detalle recupera el control.
            }
            finally
            {
                var nuevoContenido = new ByteArrayContent(contenido);

                foreach (KeyValuePair<string, IEnumerable<string>> header
                         in headers)
                {
                    nuevoContenido.Headers.TryAddWithoutValidation(
                        header.Key,
                        header.Value);
                }

                if (tipoContenido != null)
                    nuevoContenido.Headers.ContentType = tipoContenido;

                response.Content = nuevoContenido;
            }
        }

        private static async Task CapturarControlDetalleAsync(
            HttpResponseMessage response,
            int id,
            CancellationToken cancellationToken)
        {
            if (response.Content == null)
            {
                CapturarControlEncabezados(response, id);
                return;
            }

            byte[] contenido =
                await response.Content
                    .ReadAsByteArrayAsync(cancellationToken);

            MediaTypeHeaderValue? tipoContenido =
                response.Content.Headers.ContentType;

            IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers =
                response.Content.Headers
                    .Where(x => !string.Equals(
                        x.Key,
                        "Content-Length",
                        StringComparison.OrdinalIgnoreCase))
                    .Select(x =>
                        new KeyValuePair<string, IEnumerable<string>>(
                            x.Key,
                            x.Value.ToArray()))
                    .ToList();

            try
            {
                JsonObject? root =
                    JsonNode.Parse(Encoding.UTF8.GetString(contenido))?.AsObject();

                JsonObject? control =
                    root?["data"]?["controlHistorial"] as JsonObject;

                if (control != null)
                {
                    int version = LeerEntero(control["versionRegistro"]);
                    string etag = LeerTexto(control["etag"]);
                    DateTime? fechaCreacion =
                        LeerFechaUtc(control["fechaCreacionClienteUtc"]);
                    DateTime? fechaModificacion =
                        LeerFechaUtc(control["fechaUltimaModificacionUtc"]);
                    string origen = LeerTexto(control["origenRegistro"]);

                    if (version > 0)
                    {
                        GuardarControl(
                            id,
                            new ControlLocal
                            {
                                VersionRegistro = version,
                                ETag = string.IsNullOrWhiteSpace(etag)
                                    ? CrearETag(id, version)
                                    : etag,
                                FechaCreacionClienteUtc = fechaCreacion,
                                FechaUltimaModificacionUtc = fechaModificacion,
                                OrigenRegistro = origen
                            });
                    }
                }
                else
                {
                    CapturarControlEncabezados(response, id);
                }
            }
            catch
            {
                CapturarControlEncabezados(response, id);
            }
            finally
            {
                var nuevoContenido = new ByteArrayContent(contenido);

                foreach (KeyValuePair<string, IEnumerable<string>> header
                         in headers)
                {
                    nuevoContenido.Headers.TryAddWithoutValidation(
                        header.Key,
                        header.Value);
                }

                if (tipoContenido != null)
                    nuevoContenido.Headers.ContentType = tipoContenido;

                response.Content = nuevoContenido;
            }
        }

        private static void CapturarControlEncabezados(
            HttpResponseMessage response,
            int id)
        {
            int version = 0;

            if (response.Headers.TryGetValues(
                    "X-Version-Registro",
                    out IEnumerable<string>? versiones))
            {
                int.TryParse(
                    versiones.FirstOrDefault(),
                    out version);
            }

            string etag = response.Headers.ETag?.Tag ?? string.Empty;

            if (string.IsNullOrWhiteSpace(etag) &&
                response.Headers.TryGetValues(
                    "ETag",
                    out IEnumerable<string>? etags))
            {
                etag = etags.FirstOrDefault() ?? string.Empty;
            }

            if (version <= 0)
            {
                ControlLocal? anterior = ObtenerControl(id);
                version = anterior?.VersionRegistro ?? 0;
            }

            if (version <= 0)
                return;

            ControlLocal? existente = ObtenerControl(id);

            GuardarControl(
                id,
                new ControlLocal
                {
                    VersionRegistro = version,
                    ETag = string.IsNullOrWhiteSpace(etag)
                        ? CrearETag(id, version)
                        : etag,
                    FechaCreacionClienteUtc =
                        existente?.FechaCreacionClienteUtc,
                    FechaUltimaModificacionUtc = DateTime.UtcNow,
                    OrigenRegistro =
                        existente?.OrigenRegistro ?? string.Empty
                });
        }

        private static ControlLocal? ObtenerControlPayload(
            string? payloadJson,
            int analisisSueloCalculoId)
        {
            if (string.IsNullOrWhiteSpace(payloadJson) ||
                analisisSueloCalculoId <= 0)
            {
                return null;
            }

            try
            {
                JsonObject? root =
                    JsonNode.Parse(payloadJson)?.AsObject();

                int version = LeerEntero(root?["versionRegistro"]);
                if (version <= 0)
                    return null;

                string etag = LeerTexto(root?["etagBase"]);

                return new ControlLocal
                {
                    VersionRegistro = version,
                    ETag = string.IsNullOrWhiteSpace(etag)
                        ? CrearETag(analisisSueloCalculoId, version)
                        : etag,
                    FechaCreacionClienteUtc = LeerFechaUtc(
                        root?["fechaCreacionClienteUtc"]),
                    FechaUltimaModificacionUtc = null,
                    OrigenRegistro = LeerTexto(root?["origenRegistro"])
                };
            }
            catch
            {
                return null;
            }
        }

        private static DateTime? ObtenerFechaCreacionPayload(
            string? payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
                return null;

            try
            {
                JsonObject? root =
                    JsonNode.Parse(payloadJson)?.AsObject();

                return LeerFechaUtc(
                    root?["fechaCreacionClienteUtc"]);
            }
            catch
            {
                return null;
            }
        }

        private static void GuardarControl(
            int id,
            ControlLocal control)
        {
            if (id <= 0 || control.VersionRegistro <= 0)
                return;

            Controles[id] = control;

            Preferences.Set(Clave(id, "version"), control.VersionRegistro);
            Preferences.Set(Clave(id, "etag"), control.ETag ?? string.Empty);
            Preferences.Set(
                Clave(id, "fechaCreacion"),
                control.FechaCreacionClienteUtc?.ToString("O") ??
                string.Empty);
            Preferences.Set(
                Clave(id, "fechaModificacion"),
                control.FechaUltimaModificacionUtc?.ToString("O") ??
                string.Empty);
            Preferences.Set(
                Clave(id, "origen"),
                control.OrigenRegistro ?? string.Empty);
        }

        private static ControlLocal? ObtenerControl(int id)
        {
            if (id <= 0)
                return null;

            if (Controles.TryGetValue(id, out ControlLocal? control))
                return control;

            int version = Preferences.Get(Clave(id, "version"), 0);
            if (version <= 0)
                return null;

            control = new ControlLocal
            {
                VersionRegistro = version,
                ETag = Preferences.Get(
                    Clave(id, "etag"),
                    CrearETag(id, version)),
                FechaCreacionClienteUtc = ParseFecha(
                    Preferences.Get(
                        Clave(id, "fechaCreacion"),
                        string.Empty)),
                FechaUltimaModificacionUtc = ParseFecha(
                    Preferences.Get(
                        Clave(id, "fechaModificacion"),
                        string.Empty)),
                OrigenRegistro = Preferences.Get(
                    Clave(id, "origen"),
                    string.Empty)
            };

            Controles[id] = control;
            return control;
        }

        private static void ReemplazarContenido(
            HttpRequestMessage request,
            string json)
        {
            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");
        }

        private static string ObtenerPath(
            HttpRequestMessage request) =>
            request.RequestUri?.IsAbsoluteUri == true
                ? request.RequestUri.AbsolutePath
                : "/" +
                  (request.RequestUri?.OriginalString ?? string.Empty)
                    .TrimStart('/');

        private static int ObtenerUltimoEntero(string path)
        {
            string ultimo = path
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault() ?? string.Empty;

            return int.TryParse(ultimo, out int id) ? id : 0;
        }

        private static int LeerEntero(JsonNode? node)
        {
            if (node == null)
                return 0;

            try
            {
                return node.GetValue<int>();
            }
            catch
            {
                return int.TryParse(node.ToString(), out int valor)
                    ? valor
                    : 0;
            }
        }

        private static string LeerTexto(JsonNode? node)
        {
            if (node == null)
                return string.Empty;

            try
            {
                return node.GetValue<string>()?.Trim() ?? string.Empty;
            }
            catch
            {
                return node.ToString().Trim();
            }
        }

        private static DateTime? LeerFechaUtc(JsonNode? node) =>
            ParseFecha(LeerTexto(node));

        private static DateTime? ParseFecha(string? valor)
        {
            if (!DateTime.TryParse(
                    valor,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out DateTime fecha))
            {
                return null;
            }

            return fecha.Kind switch
            {
                DateTimeKind.Utc => fecha,
                DateTimeKind.Local => fecha.ToUniversalTime(),
                _ => DateTime.SpecifyKind(fecha, DateTimeKind.Utc)
            };
        }

        private static string CrearETag(int id, int version) =>
            $"\"analisis-{id}-v{version}\"";

        private static string Clave(int id, string campo) =>
            $"CONATRADEC.AnalisisHistorial.{id}.{campo}";

        private sealed class ControlLocal
        {
            public int VersionRegistro { get; init; }
            public string ETag { get; init; } = string.Empty;
            public DateTime? FechaCreacionClienteUtc { get; init; }
            public DateTime? FechaUltimaModificacionUtc { get; init; }
            public string OrigenRegistro { get; init; } = string.Empty;
        }
    }
}
