using CONATRADEC.Models;
using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Formulario para crear, editar y visualizar roles.
    ///
    /// La operación no se bloquea únicamente por el estado informado
    /// por Connectivity en Windows. La llamada real a la API determina
    /// si existe un problema de conexión, servidor o validación.
    /// </summary>
    public class RolFormViewModel : GlobalService
    {
        private RolRequest rol = new();

        private string nombreRol = string.Empty;
        private string descripcionRol = string.Empty;

        private string errorNombreRol = string.Empty;
        private string errorDescripcionRol = string.Empty;

        private FormMode.FormModeSelect mode =
            new FormMode.FormModeSelect();

        private readonly RolApiService rolApiService = new();

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public RolFormViewModel()
        {
            SaveCommand = new Command(
                async () => await SaveAsync(),
                () => !IsReadOnly && !IsBusy);

            CancelCommand = new Command(
                async () => await CancelAsync(),
                () => !IsBusy);
        }

        public RolRequest Rol
        {
            get => rol;
            set
            {
                rol = value ?? new RolRequest();

                OnPropertyChanged();

                NombreRol =
                    rol.NombreRol ?? string.Empty;

                DescripcionRol =
                    rol.DescripcionRol ?? string.Empty;

                LimpiarErrores();
            }
        }

        public string NombreRol
        {
            get => nombreRol;
            set
            {
                nombreRol = value ?? string.Empty;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(nombreRol))
                    ErrorNombreRol = string.Empty;
            }
        }

        public string DescripcionRol
        {
            get => descripcionRol;
            set
            {
                descripcionRol = value ?? string.Empty;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(descripcionRol))
                    ErrorDescripcionRol = string.Empty;
            }
        }

        public string ErrorNombreRol
        {
            get => errorNombreRol;
            private set
            {
                if (errorNombreRol == value)
                    return;

                errorNombreRol = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorNombreRol));
            }
        }

        public bool TieneErrorNombreRol =>
            !string.IsNullOrWhiteSpace(ErrorNombreRol);

        public string ErrorDescripcionRol
        {
            get => errorDescripcionRol;
            private set
            {
                if (errorDescripcionRol == value)
                    return;

                errorDescripcionRol = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorDescripcionRol));
            }
        }

        public bool TieneErrorDescripcionRol =>
            !string.IsNullOrWhiteSpace(ErrorDescripcionRol);

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                mode = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(ShowSaveButton));
                OnPropertyChanged(nameof(Title));

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
                    "Crear rol",

                FormMode.FormModeSelect.Edit =>
                    "Editar rol",

                FormMode.FormModeSelect.View =>
                    "Detalles del rol",

                _ =>
                    "Rol"
            };

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

            if (Mode == FormMode.FormModeSelect.Create)
            {
                await CrearRolAsync();
                return;
            }

            if (Mode == FormMode.FormModeSelect.Edit)
            {
                await ActualizarRolAsync();
            }
        }

        private async Task CrearRolAsync()
        {
            bool confirm =
                await ConfirmarGuardadoAsync("el rol");

            if (!confirm)
                return;

            try
            {
                IsBusy = true;
                RefrescarComandos();

                SincronizarModelo();

                ApiResult<bool> result =
                    await rolApiService.CreateRolResultAsync(Rol);

                if (!result.Success || result.Data != true)
                {
                    await MostrarErrorAsync(
                        string.IsNullOrWhiteSpace(result.Message)
                            ? "No fue posible guardar el rol. Intente nuevamente."
                            : result.Message);

                    return;
                }

                await GoToAsyncParameters("//RolPage");

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Rol guardado correctamente."
                        : result.Message);
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "guardar el rol",
                    ex);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task ActualizarRolAsync()
        {
            if (!HayCambios())
            {
                await MostrarInformacionAsync(
                    "No hay cambios para guardar.");

                return;
            }

            bool confirm =
                await ConfirmarActualizacionAsync("el rol");

            if (!confirm)
                return;

            try
            {
                IsBusy = true;
                RefrescarComandos();

                SincronizarModelo();

                ApiResult<bool> result =
                    await rolApiService.UpdateRolResultAsync(Rol);

                if (!result.Success || result.Data != true)
                {
                    await MostrarErrorAsync(
                        string.IsNullOrWhiteSpace(result.Message)
                            ? "No fue posible actualizar el rol. Intente nuevamente."
                            : result.Message);

                    return;
                }

                await GoToAsyncParameters("//RolPage");

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Rol actualizado correctamente."
                        : result.Message);
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "actualizar el rol",
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

            try
            {
                if (HayCambios())
                {
                    bool confirm =
                        await ConfirmarSalidaSinGuardarAsync();

                    if (!confirm)
                        return;
                }

                await GoToAsyncParameters("//RolPage");
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "salir del formulario de rol",
                    ex);
            }
        }

        private bool ValidarCampos()
        {
            LimpiarErrores();

            NombreRol = NombreRol.Trim();
            DescripcionRol = DescripcionRol.Trim();

            if (string.IsNullOrWhiteSpace(NombreRol))
            {
                ErrorNombreRol =
                    "Ingrese el nombre del rol.";
            }

            if (string.IsNullOrWhiteSpace(DescripcionRol))
            {
                ErrorDescripcionRol =
                    "Ingrese la descripción del rol.";
            }

            return
                !TieneErrorNombreRol &&
                !TieneErrorDescripcionRol;
        }

        private bool HayCambios()
        {
            string nombreActual =
                NombreRol.Trim();

            string descripcionActual =
                DescripcionRol.Trim();

            string nombreOriginal =
                Rol.NombreRol?.Trim() ?? string.Empty;

            string descripcionOriginal =
                Rol.DescripcionRol?.Trim() ?? string.Empty;

            return
                nombreActual != nombreOriginal ||
                descripcionActual != descripcionOriginal;
        }

        private void SincronizarModelo()
        {
            Rol.NombreRol = NombreRol.Trim();
            Rol.DescripcionRol = DescripcionRol.Trim();
        }

        private void LimpiarErrores()
        {
            ErrorNombreRol = string.Empty;
            ErrorDescripcionRol = string.Empty;
        }

        private void RefrescarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
        }
    }
}
