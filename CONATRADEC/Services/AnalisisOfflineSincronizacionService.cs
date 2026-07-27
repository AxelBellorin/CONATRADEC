using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Envía pendientes una sola vez al comenzar una sesión online o cuando el
    /// usuario ejecuta Descargar todo.
    ///
    /// No escucha cambios de red, no usa temporizadores y no reintenta durante
    /// una sesión offline.
    /// </summary>
    public sealed class AnalisisOfflineSincronizacionService
    {
        private static readonly Lazy<
            AnalisisOfflineSincronizacionService> lazy =
                new(() => new AnalisisOfflineSincronizacionService());

        private readonly SemaphoreSlim syncLock = new(1, 1);
        private int solicitadoEnSesion;

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

        public static AnalisisOfflineSincronizacionService Instance =>
            lazy.Value;

        public event EventHandler? ColaCambiada;

        private AnalisisOfflineSincronizacionService()
        {
        }

        public void ReiniciarSesion()
        {
            Interlocked.Exchange(
                ref solicitadoEnSesion,
                0);
        }

        public void SolicitarUnaVezPorSesionOnline()
        {
            if (!ModoSesionService.EsEnLinea ||
                !DatosSinConexionPermisos.TienePermiso ||
                string.IsNullOrWhiteSpace(
                    Preferences.Get(
                        SessionKeys.KeyUserId,
                        string.Empty)))
            {
                return;
            }

            if (Interlocked.Exchange(
                    ref solicitadoEnSesion,
                    1) == 1)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await SincronizarPendientesAsync();
                }
                catch
                {
                    /* Se volverá a intentar en el próximo login online. */
                }
            });
        }

        public Task<int> SincronizarAhoraAsync(
            CancellationToken cancellationToken = default) =>
            SincronizarPendientesInternoAsync(
                esperarTurno: true,
                cancellationToken);

        public Task<int> SincronizarPendientesAsync(
            CancellationToken cancellationToken = default) =>
            SincronizarPendientesInternoAsync(
                esperarTurno: false,
                cancellationToken);

        private async Task<int> SincronizarPendientesInternoAsync(
            bool esperarTurno,
            CancellationToken cancellationToken)
        {
            if (!ModoSesionService.EsEnLinea ||
                !DatosSinConexionPermisos.TienePermiso)
            {
                return 0;
            }

            bool entered;

            if (esperarTurno)
            {
                await syncLock.WaitAsync(cancellationToken);
                entered = true;
            }
            else
            {
                entered = await syncLock.WaitAsync(
                    TimeSpan.Zero,
                    cancellationToken);
            }

            if (!entered)
                return 0;

            int sincronizados = 0;

            try
            {
                List<AnalisisOfflineLocalEntity> pendientes =
                    await AnalisisOfflineDatabaseService.Instance
                        .ListarPendientesAsync();

                foreach (AnalisisOfflineLocalEntity entity in pendientes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (entity.Estado ==
                        AnalisisOfflineEstados.RequiereRevision)
                    {
                        continue;
                    }

                    if (await SincronizarUnoAsync(
                            entity,
                            cancellationToken))
                    {
                        sincronizados++;
                    }
                }

                if (sincronizados > 0)
                {
                    AnalisisListadoEstadoService
                        .MarcarActualizacionPendiente();
                }

                return sincronizados;
            }
            finally
            {
                syncLock.Release();
                ColaCambiada?.Invoke(this, EventArgs.Empty);
            }
        }

        private static async Task<bool> SincronizarUnoAsync(
            AnalisisOfflineLocalEntity entity,
            CancellationToken cancellationToken)
        {
            await AnalisisOfflineDatabaseService.Instance
                .MarcarSincronizandoAsync(entity);

            var envelope = new
            {
                operacionLocalId = Guid.Parse(
                    entity.OperacionLocalId),
                tipoOperacion = entity.TipoOperacion,
                analisisSueloCalculoId =
                    entity.AnalisisSueloCalculoIdServidor,
                solicitud = JsonSerializer.Deserialize<
                    GuardarTodoRequest>(
                    entity.PayloadJson,
                    JsonOptions),
                versionMotor = entity.VersionMotor,
                hashPaquete = entity.HashPaquete,
                fechaCalculoLocalUtc = ParseFecha(
                    entity.FechaCreacionUtc)
            };

            string json = JsonSerializer.Serialize(
                envelope,
                JsonOptions);

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    "api/analisis-offline/sincronizar")
                {
                    Content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json")
                };

                using var timeout =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken);

                timeout.CancelAfter(TimeSpan.FromSeconds(45));

                using HttpResponseMessage response =
                    await ApiClientService.Client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseContentRead,
                        timeout.Token);

                string respuesta = await response.Content
                    .ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    bool requiereRevision = response.StatusCode is
                        HttpStatusCode.BadRequest or
                        HttpStatusCode.Conflict or
                        HttpStatusCode.Forbidden or
                        HttpStatusCode.NotFound;

                    await AnalisisOfflineDatabaseService.Instance
                        .MarcarErrorAsync(
                            entity,
                            ExtraerMensaje(
                                respuesta,
                                "No fue posible enviar el análisis."),
                            requiereRevision);

                    return false;
                }

                using JsonDocument document =
                    JsonDocument.Parse(respuesta);

                JsonElement root = document.RootElement;

                if (!GetBool(root, "success"))
                {
                    await AnalisisOfflineDatabaseService.Instance
                        .MarcarErrorAsync(
                            entity,
                            GetString(root, "message"),
                            requiereRevision: true);
                    return false;
                }

                JsonElement data = GetProperty(root, "data");
                int analisisSueloId = GetInt(
                    data,
                    "analisisSueloId");
                int calculoId = GetInt(
                    data,
                    "analisisSueloCalculoId");

                if (analisisSueloId <= 0 || calculoId <= 0)
                {
                    await AnalisisOfflineDatabaseService.Instance
                        .MarcarErrorAsync(
                            entity,
                            "El servidor no devolvió los identificadores del análisis enviado.",
                            requiereRevision: true);
                    return false;
                }

                await AnalisisOfflineDatabaseService.Instance
                    .MarcarSincronizadoAsync(
                        entity,
                        analisisSueloId,
                        calculoId,
                        respuesta);

                return true;
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                await AnalisisOfflineDatabaseService.Instance
                    .MarcarErrorAsync(
                        entity,
                        "El envío tardó demasiado. Se intentará en el próximo inicio en línea.",
                        requiereRevision: false);
                return false;
            }
            catch (Exception ex)
            {
                await AnalisisOfflineDatabaseService.Instance
                    .MarcarErrorAsync(
                        entity,
                        "No fue posible enviar el análisis: " +
                        ex.Message,
                        requiereRevision: false);
                return false;
            }
        }

        private static DateTime ParseFecha(string? value) =>
            DateTime.TryParse(value, out DateTime result)
                ? result
                : DateTime.UtcNow;

        private static string ExtraerMensaje(
            string json,
            string fallback)
        {
            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(json);

                string value = GetString(
                    document.RootElement,
                    "message");

                return string.IsNullOrWhiteSpace(value)
                    ? fallback
                    : value;
            }
            catch
            {
                return fallback;
            }
        }

        private static JsonElement GetProperty(
            JsonElement element,
            string name)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return default;

            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }

            return default;
        }

        private static string GetString(
            JsonElement element,
            string name)
        {
            JsonElement value = GetProperty(element, name);
            return value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }

        private static int GetInt(
            JsonElement element,
            string name)
        {
            JsonElement value = GetProperty(element, name);
            return value.ValueKind == JsonValueKind.Number &&
                   value.TryGetInt32(out int result)
                ? result
                : 0;
        }

        private static bool GetBool(
            JsonElement element,
            string name) =>
            GetProperty(element, name).ValueKind ==
            JsonValueKind.True;
    }
}
