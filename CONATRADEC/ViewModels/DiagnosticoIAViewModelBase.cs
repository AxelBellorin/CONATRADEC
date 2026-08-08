using CONATRADEC.Services;
using Microsoft.Maui.Networking;

namespace CONATRADEC.ViewModels
{
    public abstract class DiagnosticoIAViewModelBase : GlobalService
    {
        // Se conserva el cliente anterior para no romper páginas todavía no
        // migradas y se agrega el flujo por fotografía.
        protected readonly DiagnosticoIAApiService Api =
            DiagnosticoIAApiService.Instance;

        protected readonly InspeccionFitosanitariaApiService InspeccionApi =
            InspeccionFitosanitariaApiService.Instance;

        private string mensajeEstado = string.Empty;

        protected DiagnosticoIAViewModelBase()
        {
            // Esta página se abrió desde el panel de inspección mediante una
            // ruta apilada. Regresar con ".." evita intentar abrir nuevamente
            // la misma ruta y conserva correctamente la pila de Shell.
            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar),
                () => !IsBusy);
        }

        public Command RegresarCommand { get; }

        public string MensajeEstado
        {
            get => mensajeEstado;
            protected set
            {
                if (mensajeEstado == value)
                    return;

                mensajeEstado = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensajeEstado));
            }
        }

        public bool TieneMensajeEstado =>
            !string.IsNullOrWhiteSpace(MensajeEstado);

        protected bool ValidarEnLinea(bool mostrarMensaje = true)
        {
            NetworkAccess accesoRed = Connectivity.Current.NetworkAccess;

#if WINDOWS
            bool redDisponible = accesoRed != NetworkAccess.None;
#else
            bool redDisponible = accesoRed == NetworkAccess.Internet;
#endif

            bool enLinea = redDisponible && ModoSesionService.EsEnLinea;
            if (enLinea)
                return true;

            /*
             * El técnico sí puede registrar una nueva inspección durante una
             * sesión sin conexión. La operación se guarda en el dispositivo y
             * se sincroniza posteriormente; IA, analizador y aprobador siguen
             * requiriendo el servidor para conservar concurrencia y auditoría.
             */
            bool esCapturaTecnico = EsPantallaCapturaTecnico();
            if (ModoSesionService.EsOffline &&
                esCapturaTecnico &&
                FitosanitariaOfflineService.Instance
                    .EstaPreparadoUsuarioActual)
            {
                return true;
            }

            if (!enLinea && mostrarMensaje)
            {
                string mensaje =
                    ModoSesionService.EsOffline && esCapturaTecnico
                        ? "La captura fitosanitaria sin conexión todavía no está preparada en este dispositivo. Inicie una sesión en línea, abra Datos sin conexión y utilice Descargar todo."
                        : "El análisis IA, la revisión del analizador, la aprobación y la publicación en el Álbum Botánico requieren una sesión en línea. La captura de campo del técnico sí puede realizarse sin conexión después de preparar el dispositivo.";

                _ = MostrarAlertaAsync(
                    "Conexión requerida",
                    mensaje);
            }

            return false;
        }

        private static bool EsPantallaCapturaTecnico()
        {
            string ruta = Shell.Current?
                .CurrentState?
                .Location?
                .OriginalString ?? string.Empty;

            return ruta.Contains(
                "DiagnosticoIASolicitudPage",
                StringComparison.OrdinalIgnoreCase);
        }

        protected static List<string> SepararLista(string? texto) =>
            (texto ?? string.Empty)
                .Split(
                    ['\r', '\n', ',', ';'],
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        protected static string UnirLista(IEnumerable<string>? valores) =>
            string.Join(Environment.NewLine, valores ?? []);

        protected static bool EsSesionInvalidada(Exception ex) =>
            ex is DiagnosticoIAApiException
            {
                EsSesionInvalidada: true
            } ||
            ex is InspeccionFitosanitariaApiException
            {
                EsSesionInvalidada: true
            };

        protected async Task MostrarErrorAsync(Exception ex)
        {
            if (EsSesionInvalidada(ex))
                return;

            await MostrarAlertaAsync(
                "Inspección fitosanitaria",
                ex.Message);
        }

        protected static Task MostrarAlertaAsync(
            string titulo,
            string mensaje)
        {
            if (Shell.Current == null)
                return Task.CompletedTask;

            return Shell.Current.DisplayAlert(
                titulo,
                mensaje,
                "Aceptar");
        }

        protected static Task<bool> ConfirmarAsync(
            string titulo,
            string mensaje)
        {
            if (Shell.Current == null)
                return Task.FromResult(false);

            return Shell.Current.DisplayAlert(
                titulo,
                mensaje,
                "Continuar",
                "Cancelar");
        }
    }
}
