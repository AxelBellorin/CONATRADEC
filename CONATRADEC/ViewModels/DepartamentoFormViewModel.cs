using CONATRADEC.Models;
using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    public class DepartamentoFormViewModel : GlobalService
    {
        private DepartamentoRequest departamento = new();
        private PaisRequest paisRequest = new();
        private string nombreDepartamento = string.Empty;
        private string errorNombreDepartamento = string.Empty;
        private FormMode.FormModeSelect mode;
        private readonly DepartamentoApiService departamentoApiService = new();

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public DepartamentoFormViewModel()
        {
            SaveCommand = new Command(
                async () => await SaveAsync(),
                () => CanSave && !IsBusy);

            CancelCommand = new Command(
                async () => await CancelAsync(),
                () => !IsBusy);
        }

        public DepartamentoRequest Departamento
        {
            get => departamento;
            set
            {
                departamento = value ?? new DepartamentoRequest();
                NombreDepartamento =
                    departamento.NombreDepartamento ?? string.Empty;
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
            }
        }

        public string NombreDepartamento
        {
            get => nombreDepartamento;
            set
            {
                nombreDepartamento = value ?? string.Empty;
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
                mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEntryReadOnly));
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(Title));
                RefrescarComandos();
            }
        }

        public bool IsEntryReadOnly =>
            Mode == FormMode.FormModeSelect.View;

        public bool CanSave =>
            Mode != FormMode.FormModeSelect.View;

        public string Title =>
            Mode == FormMode.FormModeSelect.Create
                ? "Crear departamento"
                : Mode == FormMode.FormModeSelect.Edit
                    ? "Editar departamento"
                    : "Detalles del departamento";

        private async Task SaveAsync()
        {
            if (!CanSave || IsBusy)
                return;

            if (!ValidarCampos())
            {
                await MostrarAdvertenciaAsync(
                    "Revise los campos marcados antes de continuar.");
                return;
            }

            bool confirm = Mode == FormMode.FormModeSelect.Create
                ? await ConfirmarGuardadoAsync("el departamento")
                : await ConfirmarActualizacionAsync("el departamento");

            if (!confirm)
                return;

            try
            {
                IsBusy = true;
                RefrescarComandos();

                Departamento.NombreDepartamento =
                    NombreDepartamento.Trim();

                Departamento.PaisId = PaisRequest.PaisId;

                bool ok = Mode == FormMode.FormModeSelect.Create
                    ? await departamentoApiService
                        .CreateDepartamentoAsync(Departamento)
                    : await departamentoApiService
                        .UpdateDepartamentoAsync(Departamento);

                if (!ok)
                {
                    await MostrarErrorAsync(
                        Mode == FormMode.FormModeSelect.Create
                            ? "No fue posible guardar el departamento. Intente nuevamente."
                            : "No fue posible actualizar el departamento. Intente nuevamente.");
                    return;
                }

                await ReturnToList();

                await MostrarExitoAsync(
                    Mode == FormMode.FormModeSelect.Create
                        ? "Departamento guardado correctamente."
                        : "Departamento actualizado correctamente.");
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    Mode == FormMode.FormModeSelect.Create
                        ? "guardar el departamento"
                        : "actualizar el departamento",
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

            await ReturnToList();
        }

        private bool ValidarCampos()
        {
            LimpiarErrores();
            NombreDepartamento = NombreDepartamento.Trim();

            if (string.IsNullOrWhiteSpace(NombreDepartamento))
            {
                ErrorNombreDepartamento =
                    "Ingrese el nombre del departamento.";
            }

            return !TieneErrorNombreDepartamento;
        }

        private void LimpiarErrores()
        {
            ErrorNombreDepartamento = string.Empty;
        }

        private Task ReturnToList()
        {
            var parameters = new Dictionary<string, object>
            {
                { "Pais", PaisRequest },
                {
                    "TitlePage",
                    $"Departamento de {PaisRequest.NombrePais}"
                }
            };

            return GoToAsyncParameters(
                "//DepartamentoPage",
                parameters);
        }

        private void RefrescarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
        }
    }
}
