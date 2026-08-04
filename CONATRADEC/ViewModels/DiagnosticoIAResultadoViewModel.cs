using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Presenta el resultado completo de una sola inspección. Mantiene las
    /// decisiones del técnico separadas de la clasificación oficial que
    /// corresponde al analizador y al aprobador.
    /// </summary>
    public sealed class DiagnosticoIAResultadoViewModel :
        DiagnosticoIAViewModelBase
    {
        private DiagnosticoIADetalle? detalle;
        private int diagnosticoId;
        private string origen = DiagnosticoIARoutes.ModoMisInspecciones;
        private CancellationTokenSource? seguimientoCts;

        public DiagnosticoIAResultadoViewModel()
        {
            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => !IsBusy && diagnosticoId > 0);

            ReintentarCommand = new Command(
                async () => await ReintentarAsync(),
                () => !IsBusy && PuedeReintentar);

            EnviarAnalizadorCommand = new Command(
                async () => await EnviarAnalizadorAsync(),
                () => !IsBusy && PuedeEnviarAnalizador);

            SolicitarNuevaEvaluacionCommand = new Command(
                async () => await SolicitarNuevaEvaluacionAsync(),
                () => !IsBusy && PuedeSolicitarNuevaEvaluacion);

            NoContinuarCommand = new Command(
                async () => await NoContinuarAsync(),
                () => !IsBusy && PuedeNoContinuar);

            AnularCommand = new Command(
                async () => await AnularAsync(),
                () => !IsBusy && PuedeAnular);

            RegresarResultadoCommand = new Command(
                async () => await RegresarResultadoAsync(),
                () => !IsBusy);
        }

        public Command ActualizarCommand { get; }
        public Command ReintentarCommand { get; }
        public Command EnviarAnalizadorCommand { get; }
        public Command SolicitarNuevaEvaluacionCommand { get; }
        public Command NoContinuarCommand { get; }
        public Command AnularCommand { get; }
        public Command RegresarResultadoCommand { get; }

        public DiagnosticoIADetalle? Detalle
        {
            get => detalle;
            private set
            {
                if (ReferenceEquals(detalle, value))
                    return;

                detalle = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneDetalle));
                OnPropertyChanged(nameof(PuedeReintentar));
                OnPropertyChanged(nameof(PuedeDecidir));
                OnPropertyChanged(nameof(PuedeEnviarAnalizador));
                OnPropertyChanged(nameof(PuedeSolicitarNuevaEvaluacion));
                OnPropertyChanged(nameof(PuedeNoContinuar));
                OnPropertyChanged(nameof(PuedeAnular));
                OnPropertyChanged(nameof(MostrarSeguimiento));
                OnPropertyChanged(nameof(TituloResultado));
                ActualizarComandos();
            }
        }

        public bool TieneDetalle => Detalle != null;

        public string TituloResultado => Detalle == null
            ? "Resultado de la inspección"
            : $"Resultado de la inspección #{Detalle.DiagnosticoIAId}";

        public string TextoRegresar => origen switch
        {
            DiagnosticoIARoutes.ModoDecisionesPendientes =>
                "Decisiones pendientes",
            DiagnosticoIARoutes.ModoHistorial => "Historial",
            _ => "Mis inspecciones"
        };

        public bool PuedeReintentar =>
            Detalle?.Estado == DiagnosticoIAEstados.ErrorAnalisis;

        public bool PuedeDecidir =>
            Detalle?.EsPropietarioSolicitud == true &&
            Detalle.Estado ==
                DiagnosticoIAEstados.PendienteDecisionTecnico;

        public bool PuedeEnviarAnalizador =>
            PuedeDecidir &&
            Detalle?.Imagenes.Count > 0 &&
            Detalle.Imagenes.All(item => item.TieneResultadoIA);

        public bool PuedeSolicitarNuevaEvaluacion =>
            PuedeDecidir &&
            (Detalle?.RevisionesGeminiIlimitadas == true ||
             (Detalle != null &&
              Detalle.RevisionesGeminiCompletadas <
                  Detalle.MaximoRevisionesGemini));

        public bool PuedeNoContinuar => PuedeDecidir;

        public bool PuedeAnular =>
            CanDelete &&
            Detalle != null &&
            Detalle.Estado is
                DiagnosticoIAEstados.Rechazado or
                DiagnosticoIAEstados.NoConcluyente or
                DiagnosticoIAEstados.ErrorAnalisis;

        public bool MostrarSeguimiento =>
            Detalle?.Estado == DiagnosticoIAEstados.AnalizandoIA;

        public void AplicarParametros(
            int id,
            string? origenVista)
        {
            diagnosticoId = id;
            origen = DiagnosticoIARoutes.NormalizarModo(origenVista);
            OnPropertyChanged(nameof(TextoRegresar));
        }

        public async Task InicializarAsync()
        {
            ActualizarPermisos();
            await ActualizarAsync();
            IniciarSeguimiento();
        }

        public void IniciarSeguimiento()
        {
            DetenerSeguimiento();

            if (!MostrarSeguimiento || diagnosticoId <= 0)
                return;

            seguimientoCts = new CancellationTokenSource();
            _ = SeguirProcesamientoAsync(seguimientoCts.Token);
        }

        public void DetenerSeguimiento()
        {
            CancellationTokenSource? anterior =
                Interlocked.Exchange(ref seguimientoCts, null);

            if (anterior == null)
                return;

            try
            {
                anterior.Cancel();
            }
            catch
            {
            }
            finally
            {
                anterior.Dispose();
            }
        }

        private void ActualizarPermisos()
        {
            var permiso = PermissionService.Instance.Get(
                DiagnosticoIARoutes.InterfazSolicitud);

            CanView = permiso.leer;
            CanAdd = permiso.agregar;
            CanEdit = permiso.actualizar;
            CanDelete = permiso.eliminar;
            OnPropertyChanged(nameof(PuedeAnular));
        }

        private async Task ActualizarAsync()
        {
            if (IsBusy || diagnosticoId <= 0 || !ValidarEnLinea(false))
                return;

            IsBusy = true;
            MensajeEstado = "Cargando resultado de la inspección...";
            ActualizarComandos();

            try
            {
                Detalle = await Api.ObtenerDetalleAsync(diagnosticoId);
                MensajeEstado = string.Empty;
            }
            catch (Exception ex)
            {
                MensajeEstado = string.Empty;
                await MostrarErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task SeguirProcesamientoAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(4),
                        cancellationToken);

                    if (cancellationToken.IsCancellationRequested ||
                        Detalle?.Estado !=
                            DiagnosticoIAEstados.AnalizandoIA)
                    {
                        break;
                    }

                    DiagnosticoIADetalle actualizado =
                        await Api.ObtenerDetalleAsync(
                            diagnosticoId,
                            cancellationToken);

                    await MainThread.InvokeOnMainThreadAsync(
                        () => Detalle = actualizado);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Seguimiento de diagnóstico IA: {ex}");
            }
        }

        private async Task ReintentarAsync()
        {
            if (!PuedeReintentar || IsBusy)
                return;

            bool confirmar = await ConfirmarAsync(
                "Reintentar análisis",
                "Se volverán a procesar únicamente las fotografías pendientes o con error. Las imágenes guardadas no se duplicarán.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Reintentando análisis...";
            ActualizarComandos();

            try
            {
                var progreso = new Progress<DiagnosticoIAProcesamientoEstado>(
                    ActualizarProgreso);

                Detalle = await Api.ReintentarIAAsync(
                    diagnosticoId,
                    progreso);
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
                await RecargarSeguroAsync();
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
                IniciarSeguimiento();
            }
        }

        private async Task EnviarAnalizadorAsync()
        {
            if (!PuedeEnviarAnalizador || IsBusy)
                return;

            bool confirmar = await ConfirmarAsync(
                "Enviar al analizador",
                "El analizador humano será responsable de confirmar la clasificación oficial de cada fotografía contra el Álbum Botánico.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Enviando al analizador...";
            ActualizarComandos();

            try
            {
                await Api.EnviarAlAnalizadorAsync(diagnosticoId);
                Detalle = await Api.ObtenerDetalleAsync(diagnosticoId);

                await MostrarAlertaAsync(
                    "Enviado",
                    "La inspección quedó disponible en la bandeja del analizador.");
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task SolicitarNuevaEvaluacionAsync()
        {
            if (!PuedeSolicitarNuevaEvaluacion || IsBusy ||
                Shell.Current == null)
            {
                return;
            }

            string? motivo = await Shell.Current.DisplayPromptAsync(
                "Solicitar otra evaluación",
                "Explique qué parte del resultado debería revisar Gemini.",
                "Solicitar",
                "Cancelar",
                "Observación obligatoria",
                1000,
                Keyboard.Default);

            if (motivo == null)
                return;

            motivo = motivo.Trim();
            if (motivo.Length < 8)
            {
                await MostrarAlertaAsync(
                    "Observación requerida",
                    "Escriba una observación de al menos 8 caracteres.");
                return;
            }

            string? propuesta = await Shell.Current.DisplayPromptAsync(
                "Diagnóstico considerado",
                "Puede indicar opcionalmente el diagnóstico que considera más probable.",
                "Continuar",
                "Omitir",
                "Diagnóstico opcional",
                300,
                Keyboard.Default);

            IsBusy = true;
            MensajeEstado = "Gemini está realizando otra evaluación...";
            ActualizarComandos();

            try
            {
                var progreso = new Progress<DiagnosticoIAProcesamientoEstado>(
                    ActualizarProgreso);

                Detalle = await Api.SolicitarNuevaEvaluacionTecnicoAsync(
                    diagnosticoId,
                    motivo,
                    propuesta,
                    progreso);
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
                await RecargarSeguroAsync();
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task NoContinuarAsync()
        {
            if (!PuedeNoContinuar || IsBusy || Shell.Current == null)
                return;

            string? motivo = await Shell.Current.DisplayPromptAsync(
                "No continuar con la solicitud",
                "Explique por qué no desea enviarla al analizador. La evidencia se conservará para auditoría.",
                "Cerrar solicitud",
                "Cancelar",
                "Motivo obligatorio",
                1000,
                Keyboard.Default);

            if (motivo == null)
                return;

            motivo = motivo.Trim();
            if (motivo.Length < 8)
            {
                await MostrarAlertaAsync(
                    "Motivo requerido",
                    "Escriba un motivo de al menos 8 caracteres.");
                return;
            }

            bool confirmar = await ConfirmarAsync(
                "Confirmar cierre",
                "La solicitud no pasará al analizador y conservará toda su evidencia e historial.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Cerrando solicitud...";
            ActualizarComandos();

            try
            {
                await Api.NoContinuarAsync(diagnosticoId, motivo);
                Detalle = await Api.ObtenerDetalleAsync(diagnosticoId);
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task AnularAsync()
        {
            if (!PuedeAnular || IsBusy || Shell.Current == null)
                return;

            string? motivo = await Shell.Current.DisplayPromptAsync(
                "Anular diagnóstico",
                "Explique por qué debe ocultarse. La evidencia y el historial no se eliminarán.",
                "Anular",
                "Cancelar",
                "Motivo obligatorio",
                1000,
                Keyboard.Default);

            if (motivo == null || motivo.Trim().Length < 8)
            {
                if (motivo != null)
                {
                    await MostrarAlertaAsync(
                        "Motivo requerido",
                        "Escriba un motivo de al menos 8 caracteres.");
                }
                return;
            }

            bool confirmar = await ConfirmarAsync(
                "Confirmar anulación",
                "El registro se ocultará de los listados normales, pero conservará toda su trazabilidad.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Anulando diagnóstico...";
            ActualizarComandos();

            try
            {
                Detalle = await Api.AnularAsync(
                    diagnosticoId,
                    motivo.Trim());
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task RegresarResultadoAsync()
        {
            DetenerSeguimiento();

            if (Shell.Current == null)
                return;

            try
            {
                // El resultado siempre se abre desde una lista o desde la
                // creación de la inspección. Regresar sobre la pila evita
                // duplicar páginas cada vez que se consulta un resultado.
                await Shell.Current.GoToAsync("..", false);
            }
            catch
            {
                string ruta =
                    DiagnosticoIARoutes.CrearRutaSolicitud(origen);
                await Shell.Current.GoToAsync(ruta, false);
            }
        }

        private async Task RecargarSeguroAsync()
        {
            try
            {
                Detalle = await Api.ObtenerDetalleAsync(diagnosticoId);
            }
            catch
            {
            }
        }

        private void ActualizarProgreso(
            DiagnosticoIAProcesamientoEstado estado)
        {
            if (estado == null)
                return;

            string mensaje = string.IsNullOrWhiteSpace(estado.Mensaje)
                ? "Procesando fotografías..."
                : estado.Mensaje.Trim();

            MensajeEstado = estado.TotalFotografias > 0
                ? $"{mensaje} {estado.FotografiasProcesadas} de {estado.TotalFotografias} ({estado.Porcentaje}%)."
                : mensaje;
        }

        private void ActualizarComandos()
        {
            ActualizarCommand.ChangeCanExecute();
            ReintentarCommand.ChangeCanExecute();
            EnviarAnalizadorCommand.ChangeCanExecute();
            SolicitarNuevaEvaluacionCommand.ChangeCanExecute();
            NoContinuarCommand.ChangeCanExecute();
            AnularCommand.ChangeCanExecute();
            RegresarResultadoCommand.ChangeCanExecute();
        }
    }
}
