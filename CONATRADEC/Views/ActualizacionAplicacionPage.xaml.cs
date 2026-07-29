using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using System.Runtime.CompilerServices;

namespace CONATRADEC.Views
{
    public partial class ActualizacionAplicacionPage :
        ContentPage,
        IQueryAttributable
    {
        private ActualizacionDisponible? actualizacion;
        private CancellationTokenSource? operacionCts;
        private string? rutaDescargada;

        private bool ocupada;
        private bool descargando;
        private bool paginaVisible;
        private bool esperandoPermisoInstalacion;

        private double progresoDescarga;
        private long bytesDescargados;
        private long totalBytes;
        private double bytesPorSegundo;
        private TimeSpan? tiempoRestante;

        private string estadoDescarga =
            "Preparando descarga...";

        private string mensajeEstado =
            string.Empty;

        public ActualizacionAplicacionPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        public string VersionInstalada =>
            $"{AppInfo.Current.VersionString} " +
            $"({AppInfo.Current.BuildString})";

        public string VersionNueva =>
            actualizacion is null
                ? "Consultando..."
                : $"{actualizacion.VersionNombre} " +
                  $"({actualizacion.VersionCodigo})";

        public string NuevaVersionTitulo =>
            actualizacion is null
                ? "Buscando actualización"
                : $"ConatraCafé Soil " +
                  $"{actualizacion.VersionNombre}";

        public string ResumenVersion =>
            actualizacion is null
                ? "Conectando con el servidor..."
                : $"Canal " +
                  $"{actualizacion.Canal.ToLowerInvariant()} · " +
                  $"compilación {actualizacion.VersionCodigo}";

        public string PlataformaVisible =>
            actualizacion is null
                ? "—"
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
                ? "Esta versión incluye mejoras generales " +
                  "y correcciones de estabilidad."
                : actualizacion.NotasVersion;

        public bool EsObligatoria =>
            actualizacion?.Obligatoria == true;

        /*
         * Una descarga opcional puede continuar aunque el usuario cierre esta
         * página. Una actualización obligatoria mantiene el bloqueo.
         */
        public bool PuedeCerrar =>
            !EsObligatoria;

        public bool Ocupada
        {
            get => ocupada;
            private set
            {
                if (ocupada == value)
                    return;

                ocupada = value;
                Notificar();
                Notificar(nameof(PuedeEjecutar));
                Notificar(nameof(PuedeCerrar));
                Notificar(nameof(TextoBotonPrincipal));
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
                Notificar();
                Notificar(nameof(MostrandoProgreso));
                Notificar(nameof(PuedeCancelarDescarga));
                Notificar(nameof(TextoBotonPrincipal));
            }
        }

        public bool PuedeEjecutar =>
            actualizacion is not null &&
            !Ocupada;

        public bool MostrandoProgreso =>
            Descargando;

        public bool PuedeCancelarDescarga =>
            Descargando &&
            operacionCts is not null;

        public double ProgresoDescarga
        {
            get => progresoDescarga;
            private set
            {
                double valor =
                    Math.Clamp(
                        value,
                        0,
                        1);

                if (Math.Abs(
                        progresoDescarga -
                        valor) < 0.0001)
                {
                    return;
                }

                progresoDescarga =
                    valor;

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
            FormatearTiempoRestante(
                tiempoRestante);

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
            !string.IsNullOrWhiteSpace(
                MensajeEstado);

        public string TextoBotonPrincipal =>
            esperandoPermisoInstalacion
                ? "Continuar instalación"
                : rutaDescargada is not null
                    ? "Instalar actualización"
                    : Descargando
                        ? "Descargando en segundo plano..."
                        : "Descargar e instalar";

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            if (query.TryGetValue(
                    "Actualizacion",
                    out object? valor) &&
                valor is ActualizacionDisponible disponible)
            {
                EstablecerActualizacion(
                    disponible);
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            paginaVisible = true;

            if (actualizacion is null &&
                !Ocupada)
            {
                await ConsultarAsync();
                return;
            }

            if (rutaDescargada is not null &&
                !Descargando &&
                !esperandoPermisoInstalacion)
            {
                MensajeEstado =
                    "La actualización ya fue descargada y verificada. " +
                    "Presione Instalar actualización.";
            }
        }

        protected override void OnDisappearing()
        {
            /*
             * No se cancela la operación. DownloadManager y
             * BackgroundDownloader deben continuar trabajando.
             */
            paginaVisible = false;

            base.OnDisappearing();
        }

        protected override bool OnBackButtonPressed()
        {
            if (EsObligatoria)
                return true;

            return base.OnBackButtonPressed();
        }

        private async Task ConsultarAsync()
        {
            Ocupada = true;

            MensajeEstado =
                "Consultando la versión más reciente...";

            try
            {
                actualizacion =
                    await ActualizacionAplicacionService
                        .Instance
                        .ComprobarActualizacionAsync();

                if (actualizacion is null)
                {
                    MensajeEstado =
                        "La aplicación ya está actualizada.";

                    await GlobalService.MostrarInformacionAsync(
                        "ConatraCafé Soil ya tiene la versión más reciente.");

                    await Shell.Current.GoToAsync(
                        "..");

                    return;
                }

                MensajeEstado =
                    string.Empty;

                NotificarTodo();
            }
            catch (Exception ex)
            {
                MensajeEstado =
                    "No fue posible consultar las actualizaciones.";

                await GlobalService.MostrarErrorAsync(
                    "No fue posible consultar las actualizaciones. " +
                    ex.Message);
            }
            finally
            {
                Ocupada = false;
            }
        }

        private async void DescargarEInstalar_Clicked(
            object sender,
            EventArgs e)
        {
            await DescargarEInstalarAsync();
        }

        private async Task DescargarEInstalarAsync()
        {
            if (actualizacion is null ||
                Ocupada)
            {
                return;
            }

            /*
             * Cuando el archivo ya está descargado, el mismo botón pasa
             * directamente a la instalación.
             */
            if (rutaDescargada is not null)
            {
                await AbrirInstaladorAsync();
                return;
            }

            Ocupada = true;
            Descargando = true;

            operacionCts?.Dispose();
            operacionCts =
                new CancellationTokenSource();

            ReiniciarProgreso();

            MensajeEstado =
                "La descarga continuará aunque minimice la aplicación.";

            /*
             * Permite que MAUI dibuje la barra antes de iniciar la operación.
             */
            await Task.Yield();

            var progreso =
                new Progress<ProgresoDescargaActualizacion>(
                    ActualizarProgreso);

            try
            {
                rutaDescargada =
                    await ActualizacionAplicacionService
                        .Instance
                        .DescargarEnSegundoPlanoAsync(
                            actualizacion,
                            progreso,
                            operacionCts.Token);

                Descargando = false;

                MensajeEstado =
                    "Descarga completada y validada correctamente.";

                Notificar(
                    nameof(TextoBotonPrincipal));

                /*
                 * El instalador solo se abre si la página sigue visible. Android
                 * y Windows no deben intentar mostrar una ventana de instalación
                 * mientras la app está en segundo plano.
                 */
                if (paginaVisible)
                {
                    await AbrirInstaladorAsync();
                }
                else
                {
                    MensajeEstado =
                        "La descarga terminó. Abra nuevamente esta pantalla " +
                        "para instalar la actualización.";
                }
            }
            catch (OperationCanceledException)
            {
                rutaDescargada = null;
                Descargando = false;
                ReiniciarProgreso();

                MensajeEstado =
                    "La descarga fue cancelada.";
            }
            catch (Exception ex)
            {
                rutaDescargada = null;
                Descargando = false;
                esperandoPermisoInstalacion = false;
                ReiniciarProgreso();

                MensajeEstado =
                    "No fue posible completar la actualización.";

                Notificar(
                    nameof(TextoBotonPrincipal));

                await GlobalService.MostrarErrorAsync(
                    "No fue posible descargar la actualización. " +
                    ex.Message);
            }
            finally
            {
                Ocupada = false;

                operacionCts?.Dispose();
                operacionCts = null;

                Notificar(
                    nameof(PuedeCancelarDescarga));
            }
        }

        private void ActualizarProgreso(
            ProgresoDescargaActualizacion progreso)
        {
            MainThread.BeginInvokeOnMainThread(
                () =>
                {
                    bytesDescargados =
                        Math.Max(
                            progreso.BytesDescargados,
                            0);

                    totalBytes =
                        progreso.TotalBytes > 0
                            ? progreso.TotalBytes
                            : actualizacion?.TamanoBytes ??
                              0;

                    bytesPorSegundo =
                        Math.Max(
                            progreso.BytesPorSegundo,
                            0);

                    tiempoRestante =
                        progreso.TiempoRestante;

                    EstadoDescarga =
                        progreso.Estado;

                    ProgresoDescarga =
                        totalBytes > 0
                            ? Math.Clamp(
                                bytesDescargados /
                                (double)totalBytes,
                                0,
                                1)
                            : 0;

                    Notificar(
                        nameof(DetalleDescarga));

                    Notificar(
                        nameof(VelocidadTexto));

                    Notificar(
                        nameof(TiempoRestanteTexto));
                });
        }

        private async Task AbrirInstaladorAsync()
        {
            if (string.IsNullOrWhiteSpace(
                    rutaDescargada))
            {
                return;
            }

            Ocupada = true;

            try
            {
                MensajeEstado =
                    "Abriendo el instalador del sistema...";

                ResultadoInstalacionActualizacion resultado =
                    await ActualizacionInstaladorService
                        .IniciarInstalacionAsync(
                            rutaDescargada);

                esperandoPermisoInstalacion =
                    resultado.RequierePermiso;

                MensajeEstado =
                    resultado.Mensaje;

                Notificar(
                    nameof(TextoBotonPrincipal));

                if (resultado.RequierePermiso)
                {
                    await GlobalService.MostrarAdvertenciaAsync(
                        resultado.Mensaje);
                }
                else if (!resultado.Iniciado)
                {
                    await GlobalService.MostrarErrorAsync(
                        resultado.Mensaje);
                }
            }
            catch (Exception ex)
            {
                esperandoPermisoInstalacion = false;

                MensajeEstado =
                    "El archivo fue descargado, pero no se pudo abrir el instalador.";

                await GlobalService.MostrarErrorAsync(
                    "No fue posible abrir el instalador. " +
                    ex.Message);
            }
            finally
            {
                Ocupada = false;
            }
        }

        private void CancelarDescarga_Clicked(
            object sender,
            EventArgs e)
        {
            if (!Descargando)
                return;

            EstadoDescarga =
                "Cancelando descarga...";

            operacionCts?.Cancel();
        }

        private async void Volver_Clicked(
            object sender,
            EventArgs e)
        {
            if (!PuedeCerrar)
                return;

            await Shell.Current.GoToAsync(
                "..");
        }

        private void EstablecerActualizacion(
            ActualizacionDisponible disponible)
        {
            actualizacion = disponible;
            rutaDescargada = null;
            esperandoPermisoInstalacion = false;

            ReiniciarProgreso();

            MensajeEstado =
                string.Empty;

            NotificarTodo();
        }

        private void ReiniciarProgreso()
        {
            bytesDescargados = 0;

            totalBytes =
                actualizacion?.TamanoBytes ??
                0;

            bytesPorSegundo = 0;
            tiempoRestante = null;

            EstadoDescarga =
                "Preparando descarga...";

            ProgresoDescarga = 0;

            Notificar(
                nameof(DetalleDescarga));

            Notificar(
                nameof(VelocidadTexto));

            Notificar(
                nameof(TiempoRestanteTexto));
        }

        private void NotificarTodo()
        {
            foreach (
                string propiedad
                in new[]
                {
                    nameof(VersionInstalada),
                    nameof(VersionNueva),
                    nameof(NuevaVersionTitulo),
                    nameof(ResumenVersion),
                    nameof(PlataformaVisible),
                    nameof(TamanoVisible),
                    nameof(NotasVersion),
                    nameof(EsObligatoria),
                    nameof(PuedeCerrar),
                    nameof(PuedeEjecutar),
                    nameof(TextoBotonPrincipal),
                    nameof(MostrandoProgreso),
                    nameof(PuedeCancelarDescarga)
                })
            {
                Notificar(
                    propiedad);
            }
        }

        private static string FormatearTamano(
            long bytes)
        {
            string[] unidades =
                { "B", "KB", "MB", "GB" };

            double valor =
                Math.Max(
                    bytes,
                    0);

            int indice = 0;

            while (valor >= 1024 &&
                   indice <
                       unidades.Length - 1)
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

            TimeSpan valor =
                tiempo.Value;

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

        private void Notificar(
            [CallerMemberName]
            string? nombre = null)
        {
            OnPropertyChanged(
                nombre);
        }
    }
}
