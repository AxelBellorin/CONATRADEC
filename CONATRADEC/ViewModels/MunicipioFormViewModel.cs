using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class MunicipioFormViewModel : GlobalService
    {
        private readonly MunicipioApiService municipioApiService;
        private CancellationTokenSource? guardadoCts;
        private int guardadoEnCurso;

        private DepartamentoRequest departamentoRequest = new();
        private PaisRequest paisRequest = new();
        private MunicipioRequest municipioRequest = new();
        private string nombreMunicipio = string.Empty;
        private string nombreOriginal = string.Empty;
        private string errorNombreMunicipio = string.Empty;
        private FormMode.FormModeSelect mode;

        public MunicipioFormViewModel()
            : this(new MunicipioApiService())
        {
        }

        public MunicipioFormViewModel(MunicipioApiService municipioApiService)
        {
            this.municipioApiService = municipioApiService
                ?? throw new ArgumentNullException(nameof(municipioApiService));

            SaveCommand = new Command(
                async () => await SaveAsync(),
                () => CanSave && !IsBusy);

            CancelCommand = new Command(
                async () => await CancelAsync(),
                () => !IsBusy);
        }

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public MunicipioRequest MunicipioRequest
        {
            get => municipioRequest;
            set
            {
                municipioRequest = value ?? new MunicipioRequest();
                NombreMunicipio =
                    municipioRequest.NombreMunicipio ?? string.Empty;
                nombreOriginal = NombreMunicipio.Trim();
                LimpiarErrores();
                OnPropertyChanged();
            }
        }

        public DepartamentoRequest DepartamentoRequest
        {
            get => departamentoRequest;
            set
            {
                departamentoRequest = value ?? new DepartamentoRequest();
                OnPropertyChanged();
                OnPropertyChanged(nameof(NombreDepartamento));
                OnPropertyChanged(nameof(DepartamentoValido));
                OnPropertyChanged(nameof(UbicacionValida));
                OnPropertyChanged(nameof(Subtitulo));
                RefrescarComandos();
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
                OnPropertyChanged(nameof(UbicacionValida));
                OnPropertyChanged(nameof(Subtitulo));
                RefrescarComandos();
            }
        }

        public string NombreMunicipio
        {
            get => nombreMunicipio;
            set
            {
                string nuevoValor = value ?? string.Empty;
                if (nombreMunicipio == nuevoValor)
                    return;

                nombreMunicipio = nuevoValor;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(nombreMunicipio))
                    ErrorNombreMunicipio = string.Empty;
            }
        }

        public string ErrorNombreMunicipio
        {
            get => errorNombreMunicipio;
            private set
            {
                if (errorNombreMunicipio == value)
                    return;

                errorNombreMunicipio = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorNombreMunicipio));
            }
        }

        public bool TieneErrorNombreMunicipio =>
            !string.IsNullOrWhiteSpace(ErrorNombreMunicipio);

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
                OnPropertyChanged(nameof(TextoBotonCancelar));
                RefrescarComandos();
            }
        }

        public string NombreDepartamento =>
            string.IsNullOrWhiteSpace(DepartamentoRequest.NombreDepartamento)
                ? "Departamento seleccionado"
                : DepartamentoRequest.NombreDepartamento;

        public string NombrePais =>
            string.IsNullOrWhiteSpace(PaisRequest.NombrePais)
                ? "País seleccionado"
                : PaisRequest.NombrePais;

        public string CodigoPais =>
            PaisRequest.CodigoISOPais ?? string.Empty;

        public bool MostrarCodigoPais =>
            !string.IsNullOrWhiteSpace(CodigoPais);

        public bool DepartamentoValido =>
            DepartamentoRequest.DepartamentoId is > 0;

        public bool PaisValido => PaisRequest.PaisId > 0;
        public bool UbicacionValida => DepartamentoValido && PaisValido;

        public bool IsReadOnly =>
            Mode == FormMode.FormModeSelect.View;

        public bool CanSave =>
            UbicacionValida &&
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
                FormMode.FormModeSelect.Create => "Crear municipio",
                FormMode.FormModeSelect.Edit => "Editar municipio",
                FormMode.FormModeSelect.View => "Detalles del municipio",
                _ => "Municipio"
            };

        public string Subtitulo =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    $"Registre un municipio en {NombreDepartamento}, {NombrePais}.",
                FormMode.FormModeSelect.Edit =>
                    $"Actualice el municipio seleccionado de {NombreDepartamento}.",
                FormMode.FormModeSelect.View =>
                    "Consulte la ubicación y la información registrada.",
                _ => string.Empty
            };

        public string TextoBotonCancelar =>
            IsReadOnly ? "Regresar" : "Cancelar";

        public void ActualizarPermisos()
        {
            LoadPagePermissions("municipioPage");
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
                ? await ConfirmarGuardadoAsync("el municipio")
                : await ConfirmarActualizacionAsync("el municipio");

            if (!confirmar)
                return;

            guardadoCts?.Cancel();
            guardadoCts?.Dispose();
            guardadoCts = new CancellationTokenSource();

            try
            {
                IsBusy = true;
                RefrescarComandos();

                MunicipioRequest.NombreMunicipio =
                    NombreMunicipio.ReplaceLineEndings(" ").Trim().ToUpperInvariant();
                MunicipioRequest.DepartamentoId =
                    DepartamentoRequest.DepartamentoId;

                ApiResult<bool> resultado =
                    Mode == FormMode.FormModeSelect.Create
                        ? await municipioApiService.CreateMunicipioResultAsync(
                            MunicipioRequest,
                            guardadoCts.Token)
                        : await municipioApiService.UpdateMunicipioResultAsync(
                            MunicipioRequest,
                            guardadoCts.Token);

                if (!resultado.Success || resultado.Data != true)
                {
                    await MostrarErrorAsync(resultado.Message);
                    return;
                }

                int departamentoId = DepartamentoRequest.DepartamentoId!.Value;

                if (Mode == FormMode.FormModeSelect.Create)
                {
                    UbicacionVisitaService.MarcarMunicipiosParaRecargar(
                        departamentoId);
                    UbicacionVisitaService.RegistrarDeltaMunicipiosDepartamento(
                        departamentoId,
                        1);
                }
                else
                {
                    UbicacionVisitaService.RegistrarMunicipioActualizado(
                        departamentoId,
                        MunicipioRequest);
                }

                await ReturnToListAsync();

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Municipio guardado correctamente."
                        : resultado.Message);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync("guardar el municipio", ex);
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
            NombreMunicipio =
                NombreMunicipio.ReplaceLineEndings(" ").Trim();

            if (!UbicacionValida)
                ErrorNombreMunicipio = "No se recibió una ubicación válida.";
            else if (string.IsNullOrWhiteSpace(NombreMunicipio))
                ErrorNombreMunicipio = "Ingrese el nombre del municipio.";
            else if (NombreMunicipio.Length > 80)
                ErrorNombreMunicipio =
                    "El nombre no puede superar 80 caracteres.";

            return !TieneErrorNombreMunicipio;
        }

        private bool HayCambios()
        {
            string nombreActual =
                NombreMunicipio.ReplaceLineEndings(" ").Trim();

            if (Mode == FormMode.FormModeSelect.Create)
                return !string.IsNullOrWhiteSpace(nombreActual);

            return !string.Equals(
                nombreActual,
                nombreOriginal,
                StringComparison.OrdinalIgnoreCase);
        }

        private void LimpiarErrores()
        {
            ErrorNombreMunicipio = string.Empty;
        }

        private Task ReturnToListAsync()
        {
            var parametros = new Dictionary<string, object>
            {
                ["Pais"] = PaisRequest,
                ["Departamento"] = DepartamentoRequest,
                ["TitlePage"] =
                    $"Municipios de {NombreDepartamento} - {NombrePais}"
            };

            return GoToAsyncParameters("//MunicipioPage", parametros);
        }

        private void RefrescarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
        }
    }
}
