using CONATRADEC.Models;
using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    public sealed class TipoAnalisisSueloFormViewModel : GlobalService
    {
        private readonly TipoAnalisisSueloApiService apiService;
        private CancellationTokenSource? guardadoCts;

        private TipoAnalisisSueloRequest item = new();
        private FormMode.FormModeSelect mode;
        private string nombre = string.Empty;
        private string descripcion = string.Empty;
        private string nombreOriginal = string.Empty;
        private string descripcionOriginal = string.Empty;
        private string errorNombre = string.Empty;
        private string errorDescripcion = string.Empty;

        public TipoAnalisisSueloFormViewModel()
            : this(new TipoAnalisisSueloApiService())
        {
        }

        public TipoAnalisisSueloFormViewModel(
            TipoAnalisisSueloApiService apiService)
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

        public TipoAnalisisSueloRequest Item
        {
            get => item;
            set
            {
                item =
                    value ??
                    new TipoAnalisisSueloRequest();

                Nombre =
                    item.NombreTipoAnalisisSuelo ??
                    string.Empty;

                Descripcion =
                    item.DescripcionTipoAnalisisSuelo ??
                    string.Empty;

                nombreOriginal =
                    Nombre.Trim();

                descripcionOriginal =
                    Descripcion.Trim();

                LimpiarErrores();
                OnPropertyChanged();
                OnPropertyChanged(nameof(CodigoInterno));
                OnPropertyChanged(nameof(MostrarCodigoInterno));
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

                if (!string.IsNullOrWhiteSpace(descripcion) &&
                    descripcion.Length <= 200)
                {
                    ErrorDescripcion =
                        string.Empty;
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

        public string CodigoInterno =>
            Item.CodigoTipoAnalisisSuelo;

        public bool MostrarCodigoInterno =>
            !string.IsNullOrWhiteSpace(
                CodigoInterno);

        public string Title =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    "Crear tipo de análisis de suelo",

                FormMode.FormModeSelect.Edit =>
                    "Editar tipo de análisis de suelo",

                FormMode.FormModeSelect.View =>
                    "Detalles del tipo de análisis de suelo",

                _ =>
                    "Tipo de análisis de suelo"
            };

        public string Subtitulo =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    "Registre una modalidad de análisis disponible para el procesamiento de resultados.",

                FormMode.FormModeSelect.Edit =>
                    "Actualice la información del tipo de análisis seleccionado.",

                FormMode.FormModeSelect.View =>
                    "Consulte la información registrada.",

                _ =>
                    string.Empty
            };

        public void ActualizarPermisos()
        {
            LoadPagePermissions(
                "tipoAnalisisSueloPage");

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
                        "el tipo de análisis de suelo")
                    : await ConfirmarActualizacionAsync(
                        "el tipo de análisis de suelo");

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

                Item.NombreTipoAnalisisSuelo =
                    Nombre
                        .ReplaceLineEndings(" ")
                        .Trim()
                        .ToUpperInvariant();

                Item.DescripcionTipoAnalisisSuelo =
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

                TipoAnalisisSueloListadoEstadoService
                    .MarcarCambio();

                await RegresarAlListadoAsync();

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(
                        resultado.Message)
                            ? "Tipo de análisis de suelo guardado correctamente."
                            : resultado.Message);
            }
            catch (OperationCanceledException)
            {
                // La página se cerró durante el guardado.
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "guardar el tipo de análisis de suelo",
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
                    "Ingrese el nombre del tipo de análisis.";
            }
            else if (Nombre.Length > 100)
            {
                ErrorNombre =
                    "El nombre no puede superar 100 caracteres.";
            }

            if (string.IsNullOrWhiteSpace(
                    Descripcion))
            {
                ErrorDescripcion =
                    "Ingrese la descripción del tipo de análisis.";
            }
            else if (Descripcion.Length > 200)
            {
                ErrorDescripcion =
                    "La descripción no puede superar 200 caracteres.";
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
                AppRoutes.TiposAnalisisSuelo);

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
