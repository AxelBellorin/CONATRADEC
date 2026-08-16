using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class DepartamentoFormViewModel : GlobalService
    {
        private readonly DepartamentoApiService departamentoApiService;
        private CancellationTokenSource? guardadoCts;
        private int guardadoEnCurso;

        private DepartamentoRequest departamento = new();
        private PaisRequest paisRequest = new();
        private string nombreDepartamento = string.Empty;
        private string nombreOriginal = string.Empty;
        private string errorNombreDepartamento = string.Empty;
        private FormMode.FormModeSelect mode;

        public DepartamentoFormViewModel()
            : this(new DepartamentoApiService())
        {
        }

        public DepartamentoFormViewModel(
            DepartamentoApiService departamentoApiService)
        {
            this.departamentoApiService = departamentoApiService
                ?? throw new ArgumentNullException(nameof(departamentoApiService));

            SaveCommand = new Command(
                async () => await SaveAsync(),
                () => CanSave && !IsBusy);

            CancelCommand = new Command(
                async () => await CancelAsync(),
                () => !IsBusy);
        }

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public DepartamentoRequest Departamento
        {
            get => departamento;
            set
            {
                departamento = value ?? new DepartamentoRequest();
                NombreDepartamento =
                    departamento.NombreDepartamento ?? string.Empty;
                nombreOriginal = NombreDepartamento.Trim();
                LimpiarErrores();
                OnPropertyChanged();
            }
        }

        public PaisRequest PaisRequest
        {
            get => paisRequest;
            set
            {
                paisRequest = value ?? new PaisRequest();
                OnPropertyChanged();
                OnPropertyChanged(nameof(NombrePais));
                OnPropertyChanged(nameof(CodigoPais));
                OnPropertyChanged(nameof(MostrarCodigoPais));
                OnPropertyChanged(nameof(PaisValido));
                OnPropertyChanged(nameof(Subtitulo));
                RefrescarComandos();
            }
        }

        public string NombrePais =>
            string.IsNullOrWhiteSpace(PaisRequest.NombrePais)
                ? "País seleccionado"
                : PaisRequest.NombrePais;

        public string CodigoPais =>
            PaisRequest.CodigoISOPais ?? string.Empty;

        public bool MostrarCodigoPais =>
            !string.IsNullOrWhiteSpace(CodigoPais);

        public bool PaisValido => PaisRequest.PaisId > 0;

        public string NombreDepartamento
        {
            get => nombreDepartamento;
            set
            {
                string nuevoValor = value ?? string.Empty;
                if (nombreDepartamento == nuevoValor)
                    return;

                nombreDepartamento = nuevoValor;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(nombreDepartamento))
                    ErrorNombreDepartamento = string.Empty;
            }
        }

        public string ErrorNombreDepartamento
        {
            get => errorNombreDepartamento;
            private set
            {
                if (errorNombreDepartamento == value)
                    return;

                errorNombreDepartamento = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorNombreDepartamento));
            }
        }

        public bool TieneErrorNombreDepartamento =>
            !string.IsNullOrWhiteSpace(ErrorNombreDepartamento);

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
            PaisValido &&
            (Mode switch
            {
                FormMode.FormModeSelect.Create => CanAdd,
                FormMode.FormModeSelect.Edit => CanEdit,
                _ => false
            });

        public bool ShowSaveButton => CanSave;

        public string Title =>
            Mode switch
            {
                FormMode.FormModeSelect.Create => "Crear departamento",
                FormMode.FormModeSelect.Edit => "Editar departamento",
                FormMode.FormModeSelect.View => "Detalles del departamento",
                _ => "Departamento"
            };

        public string Subtitulo =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    $"Registre un departamento asociado a {NombrePais}.",
                FormMode.FormModeSelect.Edit =>
                    $"Actualice el departamento seleccionado de {NombrePais}.",
                FormMode.FormModeSelect.View =>
                    "Consulte la información registrada.",
                _ => string.Empty
            };

        public void ActualizarPermisos()
        {
            LoadPagePermissions("departamentoPage");
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
            if (!CanSave || IsBusy)
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
                ? await ConfirmarGuardadoAsync("el departamento")
                : await ConfirmarActualizacionAsync("el departamento");

            if (!confirmar)
                return;

            guardadoCts?.Cancel();
            guardadoCts?.Dispose();
            guardadoCts = new CancellationTokenSource();

            try
            {
                IsBusy = true;
                RefrescarComandos();

                Departamento.NombreDepartamento =
                    NombreDepartamento.ReplaceLineEndings(" ").Trim().ToUpperInvariant();
                Departamento.PaisId = PaisRequest.PaisId;

                ApiResult<bool> resultado =
                    Mode == FormMode.FormModeSelect.Create
                        ? await departamentoApiService
                            .CreateDepartamentoResultAsync(
                                Departamento,
                                guardadoCts.Token)
                        : await departamentoApiService
                            .UpdateDepartamentoResultAsync(
                                Departamento,
                                guardadoCts.Token);

                if (!resultado.Success || resultado.Data != true)
                {
                    await MostrarErrorAsync(resultado.Message);
                    return;
                }

                if (Mode == FormMode.FormModeSelect.Create)
                {
                    // El POST no expone el ID creado: una única recarga de la
                    // página actual garantiza orden y composición correctos.
                    UbicacionVisitaService.MarcarDepartamentosParaRecargar(
                        PaisRequest.PaisId);
                    UbicacionVisitaService.RegistrarDeltaDepartamentosPais(
                        PaisRequest.PaisId,
                        1);
                }
                else
                {
                    UbicacionVisitaService.RegistrarDepartamentoActualizado(
                        PaisRequest.PaisId,
                        Departamento);
                }

                await ReturnToListAsync();

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Departamento guardado correctamente."
                        : resultado.Message);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "guardar el departamento",
                    ex);
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

            await ReturnToListAsync();
        }

        private bool ValidarCampos()
        {
            LimpiarErrores();
            NombreDepartamento =
                NombreDepartamento.ReplaceLineEndings(" ").Trim();

            if (!PaisValido)
                ErrorNombreDepartamento = "No se recibió un país válido.";
            else if (string.IsNullOrWhiteSpace(NombreDepartamento))
                ErrorNombreDepartamento = "Ingrese el nombre del departamento.";
            else if (NombreDepartamento.Length > 80)
                ErrorNombreDepartamento =
                    "El nombre no puede superar 80 caracteres.";

            return !TieneErrorNombreDepartamento;
        }

        private bool HayCambios()
        {
            string nombreActual =
                NombreDepartamento.ReplaceLineEndings(" ").Trim();

            if (Mode == FormMode.FormModeSelect.Create)
                return !string.IsNullOrWhiteSpace(nombreActual);

            return !string.Equals(
                nombreActual,
                nombreOriginal,
                StringComparison.OrdinalIgnoreCase);
        }

        private void LimpiarErrores()
        {
            ErrorNombreDepartamento = string.Empty;
        }

        private Task ReturnToListAsync()
        {
            var parameters = new Dictionary<string, object>
            {
                ["Pais"] = PaisRequest,
                ["TitlePage"] = $"Departamentos de {NombrePais}"
            };

            return GoToAsyncParameters("//DepartamentoPage", parameters);
        }

        private void RefrescarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
        }
    }
}
