using CONATRADEC.Models;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Envía en segundo plano los análisis guardados localmente.
    ///
    /// La cola se procesa al recuperar conexión, después de guardar y mediante
    /// un ciclo periódico. El endpoint utiliza operacionLocalId para evitar
    /// duplicados aunque una misma solicitud se repita.
    /// </summary>
    public sealed class AnalisisOfflineSincronizacionService
    {
        private static readonly Lazy<
            AnalisisOfflineSincronizacionService> lazy =
                new(() =>
                    new AnalisisOfflineSincronizacionService());

        private readonly SemaphoreSlim syncLock =
            new(1, 1);

        private int iniciado;

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

        public void Iniciar()
        {
            if (Interlocked.Exchange(
                    ref iniciado,
                    1) == 1)
            {
                return;
            }

            EstadoConexionService.Instance
                .ConexionPotencialmenteRestablecida +=
                OnConexionPotencialmenteRestablecida;

            EstadoConexionService.Instance
                .EstadoConexionCambiado +=
                OnEstadoConexionCambiado;

            AnalisisOfflineDatabaseService.Instance
                .DatosCambiados +=
                OnDatosCambiados;

            /*
             * Solo se registran eventos y el temporizador. No se consulta la API
             * durante la construcción del HttpClient, evitando acceso recursivo
             * a ApiClientService.Client.
             */
            _ = EjecutarCicloAsync();
        }

        public void SolicitarSincronizacion()
        {
            _ = Task.Run(
                async () =>
                    await SincronizarPendientesAsync());
        }

        public async Task<int> SincronizarPendientesAsync(
            CancellationToken cancellationToken = default)
        {
            if (!DatosSinConexionPermisos.TienePermiso)
                return 0;

            bool entered =
                await syncLock.WaitAsync(
                    TimeSpan.Zero,
                    cancellationToken);

            if (!entered)
                return 0;

            int sincronizados = 0;

            try
            {
                bool apiDisponible =
                    await EstadoConexionApiService.Instance
                        .ComprobarAsync(
                            "noticias",
                            cancellationToken);

                if (!apiDisponible)
                    return 0;

                List<AnalisisOfflineLocalEntity> pendientes =
                    await AnalisisOfflineDatabaseService
                        .Instance
                        .ListarPendientesAsync();

                foreach (AnalisisOfflineLocalEntity entity
                         in pendientes)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    if (entity.Estado ==
                        AnalisisOfflineEstados
                            .RequiereRevision)
                    {
                        continue;
                    }

                    bool ok =
                        await SincronizarUnoAsync(
                            entity,
                            cancellationToken);

                    if (ok)
                        sincronizados++;
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
                ColaCambiada?.Invoke(
                    this,
                    EventArgs.Empty);
            }
        }

        private static async Task<bool> SincronizarUnoAsync(
            AnalisisOfflineLocalEntity entity,
            CancellationToken cancellationToken)
        {
            await AnalisisOfflineDatabaseService.Instance
                .MarcarSincronizandoAsync(entity);

            var envelope =
                new
                {
                    operacionLocalId =
                        Guid.Parse(
                            entity.OperacionLocalId),

                    tipoOperacion =
                        entity.TipoOperacion,

                    analisisSueloCalculoId =
                        entity
                            .AnalisisSueloCalculoIdServidor,

                    solicitud =
                        JsonSerializer.Deserialize<
                            GuardarTodoRequest>(
                            entity.PayloadJson,
                            JsonOptions),

                    versionMotor =
                        entity.VersionMotor,

                    hashPaquete =
                        entity.HashPaquete,

                    fechaCalculoLocalUtc =
                        ParseFecha(
                            entity.FechaCreacionUtc)
                };

            string json =
                JsonSerializer.Serialize(
                    envelope,
                    JsonOptions);

            try
            {
                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        "api/analisis-offline/sincronizar")
                    {
                        Content =
                            new StringContent(
                                json,
                                Encoding.UTF8,
                                "application/json")
                    };

                using var timeout =
                    CancellationTokenSource
                        .CreateLinkedTokenSource(
                            cancellationToken);

                timeout.CancelAfter(
                    TimeSpan.FromSeconds(45));

                using HttpResponseMessage response =
                    await ApiClientService.Client
                        .SendAsync(
                            request,
                            HttpCompletionOption
                                .ResponseContentRead,
                            timeout.Token);

                string respuesta =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    bool requiereRevision =
                        response.StatusCode is
                            HttpStatusCode.BadRequest or
                            HttpStatusCode.Conflict or
                            HttpStatusCode.Forbidden or
                            HttpStatusCode.NotFound;

                    await AnalisisOfflineDatabaseService
                        .Instance
                        .MarcarErrorAsync(
                            entity,
                            ExtraerMensaje(
                                respuesta,
                                "No fue posible sincronizar el análisis."),
                            requiereRevision);

                    return false;
                }

                using JsonDocument document =
                    JsonDocument.Parse(respuesta);

                JsonElement root =
                    document.RootElement;

                if (!GetBool(root, "success"))
                {
                    await AnalisisOfflineDatabaseService
                        .Instance
                        .MarcarErrorAsync(
                            entity,
                            GetString(root, "message"),
                            requiereRevision: true);

                    return false;
                }

                JsonElement data =
                    GetProperty(root, "data");

                int analisisSueloId =
                    GetInt(
                        data,
                        "analisisSueloId");

                int calculoId =
                    GetInt(
                        data,
                        "analisisSueloCalculoId");

                if (analisisSueloId <= 0 ||
                    calculoId <= 0)
                {
                    await AnalisisOfflineDatabaseService
                        .Instance
                        .MarcarErrorAsync(
                            entity,
                            "El servidor no devolvió los identificadores del análisis sincronizado.",
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
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                await AnalisisOfflineDatabaseService.Instance
                    .MarcarErrorAsync(
                        entity,
                        "La sincronización tardó demasiado. Se intentará nuevamente.",
                        requiereRevision: false);

                return false;
            }
            catch (Exception ex)
            {
                EstadoConexionService.Instance
                    .ReportarServidorNoDisponible();

                await AnalisisOfflineDatabaseService.Instance
                    .MarcarErrorAsync(
                        entity,
                        "No fue posible conectar con la API: " +
                        ex.Message,
                        requiereRevision: false);

                return false;
            }
        }

        private async Task EjecutarCicloAsync()
        {
            try
            {
                using var timer =
                    new PeriodicTimer(
                        TimeSpan.FromMinutes(1));

                while (await timer
                    .WaitForNextTickAsync())
                {
                    await SincronizarPendientesAsync();
                }
            }
            catch
            {
                /*
                 * El ciclo nunca debe cerrar la aplicación. Los eventos de
                 * reconexión y los guardados locales seguirán reintentando.
                 */
            }
        }

        private void OnConexionPotencialmenteRestablecida()
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(2));

                await SincronizarPendientesAsync();
            });
        }

        private void OnEstadoConexionCambiado(
            bool conectado)
        {
            if (conectado)
                SolicitarSincronizacion();
        }

        private void OnDatosCambiados(
            object? sender,
            EventArgs e)
        {
            ColaCambiada?.Invoke(
                this,
                EventArgs.Empty);

            if (EstadoConexionService.Instance
                .HayInternet)
            {
                SolicitarSincronizacion();
            }
        }

        private static DateTime ParseFecha(
            string? value) =>
            DateTime.TryParse(
                value,
                out DateTime result)
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

                string value =
                    GetString(
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
            if (element.ValueKind !=
                JsonValueKind.Object)
            {
                return default;
            }

            foreach (JsonProperty property
                     in element.EnumerateObject())
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
            JsonElement value =
                GetProperty(element, name);

            return value.ValueKind ==
                    JsonValueKind.String
                ? value.GetString() ??
                  string.Empty
                : string.Empty;
        }

        private static int GetInt(
            JsonElement element,
            string name)
        {
            JsonElement value =
                GetProperty(element, name);

            return value.ValueKind ==
                    JsonValueKind.Number &&
                   value.TryGetInt32(
                       out int result)
                ? result
                : 0;
        }

        private static bool GetBool(
            JsonElement element,
            string name)
        {
            JsonElement value =
                GetProperty(element, name);

            return value.ValueKind ==
                JsonValueKind.True;
        }
    }
}
