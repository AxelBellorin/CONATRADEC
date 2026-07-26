using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Coordina Catálogos, Noticias y Álbum desde una sola operación.
    ///
    /// La descarga manual garantiza una revisión completa. Después de
    /// completarse una vez, el servicio continúa verificando y actualizando en
    /// segundo plano cuando la aplicación tiene conexión.
    /// </summary>
    public sealed class SincronizacionOfflineGlobalService
    {
        private const string EstadoClavePrefijo =
            "offline.global.estado.";

        private static readonly TimeSpan
            IntervaloVerificacionAutomatica =
                TimeSpan.FromMinutes(2);

        private static readonly Lazy<
            SincronizacionOfflineGlobalService> lazy =
                new(() =>
                    new SincronizacionOfflineGlobalService());

        private readonly SemaphoreSlim syncLock =
            new(1, 1);

        private readonly object taskLock =
            new();

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private Task<
            ResultadoSincronizacionOfflineGlobal>?
                tareaActual;

        private DateTime ultimaSolicitudAutomaticaUtc;

        public static
            SincronizacionOfflineGlobalService Instance =>
                lazy.Value;

        public event EventHandler<
            SincronizacionOfflineGlobalEventArgs>?
                EstadoCambiado;

        private SincronizacionOfflineGlobalService()
        {
            EstadoConexionService.Instance
                .ConexionPotencialmenteRestablecida +=
                OnConexionPotencialmenteRestablecida;

            EstadoConexionService.Instance
                .EstadoConexionCambiado +=
                OnEstadoConexionCambiado;

            /*
             * Mantiene la verificación automática mientras la aplicación está
             * activa. En Android e iOS el sistema puede suspender el proceso;
             * al volver a primer plano el mapper solicita otra verificación.
             */
            _ = EjecutarCicloAutomaticoAsync();
        }

        public async Task<
            SincronizacionOfflineGlobalEstado>
            ObtenerEstadoAsync()
        {
            SincronizacionOfflineGlobalEstado estado =
                CargarEstado();

            return await AgregarTamanoActualAsync(
                estado);
        }

        /// <summary>
        /// Acción del botón global. Recorre completamente los módulos
        /// habilitados. Las imágenes existentes no se vuelven a descargar si
        /// ya están guardadas y son válidas.
        /// </summary>
        public Task<
            ResultadoSincronizacionOfflineGlobal>
            DescargarOActualizarTodoAsync()
        {
            if (!DatosSinConexionPermisos.TienePermiso)
            {
                return Task.FromResult(
                    ResultadoSincronizacionOfflineGlobal.Fail(
                        "Su usuario no tiene habilitado el trabajo sin conexión.",
                        conservaCopiaAnterior: false));
            }

            lock (taskLock)
            {
                if (tareaActual != null &&
                    !tareaActual.IsCompleted)
                {
                    return tareaActual;
                }

                tareaActual =
                    EjecutarAsync(
                        forzarRevisionCompleta: true,
                        esAutomatico: false,
                        CancellationToken.None);

                return tareaActual;
            }
        }

        /// <summary>
        /// Se llama al mostrar páginas después del inicio de sesión.
        ///
        /// Antes de la primera descarga global, cada módulo mantiene su
        /// comportamiento normal. Después de completarse una vez, se verifican
        /// actualizaciones globales sin que el usuario tenga que abrir la
        /// pantalla Datos sin conexión.
        /// </summary>
        public void VerificarActualizacionesEnSegundoPlano()
        {
            if (!DatosSinConexionPermisos.TienePermiso)
                return;

            SincronizacionOfflineGlobalEstado estado =
                CargarEstado();

            if (!estado.PreparacionCompleta ||
                !EstadoConexionService.Instance.HayInternet)
            {
                return;
            }

            DateTime ahora =
                DateTime.UtcNow;

            if (ahora -
                ultimaSolicitudAutomaticaUtc <
                IntervaloVerificacionAutomatica)
            {
                return;
            }

            ultimaSolicitudAutomaticaUtc = ahora;

            lock (taskLock)
            {
                if (tareaActual != null &&
                    !tareaActual.IsCompleted)
                {
                    return;
                }

                tareaActual =
                    EjecutarAsync(
                        forzarRevisionCompleta: false,
                        esAutomatico: true,
                        CancellationToken.None);
            }
        }

        public async Task MarcarActualizacionDisponibleAsync(
            string detalle)
        {
            SincronizacionOfflineGlobalEstado anterior =
                CargarEstado();

            if (!anterior.PreparacionCompleta)
                return;

            SincronizacionOfflineGlobalEstado nuevo =
                CopiarEstado(
                    anterior,
                    estado:
                        SincronizacionOfflineGlobalEstados
                            .ActualizacionDisponible,
                    mensaje:
                        "Hay datos nuevos disponibles",
                    detalle:
                        string.IsNullOrWhiteSpace(detalle)
                            ? "La aplicación actualizará los datos cuando tenga conexión."
                            : detalle,
                    progreso:
                        anterior.ProgresoPorcentaje);

            GuardarYNotificar(nuevo);

            await Task.CompletedTask;
        }

        private async Task<
            ResultadoSincronizacionOfflineGlobal>
            EjecutarAsync(
                bool forzarRevisionCompleta,
                bool esAutomatico,
                CancellationToken cancellationToken)
        {
            bool entered =
                await syncLock.WaitAsync(
                    TimeSpan.Zero,
                    cancellationToken);

            if (!entered)
            {
                return ResultadoSincronizacionOfflineGlobal.Fail(
                    "Ya existe una sincronización global en curso.",
                    conservaCopiaAnterior: true);
            }

            SincronizacionOfflineGlobalEstado anterior =
                CargarEstado();

            try
            {
                if (!DatosSinConexionPermisos.TienePermiso)
                {
                    return ResultadoSincronizacionOfflineGlobal.Fail(
                        "Su usuario no tiene habilitado el trabajo sin conexión.",
                        conservaCopiaAnterior: false);
                }

                if (!EstadoConexionService.Instance.HayInternet)
                {
                    return ResultadoSincronizacionOfflineGlobal.Fail(
                        anterior.PreparacionCompleta
                            ? "No hay conexión. Se mantienen los datos sincronizados anteriormente."
                            : "Se necesita conexión para descargar todos los datos.",
                        anterior.PreparacionCompleta);
                }

                int totalPasos =
                    2 +
                    (PuedeDescargarNoticias() ? 1 : 0) +
                    (PuedeDescargarAlbum() ? 1 : 0);

                int paso = 0;

                var motorPendiente =
                    new ModuloOfflineResumen
                    {
                        Nombre = "Motor de cálculo",
                        Estado =
                            ModuloOfflineEstados
                                .Sincronizando,
                        Mensaje =
                            "Descargando reglas y parámetros del cálculo."
                    };

                var catalogosPendiente =
                    new ModuloOfflineResumen
                    {
                        Nombre = "Catálogos",
                        Estado =
                            ModuloOfflineEstados
                                .Sincronizando,
                        Mensaje =
                            "Descargando catálogos, terrenos y datos para los cálculos."
                    };

                ModuloOfflineResumen noticiasInicial =
                    PuedeDescargarNoticias()
                        ? new ModuloOfflineResumen
                        {
                            Nombre = "Noticias",
                            Estado =
                                ModuloOfflineEstados.Pendiente,
                            Mensaje =
                                "Pendiente de sincronización."
                        }
                        : CrearModuloNoHabilitado(
                            "Noticias");

                ModuloOfflineResumen albumInicial =
                    PuedeDescargarAlbum()
                        ? new ModuloOfflineResumen
                        {
                            Nombre = "Álbum de fotos",
                            Estado =
                                ModuloOfflineEstados.Pendiente,
                            Mensaje =
                                "Pendiente de sincronización."
                        }
                        : CrearModuloNoHabilitado(
                            "Álbum de fotos");

                SincronizacionOfflineGlobalEstado trabajando =
                    new()
                    {
                        Estado =
                            SincronizacionOfflineGlobalEstados
                                .Sincronizando,
                        Mensaje =
                            esAutomatico
                                ? "Buscando actualizaciones..."
                                : "Preparando todos los datos...",
                        Detalle =
                            "Puede continuar usando la aplicación mientras finaliza.",
                        ProgresoPorcentaje = 0,
                        PasoActual = 0,
                        TotalPasos = totalPasos,
                        PreparacionCompleta =
                            anterior.PreparacionCompleta,
                        UltimaSincronizacionCompletaUtc =
                            anterior
                                .UltimaSincronizacionCompletaUtc,
                        UltimaVerificacionUtc =
                            DateTime.UtcNow,
                        TamanoTotalBytes =
                            anterior.TamanoTotalBytes,
                        MotorCalculo =
                            motorPendiente,
                        Catalogos =
                            catalogosPendiente,
                        Noticias =
                            noticiasInicial,
                        Album =
                            albumInicial
                    };

                GuardarYNotificar(trabajando);

                ResultadoDescargaMotor motor =
                    await MotorCalculoPaqueteService.Instance
                        .DescargarOActualizarAsync(
                            forzar:
                                forzarRevisionCompleta,
                            cancellationToken:
                                cancellationToken);

                if (!motor.Success)
                {
                    return await CompletarConErrorAsync(
                        anterior,
                        trabajando,
                        "Motor de cálculo",
                        motor.Message,
                        esAutomatico);
                }

                paso++;

                trabajando =
                    CopiarEstado(
                        trabajando,
                        mensaje:
                            "Motor de cálculo preparado",
                        detalle:
                            "Continuando con catálogos y terrenos.",
                        progreso:
                            CalcularProgreso(
                                paso,
                                totalPasos),
                        pasoActual:
                            paso,
                        motorCalculo:
                            new ModuloOfflineResumen
                            {
                                Nombre =
                                    "Motor de cálculo",
                                Estado =
                                    ModuloOfflineEstados.Listo,
                                Mensaje =
                                    $"Requerimiento anual disponible. Versión {motor.VersionPaquete}.",
                                Registros =
                                    motor.TotalRegistros
                            },
                        catalogos:
                            new ModuloOfflineResumen
                            {
                                Nombre =
                                    "Catálogos",
                                Estado =
                                    ModuloOfflineEstados
                                        .Sincronizando,
                                Mensaje =
                                    "Descargando catálogos, terrenos y datos para los cálculos."
                            });

                GuardarYNotificar(trabajando);

                ResultadoDescargaOffline catalogos =
                    await PaqueteCatalogosOfflineService.Instance
                        .DescargarTodoAsync(
                            forzar:
                                forzarRevisionCompleta);

                if (!catalogos.Success)
                {
                    return await CompletarConErrorAsync(
                        anterior,
                        trabajando,
                        "Catálogos",
                        catalogos.Message,
                        esAutomatico);
                }

                paso++;

                trabajando =
                    CopiarEstado(
                        trabajando,
                        mensaje:
                            "Catálogos preparados",
                        detalle:
                            "Continuando con el contenido informativo.",
                        progreso:
                            CalcularProgreso(
                                paso,
                                totalPasos),
                        pasoActual:
                            paso,
                        catalogos:
                            new ModuloOfflineResumen
                            {
                                Nombre = "Catálogos",
                                Estado =
                                    ModuloOfflineEstados.Listo,
                                Mensaje =
                                    "Catálogos, terrenos, selectores y datos de cálculo disponibles.",
                                Registros =
                                    catalogos.TotalRegistros
                            });

                GuardarYNotificar(trabajando);

                if (PuedeDescargarNoticias())
                {
                    trabajando =
                        CopiarEstado(
                            trabajando,
                            mensaje:
                                "Sincronizando noticias...",
                            detalle:
                                "Descargando publicaciones, detalles e imágenes.",
                            noticias:
                                new ModuloOfflineResumen
                                {
                                    Nombre = "Noticias",
                                    Estado =
                                        ModuloOfflineEstados
                                            .Sincronizando,
                                    Mensaje =
                                        "Descargando publicaciones e imágenes."
                                });

                    GuardarYNotificar(trabajando);

                    NoticiasOfflineSyncResult noticias =
                        await NoticiasOfflineSyncService.Instance
                            .SincronizarSiNecesarioAsync(
                                forzarRevisionCompleta,
                                cancellationToken);

                    if (!noticias.Success)
                    {
                        return await CompletarConErrorAsync(
                            anterior,
                            trabajando,
                            "Noticias",
                            noticias.Message,
                            esAutomatico);
                    }

                    paso++;

                    trabajando =
                        CopiarEstado(
                            trabajando,
                            progreso:
                                CalcularProgreso(
                                    paso,
                                    totalPasos),
                            pasoActual:
                                paso,
                            noticias:
                                new ModuloOfflineResumen
                                {
                                    Nombre = "Noticias",
                                    Estado =
                                        ModuloOfflineEstados.Listo,
                                    Mensaje =
                                        "Publicaciones, detalles e imágenes disponibles.",
                                    Registros =
                                        noticias.TotalPublicaciones
                                });

                    GuardarYNotificar(trabajando);
                }

                if (PuedeDescargarAlbum())
                {
                    trabajando =
                        CopiarEstado(
                            trabajando,
                            mensaje:
                                "Sincronizando álbum...",
                            detalle:
                                "Descargando categorías, registros y fotografías.",
                            album:
                                new ModuloOfflineResumen
                                {
                                    Nombre = "Álbum de fotos",
                                    Estado =
                                        ModuloOfflineEstados
                                            .Sincronizando,
                                    Mensaje =
                                        "Descargando registros y fotografías."
                                });

                    GuardarYNotificar(trabajando);

                    AlbumOfflineSyncResult album =
                        await AlbumOfflineSyncService.Instance
                            .SincronizarSiNecesarioAsync(
                                forzarDescargaCompleta:
                                    forzarRevisionCompleta,
                                cancellationToken:
                                    cancellationToken);

                    if (!album.Success)
                    {
                        return await CompletarConErrorAsync(
                            anterior,
                            trabajando,
                            "Álbum de fotos",
                            album.Message,
                            esAutomatico);
                    }

                    paso++;

                    trabajando =
                        CopiarEstado(
                            trabajando,
                            progreso:
                                CalcularProgreso(
                                    paso,
                                    totalPasos),
                            pasoActual:
                                paso,
                            album:
                                new ModuloOfflineResumen
                                {
                                    Nombre = "Álbum de fotos",
                                    Estado =
                                        ModuloOfflineEstados.Listo,
                                    Mensaje =
                                        "Registros y fotografías disponibles.",
                                    Registros =
                                        album.TotalRecords > 0
                                            ? album.TotalRecords
                                            : anterior.Album.Registros,
                                    Imagenes =
                                        album.TotalPhotos > 0
                                            ? album.TotalPhotos
                                            : anterior.Album.Imagenes
                                });

                    GuardarYNotificar(trabajando);
                }

                /*
                 * El límite elimina únicamente archivos huérfanos. Las
                 * imágenes de las versiones vigentes se conservan para que
                 * Descargar todo signifique realmente tener todo disponible.
                 */
                await ImagenLocalCacheService
                    .AplicarLimiteAsync();

                DateTime ahora =
                    DateTime.UtcNow;

                SincronizacionOfflineGlobalEstado completo =
                    CopiarEstado(
                        trabajando,
                        estado:
                            SincronizacionOfflineGlobalEstados.Listo,
                        mensaje:
                            "Listo para trabajar sin conexión",
                        detalle:
                            "Motor, catálogos, terrenos y contenido habilitado están guardados en el dispositivo.",
                        progreso: 100,
                        pasoActual:
                            totalPasos,
                        preparacionCompleta:
                            true,
                        ultimaSincronizacionCompletaUtc:
                            ahora,
                        ultimaVerificacionUtc:
                            ahora);

                completo =
                    await AgregarTamanoActualAsync(
                        completo);

                GuardarYNotificar(completo);

                return ResultadoSincronizacionOfflineGlobal.Ok(
                    esAutomatico
                        ? "Los datos fueron verificados y actualizados."
                        : "Todos los datos necesarios fueron preparados.");
            }
            catch (Exception ex)
            {
                return await CompletarConErrorAsync(
                    anterior,
                    CargarEstado(),
                    "Sincronización",
                    ex.Message,
                    esAutomatico);
            }
            finally
            {
                syncLock.Release();
            }
        }

        private async Task<
            ResultadoSincronizacionOfflineGlobal>
            CompletarConErrorAsync(
                SincronizacionOfflineGlobalEstado anterior,
                SincronizacionOfflineGlobalEstado actual,
                string modulo,
                string mensajeError,
                bool esAutomatico)
        {
            bool conserva =
                anterior.PreparacionCompleta;

            ModuloOfflineResumen motorCalculo =
                actual.MotorCalculo;

            ModuloOfflineResumen catalogos =
                actual.Catalogos;

            ModuloOfflineResumen noticias =
                actual.Noticias;

            ModuloOfflineResumen album =
                actual.Album;

            var moduloError =
                new ModuloOfflineResumen
                {
                    Nombre = modulo,
                    Estado =
                        ModuloOfflineEstados.Error,
                    Mensaje =
                        string.IsNullOrWhiteSpace(
                            mensajeError)
                            ? "No fue posible completar la operación."
                            : mensajeError
                };

            if (modulo.Equals(
                    "Motor de cálculo",
                    StringComparison.OrdinalIgnoreCase))
            {
                motorCalculo = moduloError;
            }
            else if (modulo.Equals(
                    "Catálogos",
                    StringComparison.OrdinalIgnoreCase))
            {
                catalogos = moduloError;
            }
            else if (modulo.Equals(
                         "Noticias",
                         StringComparison.OrdinalIgnoreCase))
            {
                noticias = moduloError;
            }
            else if (modulo.Equals(
                         "Álbum de fotos",
                         StringComparison.OrdinalIgnoreCase))
            {
                album = moduloError;
            }

            SincronizacionOfflineGlobalEstado estado =
                CopiarEstado(
                    actual,
                    estado:
                        conserva
                            ? SincronizacionOfflineGlobalEstados
                                .ListoConAviso
                            : SincronizacionOfflineGlobalEstados.Error,
                    mensaje:
                        conserva
                            ? "Se conserva la última copia completa"
                            : "No se completó la descarga",
                    detalle:
                        $"{modulo}: " +
                        (string.IsNullOrWhiteSpace(
                            mensajeError)
                            ? "No fue posible completar la operación."
                            : mensajeError),
                    preparacionCompleta:
                        conserva,
                    ultimaSincronizacionCompletaUtc:
                        anterior
                            .UltimaSincronizacionCompletaUtc,
                    ultimaVerificacionUtc:
                        DateTime.UtcNow,
                    motorCalculo:
                        motorCalculo,
                    catalogos:
                        catalogos,
                    noticias:
                        noticias,
                    album:
                        album);

            estado =
                await AgregarTamanoActualAsync(
                    estado);

            GuardarYNotificar(estado);

            return ResultadoSincronizacionOfflineGlobal.Fail(
                estado.Detalle,
                conserva);
        }

        private static int CalcularProgreso(
            int paso,
            int totalPasos)
        {
            if (totalPasos <= 0)
                return 0;

            return Math.Clamp(
                (int)Math.Round(
                    paso * 100d /
                    totalPasos),
                0,
                100);
        }

        private async Task<
            SincronizacionOfflineGlobalEstado>
            AgregarTamanoActualAsync(
                SincronizacionOfflineGlobalEstado estado)
        {
            long total =
                ImagenLocalCacheService
                    .ObtenerTamanoFisicoBytes();

            try
            {
                string path =
                    ContenidoLocalDatabaseService.Instance
                        .DatabasePath;

                if (File.Exists(path))
                    total += new FileInfo(path).Length;
            }
            catch
            {
            }

            total +=
                MotorCalculoPaqueteService.Instance
                    .ObtenerTamanoPaqueteBytes();

            await Task.CompletedTask;

            return CopiarEstado(
                estado,
                tamanoTotalBytes:
                    total);
        }

        private static bool PuedeDescargarNoticias() =>
            PermissionService.Instance.HasRead(
                InterfazCodigos.Noticias);

        private static bool PuedeDescargarAlbum() =>
            PermissionService.Instance.HasRead(
                InterfazCodigos.AlbumFotos);

        private static ModuloOfflineResumen
            CrearModuloNoHabilitado(
                string nombre) =>
                new()
                {
                    Nombre = nombre,
                    Estado =
                        ModuloOfflineEstados.NoHabilitado,
                    Mensaje =
                        "No habilitado para este usuario."
                };

        private void GuardarYNotificar(
            SincronizacionOfflineGlobalEstado estado)
        {
            string usuarioId =
                ObtenerUsuarioId();

            if (usuarioId != "0")
            {
                Preferences.Set(
                    ConstruirClaveEstado(
                        usuarioId),
                    JsonSerializer.Serialize(
                        estado,
                        jsonOptions));
            }

            EstadoCambiado?.Invoke(
                this,
                new SincronizacionOfflineGlobalEventArgs(
                    estado));
        }

        private SincronizacionOfflineGlobalEstado
            CargarEstado()
        {
            string usuarioId =
                ObtenerUsuarioId();

            if (usuarioId == "0")
                return new SincronizacionOfflineGlobalEstado();

            string json =
                Preferences.Get(
                    ConstruirClaveEstado(
                        usuarioId),
                    string.Empty);

            if (string.IsNullOrWhiteSpace(json))
                return new SincronizacionOfflineGlobalEstado();

            try
            {
                return JsonSerializer.Deserialize<
                           SincronizacionOfflineGlobalEstado>(
                           json,
                           jsonOptions)
                       ??
                       new SincronizacionOfflineGlobalEstado();
            }
            catch
            {
                return new SincronizacionOfflineGlobalEstado();
            }
        }

        private static SincronizacionOfflineGlobalEstado
            CopiarEstado(
                SincronizacionOfflineGlobalEstado origen,
                string? estado = null,
                string? mensaje = null,
                string? detalle = null,
                int? progreso = null,
                int? pasoActual = null,
                int? totalPasos = null,
                bool? preparacionCompleta = null,
                DateTime?
                    ultimaSincronizacionCompletaUtc =
                        null,
                DateTime? ultimaVerificacionUtc =
                    null,
                long? tamanoTotalBytes = null,
                ModuloOfflineResumen? motorCalculo =
                    null,
                ModuloOfflineResumen? catalogos =
                    null,
                ModuloOfflineResumen? noticias =
                    null,
                ModuloOfflineResumen? album =
                    null) =>
                new()
                {
                    Estado =
                        estado ??
                        origen.Estado,
                    Mensaje =
                        mensaje ??
                        origen.Mensaje,
                    Detalle =
                        detalle ??
                        origen.Detalle,
                    ProgresoPorcentaje =
                        progreso ??
                        origen.ProgresoPorcentaje,
                    PasoActual =
                        pasoActual ??
                        origen.PasoActual,
                    TotalPasos =
                        totalPasos ??
                        origen.TotalPasos,
                    PreparacionCompleta =
                        preparacionCompleta ??
                        origen.PreparacionCompleta,
                    UltimaSincronizacionCompletaUtc =
                        ultimaSincronizacionCompletaUtc ??
                        origen
                            .UltimaSincronizacionCompletaUtc,
                    UltimaVerificacionUtc =
                        ultimaVerificacionUtc ??
                        origen.UltimaVerificacionUtc,
                    TamanoTotalBytes =
                        tamanoTotalBytes ??
                        origen.TamanoTotalBytes,
                    MotorCalculo =
                        motorCalculo ??
                        origen.MotorCalculo,
                    Catalogos =
                        catalogos ??
                        origen.Catalogos,
                    Noticias =
                        noticias ??
                        origen.Noticias,
                    Album =
                        album ??
                        origen.Album
                };

        private static string ObtenerUsuarioId()
        {
            string usuarioId =
                Preferences.Get(
                    SessionKeys.KeyUserId,
                    string.Empty);

            return string.IsNullOrWhiteSpace(
                    usuarioId)
                ? "0"
                : usuarioId;
        }

        private static string ConstruirClaveEstado(
            string usuarioId) =>
            EstadoClavePrefijo +
            usuarioId;

        private async Task EjecutarCicloAutomaticoAsync()
        {
            try
            {
                using var timer =
                    new PeriodicTimer(
                        IntervaloVerificacionAutomatica);

                while (await timer.WaitForNextTickAsync())
                {
                    VerificarActualizacionesEnSegundoPlano();
                }
            }
            catch
            {
                /*
                 * Un error del ciclo no debe afectar la aplicación. Las
                 * verificaciones por navegación y reconexión siguen activas.
                 */
            }
        }

        private void OnConexionPotencialmenteRestablecida()
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(2));

                VerificarActualizacionesEnSegundoPlano();
            });
        }

        private void OnEstadoConexionCambiado(
            bool conectado)
        {
            if (conectado)
                VerificarActualizacionesEnSegundoPlano();
        }
    }
}
