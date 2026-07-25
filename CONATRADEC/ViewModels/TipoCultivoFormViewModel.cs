using CONATRADEC.Models;
using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    public sealed class TipoCultivoFormViewModel : GlobalService
    {
        private readonly TipoCultivoApiService apiService;
        private CancellationTokenSource? guardadoCts;

        private TipoCultivoRequest item = new();
        private FormMode.FormModeSelect mode;
        private string nombre = string.Empty;
        private string descripcion = string.Empty;
        private string nombreOriginal = string.Empty;
        private string descripcionOriginal = string.Empty;
        private string errorNombre = string.Empty;
        private string errorDescripcion = string.Empty;

        public TipoCultivoFormViewModel()
            : this(new TipoCultivoApiService())
        {
        }

        public TipoCultivoFormViewModel(
            TipoCultivoApiService apiService)
        {
            this.apiService =
                apiService
                ?? throw new ArgumentNullException(
                    nameof(apiService));

            SaveCommand =
                new Command(
                    async () => await SaveAsync(),
                    () =>
                        CanSave &&
                        !IsBusy);

            CancelCommand =
                new Command(
                    async () => await CancelAsync(),
                    () =>
                        !IsBusy);
        }

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public TipoCultivoRequest Item
        {
            get => item;
            set
            {
                item =
                    value ??
                    new TipoCultivoRequest();

                Nombre =
                    item.NombreTipoCultivo ??
                    string.Empty;

                Descripcion =
                    item.DescripcionTipoCultivo ??
                    string.Empty;

                nombreOriginal =
                    Nombre.Trim();

                descripcionOriginal =
                    Descripcion.Trim();

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

                mode =
                    value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(ShowSaveButton));
                OnPropertyChanged(nameof(MostrarBotonCancelar));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Subtitulo));

                RefrescarComandos();
            }
        }

        public string Nombre
        {
            get => nombre;
            set
            {
                string nuevoValor =
                    (value ?? string.Empty)
                        .ReplaceLineEndings(" ");

                if (nombre == nuevoValor)
                    return;

                nombre =
                    nuevoValor;

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
                string nuevoValor =
                    value ??
                    string.Empty;

                if (descripcion == nuevoValor)
                    return;

                descripcion =
                    nuevoValor;

                OnPropertyChanged();

                if (descripcion.Length <= 150)
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

                errorNombre =
                    value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorNombre));
            }
        }

        public bool TieneErrorNombre =>
            !string.IsNullOrWhiteSpace(
                ErrorNombre);

        public string ErrorDescripcion
        {
            get => errorDescripcion;
            private set
            {
                if (errorDescripcion == value)
                    return;

                errorDescripcion =
                    value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TieneErrorDescripcion));
            }
        }

        public bool TieneErrorDescripcion =>
            !string.IsNullOrWhiteSpace(
                ErrorDescripcion);

        public bool IsReadOnly =>
            Mode ==
            FormMode.FormModeSelect.View;

        public bool CanSave =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    CanAdd,

                FormMode.FormModeSelect.Edit =>
                    CanEdit,

                _ =>
                    false
            };

        public bool ShowSaveButton =>
            CanSave;

        public bool MostrarBotonCancelar =>
            !IsReadOnly;

        public string Title =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    "Crear tipo de cultivo",

                FormMode.FormModeSelect.Edit =>
                    "Editar tipo de cultivo",

                FormMode.FormModeSelect.View =>
                    "Detalles del tipo de cultivo",

                _ =>
                    "Tipo de cultivo"
            };

        public string Subtitulo =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    "Registre un cultivo disponible para análisis y parámetros nutricionales.",

                FormMode.FormModeSelect.Edit =>
                    "Actualice la información del cultivo seleccionado.",

                FormMode.FormModeSelect.View =>
                    "Consulte la información registrada.",

                _ =>
                    string.Empty
            };

        public void ActualizarPermisos()
        {
            LoadPagePermissions(
                "tipoCultivoPage");

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
            if (!CanSave ||
                IsBusy)
            {
                return;
            }

            if (!ValidarCampos())
            {
                await MostrarAdvertenciaAsync(
                    "Revise los campos marcados antes de continuar.");

                return;
            }

            if (!await ValidarInternetAsync())
                return;

            if (Mode ==
                    FormMode.FormModeSelect.Edit &&
                !HayCambios())
            {
                await MostrarInformacionAsync(
                    "No hay cambios para guardar.");

                return;
            }

            bool confirmar =
                Mode ==
                FormMode.FormModeSelect.Create
                    ? await ConfirmarGuardadoAsync(
                        "el tipo de cultivo")
                    : await ConfirmarActualizacionAsync(
                        "el tipo de cultivo");

            if (!confirmar)
                return;

            guardadoCts?.Cancel();
            guardadoCts?.Dispose();

            guardadoCts =
                new CancellationTokenSource();

            try
            {
                IsBusy = true;
                RefrescarComandos();

                Item.NombreTipoCultivo =
                    Nombre
                        .ReplaceLineEndings(" ")
                        .Trim()
                        .ToUpperInvariant();

                Item.DescripcionTipoCultivo =
                    Descripcion
                        .ReplaceLineEndings(" ")
                        .Trim();

                ApiResult<bool> resultado =
                    Mode ==
                    FormMode.FormModeSelect.Create
                        ? await apiService
                            .CreateAsync(
                                Item,
                                guardadoCts.Token)
                        : await apiService
                            .UpdateAsync(
                                Item,
                                guardadoCts.Token);

                if (!resultado.Success ||
                    resultado.Data != true)
                {
                    await MostrarErrorAsync(
                        resultado.Message);

                    return;
                }

                TipoCultivoListadoEstadoService
                    .MarcarCambio();

                await RegresarAlListadoAsync();

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(
                        resultado.Message)
                            ? "Tipo de cultivo guardado correctamente."
                            : resultado.Message);
            }
            catch (OperationCanceledException)
            {
                // La página se cerró durante el guardado.
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "guardar el tipo de cultivo",
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

            if (!IsReadOnly &&
                HayCambios())
            {
                bool confirmar =
                    await ConfirmarSalidaSinGuardarAsync();

                if (!confirmar)
                    return;
            }

            await RegresarAlListadoAsync();
        }

        private bool ValidarCampos()
        {
            LimpiarErrores();

            Nombre =
                Nombre
                    .ReplaceLineEndings(" ")
                    .Trim();

            Descripcion =
                Descripcion.Trim();

            if (string.IsNullOrWhiteSpace(
                    Nombre))
            {
                ErrorNombre =
                    "Ingrese el nombre del tipo de cultivo.";
            }
            else if (Nombre.Length > 80)
            {
                ErrorNombre =
                    "El nombre no puede superar 80 caracteres.";
            }

            if (Descripcion.Length > 150)
            {
                ErrorDescripcion =
                    "La descripción no puede superar 150 caracteres.";
            }

            return
                !TieneErrorNombre &&
                !TieneErrorDescripcion;
        }

        private bool HayCambios()
        {
            string nombreActual =
                Nombre
                    .ReplaceLineEndings(" ")
                    .Trim();

            string descripcionActual =
                Descripcion.Trim();

            if (Mode ==
                FormMode.FormModeSelect.Create)
            {
                return
                    !string.IsNullOrWhiteSpace(
                        nombreActual) ||
                    !string.IsNullOrWhiteSpace(
                        descripcionActual);
            }

            return
                !string.Equals(
                    nombreActual,
                    nombreOriginal,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    descripcionActual,
                    descripcionOriginal,
                    StringComparison.Ordinal);
        }

        private Task RegresarAlListadoAsync() =>
            GoToAsyncParameters(
                AppRoutes.TiposCultivo);

        private void LimpiarErrores()
        {
            ErrorNombre =
                string.Empty;

            ErrorDescripcion =
                string.Empty;
        }

        private void RefrescarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
        }
    }
}
