using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Consulta el manifiesto ligero del servidor y lo compara con la última
    /// descarga completa confirmada para el usuario actual.
    ///
    /// Esta clase nunca debe invocarse durante una sesión offline.
    /// </summary>
    public sealed class SincronizacionOfflineManifiestoService
    {
        private const string ClavePrefijo =
            "offline_manifiesto_descargado_v1_";

        private static readonly Lazy<
            SincronizacionOfflineManifiestoService> lazy =
                new(() =>
                    new SincronizacionOfflineManifiestoService());

        private readonly SemaphoreSlim comprobacionLock =
            new(1, 1);

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public static SincronizacionOfflineManifiestoService Instance =>
            lazy.Value;

        private SincronizacionOfflineManifiestoService()
        {
        }

        public async Task<ResultadoComprobacionOffline>
            ComprobarAsync(
                CancellationToken cancellationToken = default)
        {
            if (!ModoSesionService.EsEnLinea)
            {
                return ResultadoComprobacionOffline.Fail(
                    "Inicie una sesión en línea para comprobar actualizaciones.");
            }

            await comprobacionLock.WaitAsync(cancellationToken);

            try
            {
                ResultadoLecturaManifiesto remoto =
                    await ObtenerRemotoAsync(cancellationToken);

                if (!remoto.Success || remoto.Manifiesto == null)
                {
                    return ResultadoComprobacionOffline.Fail(
                        remoto.Message);
                }

                string usuarioId = ObtenerUsuarioId();

                bool preparado =
                    SincronizacionOfflineGlobalService
                        .EstaPreparadoParaUsuario(usuarioId);

                SincronizacionOfflineManifiestoLocal? local =
                    CargarLocal(usuarioId);

                bool esquemaCambio =
                    local != null &&
                    local.EsquemaVersion !=
                        remoto.Manifiesto.EsquemaVersion;

                var modulos = new List<
                    SincronizacionOfflineModuloComparacion>();

                foreach (
                    SincronizacionOfflineManifiestoModulo modulo
                    in remoto.Manifiesto.Modulos)
                {
                    string versionLocal = string.Empty;

                    if (local != null &&
                        local.Versiones.TryGetValue(
                            modulo.Codigo,
                            out string? encontrada))
                    {
                        versionLocal = encontrada ?? string.Empty;
                    }

                    bool requiere =
                        modulo.Habilitado &&
                        (
                            !preparado ||
                            local == null ||
                            esquemaCambio ||
                            !string.Equals(
                                modulo.Version,
                                versionLocal,
                                StringComparison.Ordinal)
                        );

                    modulos.Add(
                        new SincronizacionOfflineModuloComparacion
                        {
                            Codigo = modulo.Codigo,
                            Nombre = modulo.Nombre,
                            Habilitado = modulo.Habilitado,
                            RequiereActualizar = requiere,
                            VersionServidor = modulo.Version,
                            VersionLocal = versionLocal,
                            TotalRegistrosServidor =
                                modulo.TotalRegistros
                        });
                }

                bool descargaInicial = !preparado;
                bool hayCambios = modulos.Any(x =>
                    x.Habilitado &&
                    x.RequiereActualizar);

                string mensaje;

                if (descargaInicial)
                {
                    mensaje =
                        "El dispositivo todavía no tiene una descarga completa válida.";
                }
                else if (hayCambios)
                {
                    mensaje =
                        "Revise las interfaces y los módulos resaltados para conocer qué información se actualizará.";
                }
                else
                {
                    mensaje =
                        "El dispositivo está actualizado.";
                }

                return new ResultadoComprobacionOffline
                {
                    Success = true,
                    RequiereDescargaInicial = descargaInicial,
                    HayActualizaciones = hayCambios,
                    Message = mensaje,
                    FechaComprobacionUtc = DateTime.UtcNow,
                    Manifiesto = remoto.Manifiesto,
                    Modulos = modulos
                };
            }
            finally
            {
                comprobacionLock.Release();
            }
        }

        public async Task<ResultadoComprobacionOffline>
            RegistrarDescargaActualAsync(
                CancellationToken cancellationToken = default)
        {
            if (!ModoSesionService.EsEnLinea)
            {
                return ResultadoComprobacionOffline.Fail(
                    "No se puede registrar el manifiesto durante una sesión sin conexión.");
            }

            await comprobacionLock.WaitAsync(cancellationToken);

            try
            {
                ResultadoLecturaManifiesto remoto =
                    await ObtenerRemotoAsync(cancellationToken);

                if (!remoto.Success || remoto.Manifiesto == null)
                {
                    return ResultadoComprobacionOffline.Fail(
                        remoto.Message);
                }

                string usuarioId = ObtenerUsuarioId();

                var local = new SincronizacionOfflineManifiestoLocal
                {
                    EsquemaVersion =
                        remoto.Manifiesto.EsquemaVersion,
                    UsuarioId = remoto.Manifiesto.UsuarioId,
                    FechaDescargaUtc = DateTime.UtcNow,
                    VersionGeneral =
                        remoto.Manifiesto.VersionGeneral,
                    Versiones = remoto.Manifiesto.Modulos
                        .Where(x => x.Habilitado)
                        .ToDictionary(
                            x => x.Codigo,
                            x => x.Version,
                            StringComparer.OrdinalIgnoreCase)
                };

                Preferences.Set(
                    ConstruirClave(usuarioId),
                    JsonSerializer.Serialize(local, jsonOptions));

                return new ResultadoComprobacionOffline
                {
                    Success = true,
                    RequiereDescargaInicial = false,
                    HayActualizaciones = false,
                    Message =
                        "El dispositivo está actualizado.",
                    FechaComprobacionUtc = DateTime.UtcNow,
                    Manifiesto = remoto.Manifiesto,
                    Modulos = remoto.Manifiesto.Modulos
                        .Select(x =>
                            new SincronizacionOfflineModuloComparacion
                            {
                                Codigo = x.Codigo,
                                Nombre = x.Nombre,
                                Habilitado = x.Habilitado,
                                RequiereActualizar = false,
                                VersionServidor = x.Version,
                                VersionLocal = x.Version,
                                TotalRegistrosServidor =
                                    x.TotalRegistros
                            })
                        .ToList()
                };
            }
            finally
            {
                comprobacionLock.Release();
            }
        }

        public DateTime? ObtenerFechaManifiestoLocal()
        {
            SincronizacionOfflineManifiestoLocal? local =
                CargarLocal(ObtenerUsuarioId());

            return local?.FechaDescargaUtc;
        }

        private async Task<ResultadoLecturaManifiesto>
            ObtenerRemotoAsync(
                CancellationToken cancellationToken)
        {
            string usuarioId = ObtenerUsuarioId();

            if (usuarioId == "0")
            {
                return ResultadoLecturaManifiesto.Fail(
                    "No fue posible identificar al usuario de la sesión.");
            }

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    "api/sincronizacion-offline/manifiesto");

                request.Headers.CacheControl =
                    new CacheControlHeaderValue
                    {
                        NoCache = true,
                        NoStore = true
                    };

                request.Headers.Remove("X-Usuario-Id");
                request.Headers.TryAddWithoutValidation(
                    "X-Usuario-Id",
                    usuarioId);

                using var timeout =
                    CancellationTokenSource
                        .CreateLinkedTokenSource(
                            cancellationToken);

                timeout.CancelAfter(TimeSpan.FromSeconds(25));

                using HttpResponseMessage response =
                    await ApiClientService.Client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseContentRead,
                        timeout.Token);

                string json = await response.Content
                    .ReadAsStringAsync(cancellationToken);

                ApiEnvelopeManifiesto? envelope = null;

                try
                {
                    envelope = JsonSerializer.Deserialize<
                        ApiEnvelopeManifiesto>(
                        json,
                        jsonOptions);
                }
                catch (JsonException)
                {
                }

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return ResultadoLecturaManifiesto.Fail(
                            "El servidor todavía no tiene disponible el servicio de comprobación de actualizaciones.");
                    }

                    return ResultadoLecturaManifiesto.Fail(
                        envelope?.Message ??
                        "No fue posible comprobar actualizaciones en el servidor.");
                }

                if (envelope?.Success != true ||
                    envelope.Data == null)
                {
                    return ResultadoLecturaManifiesto.Fail(
                        envelope?.Message ??
                        "El servidor no devolvió el manifiesto esperado.");
                }

                return ResultadoLecturaManifiesto.Ok(
                    envelope.Data,
                    envelope.Message);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ResultadoLecturaManifiesto.Fail(
                    "La comprobación tardó demasiado. Intente nuevamente.");
            }
            catch (HttpRequestException)
            {
                return ResultadoLecturaManifiesto.Fail(
                    "No fue posible conectar con la API para comprobar actualizaciones.");
            }
            catch (Exception)
            {
                return ResultadoLecturaManifiesto.Fail(
                    "Ocurrió un error al comprobar actualizaciones.");
            }
        }

        private SincronizacionOfflineManifiestoLocal? CargarLocal(
            string usuarioId)
        {
            if (usuarioId == "0")
                return null;

            string json = Preferences.Get(
                ConstruirClave(usuarioId),
                string.Empty);

            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<
                    SincronizacionOfflineManifiestoLocal>(
                    json,
                    jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private static string ObtenerUsuarioId()
        {
            string value = Preferences.Get(
                SessionKeys.KeyUserId,
                "0");

            return string.IsNullOrWhiteSpace(value)
                ? "0"
                : value.Trim();
        }

        private static string ConstruirClave(
            string usuarioId) =>
            ClavePrefijo + usuarioId;

        private sealed class ApiEnvelopeManifiesto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public SincronizacionOfflineManifiesto? Data { get; set; }
        }

        private sealed class ResultadoLecturaManifiesto
        {
            public bool Success { get; init; }
            public string Message { get; init; } = string.Empty;
            public SincronizacionOfflineManifiesto? Manifiesto
            {
                get;
                init;
            }

            public static ResultadoLecturaManifiesto Ok(
                SincronizacionOfflineManifiesto manifiesto,
                string message) =>
                new()
                {
                    Success = true,
                    Manifiesto = manifiesto,
                    Message = message
                };

            public static ResultadoLecturaManifiesto Fail(
                string message) =>
                new()
                {
                    Success = false,
                    Message = message
                };
        }
    }
}
