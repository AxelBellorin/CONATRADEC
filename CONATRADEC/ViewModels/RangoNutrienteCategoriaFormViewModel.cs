using CONATRADEC.Models;
using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    public class RangoNutrienteCategoriaFormViewModel :
        GlobalService
    {
        private readonly TipoCultivoApiService
            apiService = new();

        private TipoCultivoRequest item = new();
        private FormMode.FormModeSelect mode;

        private string nombre = string.Empty;
        private string descripcion = string.Empty;

        private string errorNombre = string.Empty;
        private string errorDescripcion = string.Empty;

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public RangoNutrienteCategoriaFormViewModel()
        {
            SaveCommand =
                new Command(
                    async () => await SaveAsync(),
                    () => !IsReadOnly && !IsBusy);

            CancelCommand =
                new Command(
                    async () => await CancelAsync(),
                    () => !IsBusy);
        }

        public TipoCultivoRequest Item
        {
            get => item;
            set
            {
                item =
                    value ?? new TipoCultivoRequest();

                Nombre =
                    item.NombreTipoCultivo ??
                    string.Empty;

                Descripcion =
                    item.DescripcionTipoCultivo ??
                    string.Empty;

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

                if (!string.IsNullOrWhiteSpace(
                        descripcion))
                {
                    ErrorDescripcion = string.Empty;
                }
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
                OnPropertyChanged(
                    nameof(TieneErrorNombre));
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
                OnPropertyChanged(
                    nameof(TieneErrorDescripcion));
            }
        }

        public bool TieneErrorDescripcion =>
            !string.IsNullOrWhiteSpace(
                ErrorDescripcion);

        public bool IsReadOnly =>
            Mode == FormMode.FormModeSelect.View;

        public bool IsEditable => !IsReadOnly;

        public bool ShowSaveButton => !IsReadOnly;

        public string Title =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    "Crear tipo de cultivo",

                FormMode.FormModeSelect.Edit =>
                    "Editar tipo de cultivo",

                _ =>
                    "Detalle del tipo de cultivo"
            };

        private bool TryGetValues()
        {
            LimpiarErrores();

            Nombre = Nombre.Trim();
            Descripcion = Descripcion.Trim();

            if (string.IsNullOrWhiteSpace(Nombre))
            {
                ErrorNombre =
                    "Ingrese el nombre del tipo de cultivo.";
            }

            if (string.IsNullOrWhiteSpace(
                    Descripcion))
            {
                ErrorDescripcion =
                    "Ingrese la descripción del tipo de cultivo.";
            }

            return
                !TieneErrorNombre &&
                !TieneErrorDescripcion;
        }

        private bool HasChanges() =>
            !string.Equals(
                Nombre.Trim(),
                Item.NombreTipoCultivo?.Trim() ??
                string.Empty,
                StringComparison.Ordinal) ||
            !string.Equals(
                Descripcion.Trim(),
                Item.DescripcionTipoCultivo?.Trim() ??
                string.Empty,
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
                AppRoutes.RangosNutrientes);
        }

        private async Task SaveAsync()
        {
            if (IsReadOnly || IsBusy)
                return;

            if (!TryGetValues())
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
                        "el tipo de cultivo")
                    : await ConfirmarActualizacionAsync(
                        "el tipo de cultivo");

            if (!confirm)
                return;

            Item.NombreTipoCultivo =
                Nombre.Trim();

            Item.DescripcionTipoCultivo =
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
                    await MostrarErrorAsync(
                        result.Message);
                    return;
                }

                await GoToAsyncParameters(
                    AppRoutes.RangosNutrientes);

                await MostrarExitoAsync(
                    result.Message);
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    Mode == FormMode.FormModeSelect.Create
                        ? "guardar el tipo de cultivo"
                        : "actualizar el tipo de cultivo",
                    ex);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
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
