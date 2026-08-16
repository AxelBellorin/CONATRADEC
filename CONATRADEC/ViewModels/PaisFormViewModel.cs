using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Text.RegularExpressions;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class PaisFormViewModel : GlobalService
    {
        private readonly PaisApiService paisApiService;
        private CancellationTokenSource? guardadoCts;
        private int guardadoEnCurso;

        private PaisRequest pais = new();
        private string nombrePais = string.Empty;
        private string codigoISOPais = string.Empty;
        private string errorNombrePais = string.Empty;
        private string errorCodigoISOPais = string.Empty;
        private FormMode.FormModeSelect mode;

        public PaisFormViewModel()
            : this(new PaisApiService())
        {
        }

        public PaisFormViewModel(PaisApiService paisApiService)
        {
            this.paisApiService = paisApiService
                ?? throw new ArgumentNullException(nameof(paisApiService));

            SaveCommand = new Command(
                async () => await SaveAsync(),
                () => CanSave && !IsBusy);

            CancelCommand = new Command(
                async () => await CancelAsync(),
                () => !IsBusy);
        }

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public string NombrePais
        {
            get => nombrePais;
            set
            {
                string nuevoValor = value ?? string.Empty;
                if (nombrePais == nuevoValor)
                    return;

                nombrePais = nuevoValor;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(nombrePais))
                    ErrorNombrePais = string.Empty;
            }
        }

        public string CodigoISOPais
        {
            get => codigoISOPais;
            set
            {
                string nuevoValor = new string(
                    (value ?? string.Empty)
                        .Where(char.IsLetter)
                        .Take(3)
                        .ToArray())
                    .ToUpperInvariant();

                if (codigoISOPais == nuevoValor)
                    return;

                codigoISOPais = nuevoValor;
                OnPropertyChanged();

                if (Regex.IsMatch(codigoISOPais, "^[A-Z]{3}$"))
                    ErrorCodigoISOPais = string.Empty;
            }
        }

        public string ErrorNombrePais
        {
            get => errorNombrePais;
            private set
            {
                if (errorNombrePais == value)
                    return;

                errorNombrePais = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorNombrePais));
            }
        }

        public bool TieneErrorNombrePais =>
            !string.IsNullOrWhiteSpace(ErrorNombrePais);

        public string ErrorCodigoISOPais
        {
            get => errorCodigoISOPais;
            private set
            {
                if (errorCodigoISOPais == value)
                    return;

                errorCodigoISOPais = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorCodigoISOPais));
            }
        }

        public bool TieneErrorCodigoISOPais =>
            !string.IsNullOrWhiteSpace(ErrorCodigoISOPais);

        public PaisRequest Pais
        {
            get => pais;
            set
            {
                pais = value ?? new PaisRequest();
                NombrePais = pais.NombrePais ?? string.Empty;
                CodigoISOPais = pais.CodigoISOPais ?? string.Empty;
                LimpiarErrores();
                OnPropertyChanged();
            }
        }

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                if (mode == value)
                    return;

                mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(ShowSaveButton));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Subtitulo));
                RefrescarComandos();
            }
        }

        public bool IsReadOnly =>
            Mode == FormMode.FormModeSelect.View;

        public bool CanSave =>
            Mode switch
            {
                FormMode.FormModeSelect.Create => CanAdd,
                FormMode.FormModeSelect.Edit => CanEdit,
                _ => false
            };

        public bool ShowSaveButton => CanSave;

        public string Title =>
            Mode switch
            {
                FormMode.FormModeSelect.Create => "Crear país",
                FormMode.FormModeSelect.Edit => "Editar país",
                FormMode.FormModeSelect.View => "Detalles del país",
                _ => "País"
            };

        public string Subtitulo =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    "Registre el nombre oficial y el código ISO de tres letras.",
                FormMode.FormModeSelect.Edit =>
                    "Actualice la información general del país seleccionado.",
                FormMode.FormModeSelect.View =>
                    "Consulte la información registrada para este país.",
                _ => string.Empty
            };

        public void ActualizarPermisos()
        {
            LoadPagePermissions("paisPage");
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(ShowSaveButton));
            RefrescarComandos();
        }

        public void CancelarOperaciones()
        {
            try
            {
                guardadoCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private async Task SaveAsync()
        {
            if (Interlocked.CompareExchange(
                    ref guardadoEnCurso,
                    1,
                    0) != 0)
            {
                return;
            }

            try
            {
                await SaveCoreAsync();
            }
            finally
            {
                Volatile.Write(ref guardadoEnCurso, 0);
            }
        }

        private async Task SaveCoreAsync()
        {
            if (!CanSave || IsBusy || IsReadOnly)
                return;

            if (!ValidarCampos())
            {
                await MostrarAdvertenciaAsync(
                    "Revise los campos marcados antes de continuar.");
                return;
            }

            if (!await ValidarInternetAsync())
                return;

            if (Mode == FormMode.FormModeSelect.Edit && !HayCambios())
            {
                await MostrarInformacionAsync("No hay cambios para guardar.");
                return;
            }

            bool confirmar = Mode == FormMode.FormModeSelect.Create
                ? await ConfirmarGuardadoAsync("el país")
                : await ConfirmarActualizacionAsync("el país");

            if (!confirmar)
                return;

            guardadoCts?.Cancel();
            guardadoCts?.Dispose();
            guardadoCts = new CancellationTokenSource();

            try
            {
                IsBusy = true;
                RefrescarComandos();
                SincronizarModelo();

                ApiResult<PaisResponse> resultado =
                    Mode == FormMode.FormModeSelect.Create
                        ? await paisApiService.CreatePaisResultAsync(
                            Pais,
                            guardadoCts.Token)
                        : await paisApiService.UpdatePaisResultAsync(
                            Pais,
                            guardadoCts.Token);

                if (!resultado.Success ||
                    resultado.Data == null ||
                    resultado.Data.PaisId <= 0)
                {
                    await MostrarErrorAsync(resultado.Message);
                    return;
                }

                Pais = new PaisRequest(resultado.Data);

                if (Mode == FormMode.FormModeSelect.Create)
                {
                    // El servidor ya devuelve el DTO real creado/reactivado.
                    // La recarga sigue siendo justificada para ubicarlo dentro
                    // del orden global cuando existen varias páginas.
                    UbicacionVisitaService.MarcarPaisesParaRecargar();
                }
                else
                {
                    UbicacionVisitaService.RegistrarPaisActualizado(Pais);
                }

                await GoToAsyncParameters(AppRoutes.Paises);

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "País guardado correctamente."
                        : resultado.Message);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync("guardar el país", ex);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task CancelAsync()
        {
            if (IsBusy)
                return;

            if (!IsReadOnly && HayCambios())
            {
                bool confirmar = await ConfirmarSalidaSinGuardarAsync();
                if (!confirmar)
                    return;
            }

            await GoToAsyncParameters(AppRoutes.Paises);
        }

        private void SincronizarModelo()
        {
            Pais.NombrePais =
                NombrePais
                    .ReplaceLineEndings(" ")
                    .Trim()
                    .ToUpperInvariant();

            Pais.CodigoISOPais =
                CodigoISOPais
                    .Trim()
                    .ToUpperInvariant();
        }

        private bool HayCambios()
        {
            string nombreActual =
                NombrePais.ReplaceLineEndings(" ").Trim();

            string codigoActual =
                CodigoISOPais.Trim().ToUpperInvariant();

            string nombreOriginal =
                Pais.NombrePais?.Trim() ?? string.Empty;

            string codigoOriginal =
                Pais.CodigoISOPais?
                    .Trim()
                    .ToUpperInvariant()
                ?? string.Empty;

            if (Mode == FormMode.FormModeSelect.Create)
            {
                return !string.IsNullOrWhiteSpace(nombreActual) ||
                       !string.IsNullOrWhiteSpace(codigoActual);
            }

            return
                !string.Equals(
                    nombreActual,
                    nombreOriginal,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    codigoActual,
                    codigoOriginal,
                    StringComparison.Ordinal);
        }

        private bool ValidarCampos()
        {
            LimpiarErrores();

            NombrePais =
                NombrePais.ReplaceLineEndings(" ").Trim();

            CodigoISOPais =
                CodigoISOPais.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(NombrePais))
                ErrorNombrePais = "Ingrese el nombre del país.";
            else if (NombrePais.Length > 80)
                ErrorNombrePais = "El nombre no puede superar 80 caracteres.";

            if (string.IsNullOrWhiteSpace(CodigoISOPais))
                ErrorCodigoISOPais = "Ingrese el código ISO del país.";
            else if (!Regex.IsMatch(CodigoISOPais, "^[A-Z]{3}$"))
            {
                ErrorCodigoISOPais =
                    "El código ISO debe contener exactamente 3 letras.";
            }

            return !TieneErrorNombrePais &&
                   !TieneErrorCodigoISOPais;
        }

        private void LimpiarErrores()
        {
            ErrorNombrePais = string.Empty;
            ErrorCodigoISOPais = string.Empty;
        }

        private void RefrescarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
        }
    }
}
