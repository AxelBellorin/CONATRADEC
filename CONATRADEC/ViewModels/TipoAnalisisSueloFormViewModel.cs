using CONATRADEC.Models;
using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    public class TipoAnalisisSueloFormViewModel : GlobalService
    {
        private readonly TipoAnalisisSueloApiService apiService = new();
        private TipoAnalisisSueloRequest item = new();
        private FormMode.FormModeSelect mode;
        private string nombre = string.Empty;
        private string descripcion = string.Empty;
        private string errorNombre = string.Empty;
        private string errorDescripcion = string.Empty;

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public TipoAnalisisSueloFormViewModel()
        {
            SaveCommand = new Command(
                async () => await SaveAsync(),
                () => !IsReadOnly && !IsBusy);

            CancelCommand = new Command(
                async () => await CancelAsync(),
                () => !IsBusy);
        }

        public TipoAnalisisSueloRequest Item
        {
            get => item;
            set
            {
                item =
                    value ?? new TipoAnalisisSueloRequest();

                Nombre =
                    item.NombreTipoAnalisisSuelo
                    ?? string.Empty;

                Descripcion =
                    item.DescripcionTipoAnalisisSuelo
                    ?? string.Empty;

                LimpiarErrores();
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
                OnPropertyChanged(nameof(IsEditable));
                OnPropertyChanged(nameof(ShowSaveButton));
                OnPropertyChanged(nameof(Title));
                RefrescarComandos();
            }
        }

        public string Nombre
        {
            get => nombre;
            set
            {
                nombre = value ?? string.Empty;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(nombre))
                    ErrorNombre = string.Empty;
            }
        }

        public string Descripcion
        {
            get => descripcion;
            set
            {
                descripcion = value ?? string.Empty;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(descripcion))
                    ErrorDescripcion = string.Empty;
            }
        }

        public string ErrorNombre
        {
            get => errorNombre;
            private set
            {
                if (errorNombre == value)
                    return;

                errorNombre = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorNombre));
            }
        }

        public bool TieneErrorNombre =>
            !string.IsNullOrWhiteSpace(ErrorNombre);

        public string ErrorDescripcion
        {
            get => errorDescripcion;
            private set
            {
                if (errorDescripcion == value)
                    return;

                errorDescripcion = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorDescripcion));
            }
        }

        public bool TieneErrorDescripcion =>
            !string.IsNullOrWhiteSpace(ErrorDescripcion);

        public bool IsReadOnly =>
            Mode == FormMode.FormModeSelect.View;

        public bool IsEditable => !IsReadOnly;

        public bool ShowSaveButton => !IsReadOnly;

        public string Title =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    "Crear tipo de análisis de suelo",
                FormMode.FormModeSelect.Edit =>
                    "Editar tipo de análisis de suelo",
                _ =>
                    "Detalle del tipo de análisis de suelo"
            };

        private bool HasChanges() =>
            !string.Equals(
                Nombre.Trim(),
                Item.NombreTipoAnalisisSuelo?.Trim()
                    ?? string.Empty,
                StringComparison.Ordinal) ||
            !string.Equals(
                Descripcion.Trim(),
                Item.DescripcionTipoAnalisisSuelo?.Trim()
                    ?? string.Empty,
                StringComparison.Ordinal);

        private async Task CancelAsync()
        {
            if (!IsReadOnly && HasChanges())
            {
                bool confirm =
                    await ConfirmarSalidaSinGuardarAsync();

                if (!confirm)
                    return;
            }

            await GoToAsyncParameters(
                AppRoutes.TiposAnalisisSuelo);
        }

        private async Task SaveAsync()
        {
            if (IsReadOnly || IsBusy)
                return;

            if (!ValidarCampos())
            {
                await MostrarAdvertenciaAsync(
                    "Revise los campos marcados antes de continuar.");
                return;
            }

            if (!HasChanges())
            {
                await MostrarInformacionAsync(
                    "No hay cambios para guardar.");
                return;
            }

            bool confirm =
                Mode == FormMode.FormModeSelect.Create
                    ? await ConfirmarGuardadoAsync(
                        "el tipo de análisis de suelo")
                    : await ConfirmarActualizacionAsync(
                        "el tipo de análisis de suelo");

            if (!confirm)
                return;

            Item.NombreTipoAnalisisSuelo =
                Nombre.Trim();

            Item.DescripcionTipoAnalisisSuelo =
                Descripcion.Trim();

            try
            {
                IsBusy = true;
                RefrescarComandos();

                ApiResult<bool> result =
                    Mode == FormMode.FormModeSelect.Create
                        ? await apiService.CreateAsync(Item)
                        : await apiService.UpdateAsync(Item);

                if (!result.Success)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                await GoToAsyncParameters(
                    AppRoutes.TiposAnalisisSuelo);

                await MostrarExitoAsync(result.Message);
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    Mode == FormMode.FormModeSelect.Create
                        ? "guardar el tipo de análisis de suelo"
                        : "actualizar el tipo de análisis de suelo",
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

            Nombre = Nombre.Trim();
            Descripcion = Descripcion.Trim();

            if (string.IsNullOrWhiteSpace(Nombre))
            {
                ErrorNombre =
                    "Ingrese el nombre del tipo de análisis.";
            }

            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                ErrorDescripcion =
                    "Ingrese la descripción del tipo de análisis.";
            }

            return
                !TieneErrorNombre &&
                !TieneErrorDescripcion;
        }

        private void LimpiarErrores()
        {
            ErrorNombre = string.Empty;
            ErrorDescripcion = string.Empty;
        }

        private void RefrescarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
        }
    }
}
