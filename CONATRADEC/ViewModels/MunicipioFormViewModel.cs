using CONATRADEC.Models;
using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    public class MunicipioFormViewModel : GlobalService
    {
        private DepartamentoRequest departamentoRequest = new();
        private PaisRequest paisRequest = new();
        private MunicipioRequest municipioRequest = new();
        private string nombreMunicipio = string.Empty;
        private string errorNombreMunicipio = string.Empty;
        private FormMode.FormModeSelect mode = new();
        private readonly MunicipioApiService municipioApiService = new();

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public MunicipioFormViewModel()
        {
            SaveCommand = new Command(
                async () => await SaveAsync(),
                () => !IsReadOnly && !IsBusy);

            CancelCommand = new Command(
                async () => await CancelAsync(),
                () => !IsBusy);
        }

        public string NombreMunicipio
        {
            get => nombreMunicipio;
            set
            {
                nombreMunicipio = value ?? string.Empty;
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

        public MunicipioRequest MunicipioRequest
        {
            get => municipioRequest;
            set
            {
                municipioRequest = value ?? new MunicipioRequest();
                NombreMunicipio =
                    municipioRequest.NombreMunicipio ?? string.Empty;
                LimpiarErrores();
                OnPropertyChanged();
            }
        }

        public DepartamentoRequest DepartamentoRequest
        {
            get => departamentoRequest;
            set
            {
                departamentoRequest =
                    value ?? new DepartamentoRequest();
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
            }
        }

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(ShowSaveButton));
                RefrescarComandos();
            }
        }

        public bool IsReadOnly =>
            Mode == FormMode.FormModeSelect.View;

        public bool ShowSaveButton =>
            Mode != FormMode.FormModeSelect.View;

        public string Title =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    "Crear municipio",
                FormMode.FormModeSelect.Edit =>
                    "Editar municipio",
                FormMode.FormModeSelect.View =>
                    "Detalles del municipio",
                _ =>
                    "Municipio"
            };

        private async Task CancelAsync()
        {
            if (IsBusy)
                return;

            try
            {
                if (!IsReadOnly && HayCambios())
                {
                    bool confirm =
                        await ConfirmarSalidaSinGuardarAsync();

                    if (!confirm)
                        return;
                }

                await ReturnToList();
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "salir del formulario de municipio",
                    ex);
            }
        }

        private async Task SaveAsync()
        {
            if (IsBusy || IsReadOnly)
                return;

            if (!ValidarCampos())
            {
                await MostrarAdvertenciaAsync(
                    "Revise los campos marcados antes de continuar.");
                return;
            }

            bool confirm = Mode == FormMode.FormModeSelect.Create
                ? await ConfirmarGuardadoAsync("el municipio")
                : await ConfirmarActualizacionAsync("el municipio");

            if (!confirm)
                return;

            try
            {
                IsBusy = true;
                RefrescarComandos();

                MunicipioRequest.NombreMunicipio =
                    NombreMunicipio.Trim();

                MunicipioRequest.DepartamentoId =
                    DepartamentoRequest.DepartamentoId;

                if (Mode == FormMode.FormModeSelect.Create &&
                    !await ValidarInternetAsync())
                {
                    return;
                }

                bool response =
                    Mode == FormMode.FormModeSelect.Create
                        ? await municipioApiService
                            .CreateMunicipioAsync(MunicipioRequest)
                        : await municipioApiService
                            .UpdateMunicipioAsync(MunicipioRequest);

                if (!response)
                {
                    await MostrarErrorAsync(
                        Mode == FormMode.FormModeSelect.Create
                            ? "No fue posible guardar el municipio. Intente nuevamente."
                            : "No fue posible actualizar el municipio. Intente nuevamente.");
                    return;
                }

                await ReturnToList();

                await MostrarExitoAsync(
                    Mode == FormMode.FormModeSelect.Create
                        ? "Municipio guardado correctamente."
                        : "Municipio actualizado correctamente.");
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    Mode == FormMode.FormModeSelect.Create
                        ? "guardar el municipio"
                        : "actualizar el municipio",
                    ex);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private bool ValidarCampos()
        {
            LimpiarErrores();
            NombreMunicipio = NombreMunicipio.Trim();

            if (string.IsNullOrWhiteSpace(NombreMunicipio))
            {
                ErrorNombreMunicipio =
                    "Ingrese el nombre del municipio.";
            }

            if (DepartamentoRequest?.DepartamentoId is not > 0)
            {
                _ = MostrarAdvertenciaAsync(
                    "No se recibió un departamento válido.");
                return false;
            }

            return !TieneErrorNombreMunicipio;
        }

        private bool HayCambios()
        {
            return !string.Equals(
                NombreMunicipio.Trim(),
                MunicipioRequest.NombreMunicipio?.Trim()
                    ?? string.Empty,
                StringComparison.Ordinal);
        }

        private void LimpiarErrores()
        {
            ErrorNombreMunicipio = string.Empty;
        }

        private Task ReturnToList()
        {
            var parameters = new Dictionary<string, object>
            {
                { "Pais", PaisRequest },
                { "Departamento", DepartamentoRequest },
                {
                    "TitlePage",
                    $"Municipios de {DepartamentoRequest.NombreDepartamento} - {PaisRequest.NombrePais}"
                }
            };

            return GoToAsyncParameters(
                "//MunicipioPage",
                parameters);
        }

        private void RefrescarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
        }
    }
}
