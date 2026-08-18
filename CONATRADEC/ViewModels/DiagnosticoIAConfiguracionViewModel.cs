using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Administra únicamente el límite de revisiones adicionales de IA.
    /// El proveedor, la URL, los modelos y la API key se configuran en el
    /// backend para evitar dependencias innecesarias en MAUI.
    /// </summary>
    public sealed class DiagnosticoIAConfiguracionViewModel : GlobalService
    {
        private readonly DiagnosticoIAConfiguracionApiService api = new();

        private bool inicializado;
        private bool revisionesIlimitadas;
        private string maximoRevisionesTexto = "2";
        private string resumenActual = string.Empty;
        private string ultimaModificacion = string.Empty;
        private string mensajeEstado = string.Empty;
        private string rowVersion = string.Empty;
        private int maximoPersistido = 2;

        public DiagnosticoIAConfiguracionViewModel()
        {
            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar),
                () => !IsBusy);

            ActualizarCommand = new Command(
                async () => await CargarAsync(),
                () => !IsBusy && CanView);

            GuardarCommand = new Command(
                async () => await GuardarAsync(),
                () => !IsBusy && CanEdit);

            AdministrarTiposFotografiaCommand = new Command(
                async () => await GoToAsyncParameters(
                    TipoFotografiaIARoutes.Pagina),
                () => !IsBusy && CanView);
        }

        public ObservableCollection<DiagnosticoIAConfiguracionHistorialItem>
            Historial { get; } = [];

        public Command RegresarCommand { get; }
        public Command ActualizarCommand { get; }
        public Command GuardarCommand { get; }
        public Command AdministrarTiposFotografiaCommand { get; }

        public bool RevisionesIlimitadas
        {
            get => revisionesIlimitadas;
            set
            {
                if (revisionesIlimitadas == value)
                    return;

                revisionesIlimitadas = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MostrarCampoMaximo));
                OnPropertyChanged(nameof(AyudaLimite));
            }
        }

        public bool MostrarCampoMaximo => !RevisionesIlimitadas;

        public string MaximoRevisionesTexto
        {
            get => maximoRevisionesTexto;
            set
            {
                string nuevoValor = value ?? string.Empty;

                if (maximoRevisionesTexto == nuevoValor)
                    return;

                maximoRevisionesTexto = nuevoValor;
                OnPropertyChanged();
            }
        }

        public string ResumenActual
        {
            get => resumenActual;
            private set
            {
                if (resumenActual == value)
                    return;

                resumenActual = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string UltimaModificacion
        {
            get => ultimaModificacion;
            private set
            {
                if (ultimaModificacion == value)
                    return;

                ultimaModificacion = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string MensajeEstado
        {
            get => mensajeEstado;
            private set
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

        public bool SoloLectura => !CanEdit;
        public bool PuedeAdministrarTiposFotografia => CanView;
        public bool TieneHistorial => Historial.Count > 0;
        public bool SinHistorial => !TieneHistorial;
        public bool SinPermisoLectura => !CanView;

        public string AyudaLimite => RevisionesIlimitadas
            ? "Cada diagnóstico podrá solicitar revisiones adicionales mientras continúe en una etapa que las permita. Los errores técnicos no cuentan como revisión completada."
            : "El análisis inicial no cuenta. Solo se contabilizan las revisiones adicionales completadas correctamente.";

        public async Task InicializarAsync()
        {
            ActualizarPermisos();

            if (!CanView)
            {
                MensajeEstado = string.Empty;
                return;
            }

            inicializado = true;
            await CargarAsync();
        }

        private void ActualizarPermisos()
        {
            var permiso = PermissionService.Instance.Get(
                DiagnosticoIARoutes.InterfazConfiguracion);

            CanView = permiso?.leer == true;
            CanEdit = permiso?.actualizar == true;
            CanAdd = permiso?.agregar == true;
            CanDelete = permiso?.eliminar == true;

            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(SoloLectura));
            OnPropertyChanged(nameof(PuedeAdministrarTiposFotografia));
            OnPropertyChanged(nameof(SinPermisoLectura));
            ActualizarComandos();
        }

        private async Task CargarAsync()
        {
            if (IsBusy || !CanView)
                return;

            IsBusy = true;
            MensajeEstado = "Cargando configuración...";
            ActualizarComandos();

            try
            {
                DiagnosticoIAConfiguracion configuracion =
                    await api.ObtenerAsync();

                AplicarConfiguracion(configuracion);
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

        private async Task GuardarAsync()
        {
            if (IsBusy || !CanEdit)
                return;

            int maximo;

            if (RevisionesIlimitadas)
            {
                maximo = int.TryParse(MaximoRevisionesTexto, out int valorActual) &&
                         valorActual is >= 1 and <= 20
                    ? valorActual
                    : Math.Clamp(maximoPersistido, 1, 20);
            }
            else if (!int.TryParse(MaximoRevisionesTexto, out maximo) ||
                     maximo is < 1 or > 20)
            {
                await MostrarAlertaAsync(
                    "Configuración de IA",
                    "Indique un valor entre 1 y 20 revisiones.");
                return;
            }

            if (string.IsNullOrWhiteSpace(rowVersion))
            {
                await MostrarAlertaAsync(
                    "Configuración de IA",
                    "La configuración todavía no tiene una versión válida. Pulse Actualizar e intente nuevamente.");
                return;
            }

            bool confirmar = await ConfirmarAsync(
                "Guardar configuración",
                RevisionesIlimitadas
                    ? "Los diagnósticos podrán solicitar revisiones adicionales sin límite numérico."
                    : $"Cada diagnóstico podrá solicitar hasta {maximo} revisiones adicionales.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Guardando configuración...";
            ActualizarComandos();

            bool recargarPorConflicto = false;

            try
            {
                DiagnosticoIAConfiguracion configuracion =
                    await api.ActualizarAsync(
                        new DiagnosticoIAConfiguracionActualizarRequest
                        {
                            MaximoRevisionesGemini = maximo,
                            RevisionesIlimitadas = RevisionesIlimitadas,
                            RowVersion = rowVersion
                        });

                AplicarConfiguracion(configuracion);

                await MostrarAlertaAsync(
                    "Configuración de IA",
                    "El límite de revisiones se actualizó correctamente.");
            }
            catch (DiagnosticoIAApiException ex) when (ex.StatusCode == 409)
            {
                recargarPorConflicto = true;
                await MostrarAlertaAsync(
                    "Configuración actualizada por otro usuario",
                    ex.Message);
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

            if (recargarPorConflicto)
                await CargarAsync();
        }

        private void AplicarConfiguracion(
            DiagnosticoIAConfiguracion configuracion)
        {
            RevisionesIlimitadas = configuracion.RevisionesIlimitadas;

            maximoPersistido = Math.Clamp(
                configuracion.MaximoRevisionesGemini,
                1,
                20);

            MaximoRevisionesTexto = maximoPersistido.ToString();
            rowVersion = configuracion.RowVersion ?? string.Empty;

            ResumenActual = configuracion.Resumen;
            UltimaModificacion =
                $"Última modificación: {configuracion.FechaModificacionTexto} · {configuracion.UsuarioModificacion}";

            Historial.Clear();

            foreach (DiagnosticoIAConfiguracionHistorialItem item
                     in configuracion.Historial)
            {
                Historial.Add(item);
            }

            OnPropertyChanged(nameof(TieneHistorial));
            OnPropertyChanged(nameof(SinHistorial));
            OnPropertyChanged(nameof(AyudaLimite));
        }

        private async Task MostrarErrorAsync(Exception ex)
        {
            if (ex is DiagnosticoIAApiException
                {
                    EsSesionInvalidada: true
                })
            {
                return;
            }

            await MostrarAlertaAsync(
                "Configuración de IA",
                ex.Message);
        }

        private static Task MostrarAlertaAsync(
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

        private static Task<bool> ConfirmarAsync(
            string titulo,
            string mensaje)
        {
            if (Shell.Current == null)
                return Task.FromResult(false);

            return Shell.Current.DisplayAlert(
                titulo,
                mensaje,
                "Guardar",
                "Cancelar");
        }

        private void ActualizarComandos()
        {
            RegresarCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            GuardarCommand.ChangeCanExecute();
            AdministrarTiposFotografiaCommand.ChangeCanExecute();
        }
    }
}
