using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Estado global y persistente del centro de actualizaciones.
    ///
    /// La descarga puede sobrevivir al cambio de pantalla y, en los mecanismos
    /// compatibles, recuperarse al volver a abrir la app. La frescura de una
    /// visita es independiente: cada nueva instancia de la página ejecuta una
    /// comprobación fresca al servidor.
    /// </summary>
    public sealed class ActualizacionEstadoService :
        INotifyPropertyChanged
    {
        private const string ClaveActualizacion =
            "Actualizaciones.Estado.Actualizacion";

        private const string ClaveRutaDescargada =
            "Actualizaciones.Estado.RutaDescargada";

        private const string ClaveDescargaSolicitada =
            "Actualizaciones.Estado.DescargaSolicitada";

        private const string ClaveUltimaComprobacion =
            "Actualizaciones.Estado.UltimaComprobacion";

        private static readonly Lazy<ActualizacionEstadoService>
            instancia =
                new(() => new ActualizacionEstadoService());

        private readonly SemaphoreSlim inicializacionSemaforo =
            new(1, 1);

        private readonly SemaphoreSlim comprobacionSemaforo =
            new(1, 1);

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web);

        private ActualizacionDisponible? actualizacion;
        private CancellationTokenSource? descargaCts;
        private Task? descargaActiva;
        private string? rutaDescargada;

        private bool inicializado;
        private bool comprobacionRealizada;
        private bool actualizacionConfirmadaEnServidor;
        private bool consultando;
        private bool descargando;
        private bool instalando;
        private bool esperandoPermisoInstalacion;

        private double progresoDescarga;
        private long bytesDescargados;
        private long totalBytes;
        private double bytesPorSegundo;
        private TimeSpan? tiempoRestante;

        private string estadoDescarga =
            "Preparando descarga...";

        private string mensajeEstado =
            "Compruebe si existe una versión nueva de ConatraCafé Soil.";

        private DateTime? ultimaComprobacionLocal;

        public static ActualizacionEstadoService Instance =>
            instancia.Value;

        private ActualizacionEstadoService()
        {
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ActualizacionDisponible? Actualizacion =>
            actualizacion;

        public string VersionInstalada =>
            $"{AppInfo.Current.VersionString} " +
            $"({AppInfo.Current.BuildString})";

        public string VersionNueva =>
            actualizacion is null
                ? "—"
                : $"{actualizacion.VersionNombre} " +
                  $"({actualizacion.VersionCodigo})";

        public string NuevaVersionTitulo =>
            actualizacion is null
                ? "ConatraCafé Soil está al día"
                : $"ConatraCafé Soil " +
                  $"{actualizacion.VersionNombre}";

        public string ResumenVersion =>
            actualizacion is null
                ? "No hay una actualización pendiente."
                : $"Canal " +
                  $"{actualizacion.Canal.ToLowerInvariant()} · " +
                  $"compilación {actualizacion.VersionCodigo}";

        public string PlataformaVisible =>
            actualizacion is null
                ? ObtenerPlataformaVisibleActual()
                : actualizacion.Plataforma.Equals(
                    "ANDROID",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Android"
                    : "Windows";

        public string TamanoVisible =>
            actualizacion?.TamanoVisible ??
            "—";

        public string NotasVersion =>
            string.IsNullOrWhiteSpace(
                actualizacion?.NotasVersion)
                ? "Esta versión incluye mejoras generales y " +
                  "correcciones de estabilidad."
                : actualizacion.NotasVersion;

        public bool EsObligatoria =>
            actualizacion?.Obligatoria == true;

        public bool TieneActualizacion =>
            actualizacion is not null;

        public bool NoTieneActualizacion =>
            !TieneActualizacion;

        public bool TieneArchivoDescargado =>
            !string.IsNullOrWhiteSpace(rutaDescargada) &&
            File.Exists(rutaDescargada);

        public bool Consultando
        {
            get => consultando;
            private set
            {
                if (consultando == value)
                    return;

                consultando = value;
                NotificarTodo();
            }
        }

        public bool Descargando
        {
            get => descargando;
            private set
            {
                if (descargando == value)
                    return;

                descargando = value;
                NotificarTodo();
            }
        }

        public bool Instalando
        {
            get => instalando;
            private set
            {
                if (instalando == value)
                    return;

                instalando = value;
                NotificarTodo();
            }
        }

        public bool EsperandoPermisoInstalacion
        {
            get => esperandoPermisoInstalacion;
            private set
            {
                if (esperandoPermisoInstalacion == value)
                    return;

                esperandoPermisoInstalacion = value;
                NotificarTodo();
            }
        }

        public bool Ocupada =>
            Consultando ||
            Descargando ||
            Instalando;

        /// <summary>
        /// Operaciones cortas que deben bloquear completamente la interfaz.
        /// La transferencia larga queda excluida para que el usuario pueda ver
        /// progreso, cancelar o navegar mientras el sistema la administra.
        /// </summary>
        public bool BloqueoInterfaz =>
            Consultando ||
            Instalando;

        public bool PuedeBuscar =>
            !Ocupada;

        /// <summary>
        /// Una obligación guardada localmente no puede encerrar al usuario. El
        /// cierre solo se bloquea cuando la versión obligatoria fue confirmada
        /// por el servidor durante la sesión/visita actual.
        /// </summary>
        public bool PuedeCerrar =>
            !EsObligatoria ||
            !actualizacionConfirmadaEnServidor;

        public bool MostrandoProgreso =>
            Descargando;

        public bool PuedeCancelarDescarga =>
            Descargando &&
            descargaCts is not null;

        public bool PuedeEjecutarPrincipal =>
            !Ocupada;

        public bool DebeComprobarAlAbrir =>
            inicializado &&
            !comprobacionRealizada &&
            !Descargando;

        public string DescripcionPersistenciaDescarga =>
#if ANDROID
            "Puede cambiar de pantalla o minimizar la aplicación. Android continuará la transferencia y el progreso se recuperará al volver.";
#elif WINDOWS
            "Puede cambiar de pantalla mientras descarga. Si cierra la aplicación, Windows conservará el archivo parcial y continuará desde ese punto la próxima vez.";
#else
            "La descarga puede conservar su progreso mientras la aplicación permanezca disponible.";
#endif

        public string TextoBotonPrincipal
        {
            get
            {
                if (Consultando)
                    return "Buscando actualizaciones...";

                if (Descargando)
                    return "Descargando actualización...";

                if (Instalando)
                    return "Abriendo instalador...";

                if (TieneArchivoDescargado)
                {
                    return EsperandoPermisoInstalacion
                        ? "Continuar instalación"
                        : "Instalar actualización";
                }

                if (TieneActualizacion)
                    return "Descargar e instalar";

                return "Buscar actualizaciones";
            }
        }

        public string TituloEstadoGeneral
        {
            get
            {
                if (Consultando)
                    return "Buscando actualizaciones";

                if (TieneActualizacion)
                    return "Actualización disponible";

                if (comprobacionRealizada)
                    return "Aplicación actualizada";

                return "Centro de actualizaciones";
            }
        }

        public string DescripcionEstadoGeneral
        {
            get
            {
                if (Consultando)
                {
                    return "Estamos consultando la versión más reciente " +
                           "publicada para este dispositivo.";
                }

                if (TieneActualizacion)
                {
                    return "Puede descargarla ahora o volver más tarde. " +
                           "La descarga conserva su estado de forma segura.";
                }

                if (comprobacionRealizada)
                {
                    return "ConatraCafé Soil ya tiene la versión más " +
                           "reciente disponible para este canal.";
                }

                return "Busque, descargue e instale nuevas versiones de " +
                       "ConatraCafé Soil.";
            }
        }

        public double ProgresoDescarga
        {
            get => progresoDescarga;
            private set
            {
                double valor = Math.Clamp(
                    value,
                    0,
                    1);

                if (Math.Abs(
                        progresoDescarga - valor) < 0.0001)
                {
                    return;
                }

                progresoDescarga = valor;
                Notificar();
                Notificar(nameof(PorcentajeTexto));
            }
        }

        public string PorcentajeTexto =>
            $"{ProgresoDescarga * 100:0}%";

        public string EstadoDescarga
        {
            get => estadoDescarga;
            private set
            {
                if (estadoDescarga == value)
                    return;

                estadoDescarga = value;
                Notificar();
            }
        }

        public string DetalleDescarga =>
            $"{FormatearTamano(bytesDescargados)} de " +
            $"{FormatearTamano(totalBytes)}";

        public string VelocidadTexto =>
            bytesPorSegundo <= 0
                ? "Calculando velocidad..."
                : $"{FormatearTamano((long)bytesPorSegundo)}/s";

        public string TiempoRestanteTexto =>
            FormatearTiempoRestante(tiempoRestante);

        public string MensajeEstado
        {
            get => mensajeEstado;
            private set
            {
                if (mensajeEstado == value)
                    return;

                mensajeEstado = value;
                Notificar();
                Notificar(nameof(TieneMensajeEstado));
            }
        }

        public bool TieneMensajeEstado =>
            !string.IsNullOrWhiteSpace(MensajeEstado);

        public string UltimaComprobacionTexto =>
            ultimaComprobacionLocal.HasValue
                ? "Última comprobación: " +
                  ultimaComprobacionLocal.Value
                      .ToString(
                          "dd/MM/yyyy hh:mm tt",
                          CultureInfo.CurrentCulture)
                : "Todavía no se ha comprobado en este dispositivo.";

        /// <summary>
        /// Recupera únicamente metadatos no sensibles y el estado de la
        /// transferencia. Las credenciales temporales nunca se leen de
        /// Preferences porque nunca se guardan allí.
        /// </summary>
        public async Task InicializarAsync()
        {
            if (inicializado)
                return;

            await inicializacionSemaforo.WaitAsync();

            try
            {
                if (inicializado)
                    return;

                CargarUltimaComprobacion();

                ActualizacionDisponible? guardada =
                    CargarActualizacionPersistida();

                if (guardada is not null &&
                    guardada.VersionCodigo <=
                    ActualizacionAplicacionService
                        .ObtenerVersionCodigoInstalada())
                {
                    LimpiarPersistencia(
                        eliminarArchivo: true);

                    guardada = null;
                    comprobacionRealizada = true;
                }

                actualizacion = guardada;
                actualizacionConfirmadaEnServidor = false;

                string ruta = Preferences.Get(
                    ClaveRutaDescargada,
                    string.Empty);

                if (actualizacion is not null &&
                    await EsArchivoValidoAsync(
                        ruta,
                        actualizacion))
                {
                    rutaDescargada = ruta;
                    comprobacionRealizada = true;
                    MensajeEstado =
                        "La actualización ya fue descargada y verificada. " +
                        "Se confirmará nuevamente con el servidor al abrir el centro.";
                }
                else
                {
                    LimpiarRutaDescargadaPersistida(
                        eliminarArchivo: true);

                    if (actualizacion is not null)
                    {
                        comprobacionRealizada = true;
                        MensajeEstado =
                            "Hay una actualización guardada en este dispositivo. " +
                            "Se confirmará nuevamente con el servidor.";
                    }
                }

                inicializado = true;
                ReiniciarProgreso();
                NotificarTodo();

            }
            finally
            {
                inicializacionSemaforo.Release();
            }
        }

        /// <summary>
        /// Registra una actualización obtenida en una comprobación fresca del
        /// login. Se considera confirmada por el servidor.
        /// </summary>
        public async Task EstablecerActualizacionAsync(
            ActualizacionDisponible disponible)
        {
            ArgumentNullException.ThrowIfNull(disponible);

            await InicializarAsync();

            await EstablecerActualizacionInternaAsync(
                disponible,
                confirmadaEnServidor: true);
        }

        /// <summary>
        /// Ejecuta una comprobación fresca. Una nueva visita a la página llama
        /// siempre a este método, incluso cuando existe estado persistido.
        /// </summary>
        public async Task ComprobarAsync(
            CancellationToken cancellationToken = default)
        {
            await InicializarAsync();

            if (Descargando || Instalando)
                return;

            await comprobacionSemaforo.WaitAsync(
                cancellationToken);

            try
            {
                if (Descargando || Instalando)
                    return;

                actualizacionConfirmadaEnServidor = false;
                Consultando = true;
                MensajeEstado =
                    "Consultando la versión más reciente...";

                ActualizacionDisponible? disponible =
                    await ActualizacionAplicacionService
                        .Instance
                        .ComprobarActualizacionAsync(
                            cancellationToken);

                if (disponible is null)
                {
                    ActualizacionDisponible? anterior = actualizacion;

                    LimpiarPersistencia(
                        eliminarArchivo: true);

                    if (anterior is not null)
                    {
                        ActualizacionAplicacionService.Instance
                            .EliminarDescargaParcial(anterior);
                    }

                    actualizacion = null;
                    comprobacionRealizada = true;
                    actualizacionConfirmadaEnServidor = false;
                    EsperandoPermisoInstalacion = false;
                    MensajeEstado =
                        "ConatraCafé Soil ya tiene la versión más reciente.";
                }
                else
                {
                    await EstablecerActualizacionInternaAsync(
                        disponible,
                        confirmadaEnServidor: true);
                }

                ultimaComprobacionLocal = DateTime.Now;

                Preferences.Set(
                    ClaveUltimaComprobacion,
                    ultimaComprobacionLocal.Value
                        .ToString(
                            "O",
                            CultureInfo.InvariantCulture));
            }
            catch (OperationCanceledException)
            {
                actualizacionConfirmadaEnServidor = false;
                MensajeEstado =
                    "La comprobación fue cancelada.";

                throw;
            }
            catch (Exception ex)
            {
                /*
                 * Si no fue posible confirmar una versión obligatoria guardada,
                 * no se bloquea la salida basándose únicamente en datos locales.
                 */
                actualizacionConfirmadaEnServidor = false;
                MensajeEstado =
                    "No fue posible comprobar las actualizaciones. " +
                    ex.Message;
            }
            finally
            {
                Consultando = false;
                NotificarTodo();
                comprobacionSemaforo.Release();
            }
        }

        public async Task EjecutarAccionPrincipalAsync(
            bool instalarAutomaticamente = true)
        {
            await InicializarAsync();

            if (Ocupada)
                return;

            if (TieneArchivoDescargado)
            {
                await InstalarAsync();
                return;
            }

            if (TieneActualizacion)
            {
                await IniciarOContinuarDescargaAsync(
                    instalarAutomaticamente);
                return;
            }

            await ComprobarAsync();
        }

        public async Task IniciarOContinuarDescargaAsync(
            bool instalarAutomaticamente = true)
        {
            await InicializarAsync();

            if (actualizacion is null)
                return;

            if (TieneArchivoDescargado)
            {
                if (instalarAutomaticamente)
                    await InstalarAsync();

                return;
            }

            if (descargaActiva is not null &&
                !descargaActiva.IsCompleted)
            {
                return;
            }

            descargaCts?.Dispose();
            descargaCts = new CancellationTokenSource();

            Preferences.Set(
                ClaveDescargaSolicitada,
                true);

            Descargando = true;
            EsperandoPermisoInstalacion = false;
            ReiniciarProgreso();

            MensajeEstado =
                DescripcionPersistenciaDescarga;

            Task tarea = DescargarInternamenteAsync(
                actualizacion,
                instalarAutomaticamente,
                descargaCts.Token);

            descargaActiva = tarea;

            try
            {
                await tarea;
            }
            finally
            {
                if (ReferenceEquals(
                        descargaActiva,
                        tarea))
                {
                    descargaActiva = null;
                }
            }
        }

        public void CancelarDescarga()
        {
            if (!Descargando)
                return;

            EstadoDescarga =
                "Cancelando descarga...";

            descargaCts?.Cancel();
        }

        public async Task InstalarAsync()
        {
            await InicializarAsync();

            if (actualizacion is null ||
                string.IsNullOrWhiteSpace(rutaDescargada))
            {
                return;
            }

            if (!await EsArchivoValidoAsync(
                    rutaDescargada,
                    actualizacion))
            {
                LimpiarRutaDescargadaPersistida(
                    eliminarArchivo: true);

                rutaDescargada = null;
                EsperandoPermisoInstalacion = false;
                MensajeEstado =
                    "El archivo descargado ya no es válido. " +
                    "Debe descargarlo nuevamente.";

                NotificarTodo();
                return;
            }

            Instalando = true;
            MensajeEstado =
                "Abriendo el instalador del sistema...";

            try
            {
                ResultadoInstalacionActualizacion resultado =
                    await ActualizacionInstaladorService
                        .IniciarInstalacionAsync(
                            rutaDescargada);

                EsperandoPermisoInstalacion =
                    resultado.RequierePermiso;

                MensajeEstado = resultado.Mensaje;

                /*
                 * El archivo se conserva. Si la persona cancela el instalador
                 * del sistema puede iniciar la instalación nuevamente.
                 */
            }
            catch (Exception ex)
            {
                EsperandoPermisoInstalacion = false;
                MensajeEstado =
                    "El archivo está listo, pero no fue posible abrir el " +
                    "instalador. " + ex.Message;
            }
            finally
            {
                Instalando = false;
                NotificarTodo();
            }
        }

        private async Task EstablecerActualizacionInternaAsync(
            ActualizacionDisponible disponible,
            bool confirmadaEnServidor)
        {
            bool mismaActualizacion =
                actualizacion?.ActualizacionAplicacionId ==
                    disponible.ActualizacionAplicacionId;

            if (!mismaActualizacion)
            {
                LimpiarRutaDescargadaPersistida(
                    eliminarArchivo: true);

                if (actualizacion is not null)
                {
                    ActualizacionAplicacionService.Instance
                        .EliminarDescargaParcial(actualizacion);
                }

                rutaDescargada = null;
                Preferences.Set(
                    ClaveDescargaSolicitada,
                    false);
            }

            actualizacion = disponible;
            comprobacionRealizada = true;
            actualizacionConfirmadaEnServidor =
                confirmadaEnServidor;
            EsperandoPermisoInstalacion = false;

            GuardarActualizacionPersistida(disponible);

            string ruta = Preferences.Get(
                ClaveRutaDescargada,
                string.Empty);

            if (await EsArchivoValidoAsync(
                    ruta,
                    disponible))
            {
                rutaDescargada = ruta;
                MensajeEstado =
                    "La actualización ya fue descargada y verificada. " +
                    "Puede instalarla cuando esté listo.";
            }
            else
            {
                LimpiarRutaDescargadaPersistida(
                    eliminarArchivo: true);

                rutaDescargada = null;
                MensajeEstado =
                    disponible.Obligatoria
                        ? "Esta actualización es obligatoria para continuar."
                        : "Existe una actualización disponible para este " +
                          "dispositivo.";
            }

            totalBytes = disponible.TamanoBytes;
            NotificarTodo();

            /*
             * Una transferencia pendiente solo se reanuda después de haber
             * reconciliado la versión con el servidor. Así nunca se continúa
             * automáticamente una publicación revocada o sustituida.
             */
            bool descargaSolicitada =
                Preferences.Get(
                    ClaveDescargaSolicitada,
                    false);

            if (descargaSolicitada &&
                !TieneArchivoDescargado &&
                (descargaActiva is null || descargaActiva.IsCompleted))
            {
                _ = ReanudarDescargaSeguraAsync();
            }
        }

        private async Task DescargarInternamenteAsync(
            ActualizacionDisponible disponible,
            bool instalarAutomaticamente,
            CancellationToken cancellationToken)
        {
            var progreso =
                new Progress<ProgresoDescargaActualizacion>(
                    ActualizarProgreso);

            try
            {
                string ruta =
                    await ActualizacionAplicacionService
                        .Instance
                        .DescargarEnSegundoPlanoAsync(
                            disponible,
                            progreso,
                            cancellationToken);

                if (!await EsArchivoValidoAsync(
                        ruta,
                        disponible,
                        cancellationToken))
                {
                    throw new InvalidDataException(
                        "El archivo descargado no superó la validación de " +
                        "seguridad.");
                }

                rutaDescargada = ruta;

                Preferences.Set(
                    ClaveRutaDescargada,
                    ruta);

                Preferences.Set(
                    ClaveDescargaSolicitada,
                    false);

                Descargando = false;
                EstadoDescarga =
                    "Descarga completada y verificada.";

                MensajeEstado =
                    "La actualización está lista para instalar.";

                NotificarTodo();

                if (instalarAutomaticamente)
                    await InstalarAsync();
            }
            catch (ActualizacionYaNoDisponibleException ex)
            {
                rutaDescargada = null;
                Descargando = false;
                EsperandoPermisoInstalacion = false;
                actualizacionConfirmadaEnServidor = false;

                LimpiarPersistencia(
                    eliminarArchivo: true);

                ActualizacionAplicacionService.Instance
                    .EliminarDescargaParcial(disponible);

                comprobacionRealizada = true;
                ReiniciarProgreso();

                MensajeEstado =
                    "La actualización pendiente ya no está disponible. " +
                    ex.Message;
            }
            catch (OperationCanceledException)
            {
                rutaDescargada = null;
                Descargando = false;
                EsperandoPermisoInstalacion = false;

                Preferences.Set(
                    ClaveDescargaSolicitada,
                    false);

                LimpiarRutaDescargadaPersistida(
                    eliminarArchivo: true);

                /* Cancelar explícitamente sí descarta el .part de Windows. */
                ActualizacionAplicacionService.Instance
                    .EliminarDescargaParcial(disponible);

                ReiniciarProgreso();
                MensajeEstado =
                    "La descarga fue cancelada.";
            }
            catch (Exception ex)
            {
                rutaDescargada = null;
                Descargando = false;
                EsperandoPermisoInstalacion = false;

                Preferences.Set(
                    ClaveDescargaSolicitada,
                    false);

                LimpiarRutaDescargadaPersistida(
                    eliminarArchivo: true);

                /*
                 * En Windows el .part se conserva ante un error de red para que
                 * el siguiente intento continúe desde lo ya recibido.
                 */
                ReiniciarProgreso();
                MensajeEstado =
                    "No fue posible completar la actualización. " +
                    ex.Message;
            }
            finally
            {
                Descargando = false;

                descargaCts?.Dispose();
                descargaCts = null;

                NotificarTodo();
            }
        }

        private async Task ReanudarDescargaSeguraAsync()
        {
            try
            {
                await IniciarOContinuarDescargaAsync(
                    instalarAutomaticamente: false);
            }
            catch
            {
                /*
                 * DescargarInternamenteAsync deja cualquier error visible y
                 * limpia solo el estado que resulte inconsistente.
                 */
            }
        }

        private void ActualizarProgreso(
            ProgresoDescargaActualizacion progreso)
        {
            void Aplicar()
            {
                bytesDescargados =
                    Math.Max(
                        progreso.BytesDescargados,
                        0);

                totalBytes =
                    progreso.TotalBytes > 0
                        ? progreso.TotalBytes
                        : actualizacion?.TamanoBytes ?? 0;

                bytesPorSegundo =
                    Math.Max(
                        progreso.BytesPorSegundo,
                        0);

                tiempoRestante =
                    progreso.TiempoRestante;

                EstadoDescarga = progreso.Estado;

                ProgresoDescarga =
                    totalBytes > 0
                        ? Math.Clamp(
                            bytesDescargados /
                            (double)totalBytes,
                            0,
                            1)
                        : 0;

                Notificar(nameof(DetalleDescarga));
                Notificar(nameof(VelocidadTexto));
                Notificar(nameof(TiempoRestanteTexto));
            }

            if (MainThread.IsMainThread)
            {
                Aplicar();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(
                    Aplicar);
            }
        }

        private void ReiniciarProgreso()
        {
            bytesDescargados = 0;
            totalBytes = actualizacion?.TamanoBytes ?? 0;
            bytesPorSegundo = 0;
            tiempoRestante = null;

            EstadoDescarga =
                "Preparando descarga...";

            ProgresoDescarga = 0;

            Notificar(nameof(DetalleDescarga));
            Notificar(nameof(VelocidadTexto));
            Notificar(nameof(TiempoRestanteTexto));
        }

        private async Task<bool> EsArchivoValidoAsync(
            string? ruta,
            ActualizacionDisponible disponible,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ruta) ||
                !File.Exists(ruta))
            {
                return false;
            }

            try
            {
                var archivo = new FileInfo(ruta);

                if (disponible.TamanoBytes > 0 &&
                    archivo.Length != disponible.TamanoBytes)
                {
                    return false;
                }

                await using FileStream stream =
                    new(
                        ruta,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        bufferSize: 1024 * 1024,
                        useAsync: true);

                byte[] hash =
                    await SHA256.HashDataAsync(
                        stream,
                        cancellationToken);

                string valor =
                    Convert.ToHexString(hash);

                return string.Equals(
                    valor,
                    disponible.HashSha256,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Persiste únicamente metadatos. UrlDescarga y PermisoDescarga se
        /// limpian expresamente porque son datos efímeros de autorización.
        /// </summary>
        private void GuardarActualizacionPersistida(
            ActualizacionDisponible disponible)
        {
            var persistible =
                new ActualizacionDisponible
                {
                    ActualizacionAplicacionId =
                        disponible.ActualizacionAplicacionId,
                    Plataforma = disponible.Plataforma,
                    Canal = disponible.Canal,
                    VersionNombre = disponible.VersionNombre,
                    VersionCodigo = disponible.VersionCodigo,
                    NotasVersion = disponible.NotasVersion,
                    Obligatoria = disponible.Obligatoria,
                    VersionMinimaCodigo =
                        disponible.VersionMinimaCodigo,
                    NombreArchivo = disponible.NombreArchivo,
                    TipoContenido = disponible.TipoContenido,
                    TamanoBytes = disponible.TamanoBytes,
                    HashSha256 = disponible.HashSha256,
                    UrlDescarga = string.Empty,
                    PermisoDescarga = string.Empty,
                    FechaPublicacionUtc =
                        disponible.FechaPublicacionUtc
                };

            string json =
                JsonSerializer.Serialize(
                    persistible,
                    jsonOptions);

            Preferences.Set(
                ClaveActualizacion,
                json);
        }

        private ActualizacionDisponible?
            CargarActualizacionPersistida()
        {
            string json = Preferences.Get(
                ClaveActualizacion,
                string.Empty);

            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                ActualizacionDisponible? resultado =
                    JsonSerializer.Deserialize<
                        ActualizacionDisponible>(
                            json,
                            jsonOptions);

                if (resultado is not null)
                {
                    /*
                     * Limpieza defensiva para instalaciones que pudieron haber
                     * guardado UrlDescarga con permiso antes de esta versión.
                     */
                    resultado.UrlDescarga = string.Empty;
                    resultado.PermisoDescarga = string.Empty;
                }

                return resultado;
            }
            catch (JsonException)
            {
                Preferences.Remove(ClaveActualizacion);
                return null;
            }
        }

        private void CargarUltimaComprobacion()
        {
            string valor = Preferences.Get(
                ClaveUltimaComprobacion,
                string.Empty);

            if (DateTime.TryParse(
                    valor,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime fecha))
            {
                ultimaComprobacionLocal =
                    fecha.ToLocalTime();
            }
        }

        private void LimpiarPersistencia(
            bool eliminarArchivo)
        {
            LimpiarRutaDescargadaPersistida(
                eliminarArchivo);

            Preferences.Remove(ClaveActualizacion);

            Preferences.Set(
                ClaveDescargaSolicitada,
                false);

            actualizacion = null;
            rutaDescargada = null;
            actualizacionConfirmadaEnServidor = false;
        }

        private void LimpiarRutaDescargadaPersistida(
            bool eliminarArchivo)
        {
            string rutaGuardada = Preferences.Get(
                ClaveRutaDescargada,
                string.Empty);

            if (eliminarArchivo &&
                !string.IsNullOrWhiteSpace(rutaGuardada))
            {
                EliminarArchivoSeguro(rutaGuardada);
            }

            Preferences.Remove(ClaveRutaDescargada);

            if (string.Equals(
                    rutaDescargada,
                    rutaGuardada,
                    StringComparison.OrdinalIgnoreCase))
            {
                rutaDescargada = null;
            }
        }

        private static void EliminarArchivoSeguro(
            string ruta)
        {
            try
            {
                if (File.Exists(ruta))
                    File.Delete(ruta);
            }
            catch
            {
                // El sistema podrá limpiar el archivo posteriormente.
            }
        }

        private static string ObtenerPlataformaVisibleActual()
        {
#if ANDROID
            return "Android";
#elif WINDOWS
            return "Windows";
#else
            return "No compatible";
#endif
        }

        private static string FormatearTamano(
            long bytes)
        {
            string[] unidades =
                { "B", "KB", "MB", "GB" };

            double valor = Math.Max(bytes, 0);
            int indice = 0;

            while (valor >= 1024 &&
                   indice < unidades.Length - 1)
            {
                valor /= 1024;
                indice++;
            }

            return indice == 0
                ? $"{valor:0} {unidades[indice]}"
                : $"{valor:0.##} {unidades[indice]}";
        }

        private static string FormatearTiempoRestante(
            TimeSpan? tiempo)
        {
            if (!tiempo.HasValue)
                return "Calculando tiempo restante...";

            TimeSpan valor = tiempo.Value;

            if (valor.TotalSeconds < 2)
                return "Menos de 2 segundos restantes";

            if (valor.TotalMinutes < 1)
            {
                return
                    $"{Math.Ceiling(valor.TotalSeconds):0} " +
                    "segundos restantes";
            }

            if (valor.TotalHours < 1)
            {
                return
                    $"{Math.Ceiling(valor.TotalMinutes):0} " +
                    "minutos restantes";
            }

            return
                $"{Math.Ceiling(valor.TotalHours):0} " +
                "horas restantes";
        }

        private void NotificarTodo()
        {
            foreach (string propiedad in new[]
            {
                nameof(Actualizacion),
                nameof(VersionInstalada),
                nameof(VersionNueva),
                nameof(NuevaVersionTitulo),
                nameof(ResumenVersion),
                nameof(PlataformaVisible),
                nameof(TamanoVisible),
                nameof(NotasVersion),
                nameof(EsObligatoria),
                nameof(TieneActualizacion),
                nameof(NoTieneActualizacion),
                nameof(TieneArchivoDescargado),
                nameof(Ocupada),
                nameof(BloqueoInterfaz),
                nameof(PuedeBuscar),
                nameof(PuedeCerrar),
                nameof(MostrandoProgreso),
                nameof(PuedeCancelarDescarga),
                nameof(PuedeEjecutarPrincipal),
                nameof(DebeComprobarAlAbrir),
                nameof(DescripcionPersistenciaDescarga),
                nameof(TextoBotonPrincipal),
                nameof(TituloEstadoGeneral),
                nameof(DescripcionEstadoGeneral),
                nameof(UltimaComprobacionTexto)
            })
            {
                Notificar(propiedad);
            }
        }

        private void Notificar(
            [CallerMemberName]
            string? nombre = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nombre));
        }
    }
}
