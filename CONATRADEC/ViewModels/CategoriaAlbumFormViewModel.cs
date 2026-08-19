using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Storage;

namespace CONATRADEC.ViewModels
{
    public sealed class CategoriaAlbumFormViewModel : GlobalService
    {
        private readonly AlbumBotanicoApiService apiService = new();
        private CategoriaAlbumBotanicoRequest item = new();
        private FormMode.FormModeSelect mode;
        private string nombre = string.Empty;
        private string descripcion = string.Empty;
        private string nombreInicial = string.Empty;
        private string descripcionInicial = string.Empty;
        private string errorNombre = string.Empty;
        private string errorDescripcion = string.Empty;
        private FileResult? archivoSeleccionado;

        public CategoriaAlbumBotanicoRequest Item
        {
            get => item;
            set
            {
                item = value ?? new CategoriaAlbumBotanicoRequest();

                Nombre = item.NombreCategoria ?? string.Empty;
                Descripcion = item.Descripcion ?? string.Empty;

                /*
                 * Se conserva una fotografía del estado inicial para que
                 * Cancelar sólo pregunte cuando el usuario realmente cambió
                 * información y no simplemente por estar editando un registro.
                 */
                nombreInicial = Nombre.Trim();
                descripcionInicial = Descripcion.Trim();

                LimpiarErrores();
                OnPropertyChanged();
                OnPropertyChanged(nameof(ImagenActual));
                OnPropertyChanged(nameof(TieneImagenActual));
            }
        }

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TituloPagina));
                OnPropertyChanged(nameof(PuedeGuardar));
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

                if (!string.IsNullOrWhiteSpace(nombre) &&
                    nombre.Trim().Length <= 100)
                {
                    ErrorNombre = string.Empty;
                }
            }
        }

        public string Descripcion
        {
            get => descripcion;
            set
            {
                descripcion = value ?? string.Empty;
                OnPropertyChanged();

                if (descripcion.Trim().Length <= 500)
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

        public string? ImagenActual => Item.RutaImagenPortadaActual;

        public bool TieneImagenActual =>
            !string.IsNullOrWhiteSpace(ImagenActual);

        public string ArchivoSeleccionadoTexto =>
            archivoSeleccionado == null
                ? "No se ha seleccionado una nueva imagen."
                : archivoSeleccionado.FileName;

        public string TituloPagina =>
            Mode == FormMode.FormModeSelect.Create
                ? "Nueva categoría"
                : "Editar categoría";

        public bool PuedeGuardar =>
            CanView &&
            (Mode == FormMode.FormModeSelect.Create
                ? CanAdd
                : Mode == FormMode.FormModeSelect.Edit && CanEdit);

        public Command SeleccionarPortadaCommand { get; }
        public Command GuardarCommand { get; }
        public Command CancelarCommand { get; }

        public CategoriaAlbumFormViewModel()
        {
            SeleccionarPortadaCommand =
                new Command(
                    async () => await SeleccionarPortadaAsync(),
                    () => !IsBusy && PuedeGuardar);

            GuardarCommand =
                new Command(
                    async () => await GuardarAsync(),
                    () => !IsBusy && PuedeGuardar);

            CancelarCommand =
                new Command(
                    async () => await CancelarAsync(),
                    () => !IsBusy);
        }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("albumFotosPage");
            OnPropertyChanged(nameof(PuedeGuardar));
            RefrescarComandos();
        }

        private async Task SeleccionarPortadaAsync()
        {
            if (IsBusy || !PuedeGuardar)
                return;

            try
            {
                FileResult? archivo = await FilePicker.Default.PickAsync(
                    new PickOptions
                    {
                        PickerTitle = "Seleccione la portada de la categoría",
                        FileTypes = FilePickerFileType.Images
                    });

                if (archivo == null)
                    return;

                string extension = Path.GetExtension(archivo.FileName)
                    .ToLowerInvariant();

                if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                {
                    await MostrarAdvertenciaAsync(
                        "Seleccione una imagen JPG, JPEG, PNG o WEBP.");
                    return;
                }

                const long tamanioMaximo = 8L * 1024 * 1024;
                await using Stream stream = await archivo.OpenReadAsync();

                if (stream.CanSeek && stream.Length > tamanioMaximo)
                {
                    await MostrarAdvertenciaAsync(
                        "La imagen no puede superar los 8 MB.");
                    return;
                }

                archivoSeleccionado = archivo;
                OnPropertyChanged(nameof(ArchivoSeleccionadoTexto));
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "seleccionar la portada de la categoría",
                    ex);
            }
        }

        private async Task GuardarAsync()
        {
            if (IsBusy || !PuedeGuardar)
                return;

            if (!ValidarCampos())
            {
                await MostrarAdvertenciaAsync(
                    "Revise los campos marcados antes de continuar.");
                return;
            }

            bool confirm = Mode == FormMode.FormModeSelect.Create
                ? await ConfirmarGuardadoAsync("la categoría")
                : await ConfirmarActualizacionAsync("la categoría");

            if (!confirm)
                return;

            Item.NombreCategoria = Nombre.Trim();
            Item.Descripcion = string.IsNullOrWhiteSpace(Descripcion)
                ? null
                : Descripcion.Trim();

            IsBusy = true;
            RefrescarComandos();

            try
            {
                int categoriaId;
                string mensajePrincipal;

                if (Mode == FormMode.FormModeSelect.Create)
                {
                    ApiResult<CategoriaCreadaData> result =
                        await apiService.CrearCategoriaAsync(Item);

                    if (!result.Success || result.Data == null)
                    {
                        await MostrarErrorAsync(result.Message);
                        return;
                    }

                    categoriaId = result.Data.CategoriaAlbumBotanicoId;

                    /*
                     * La categoría ya existe desde este momento. Si la portada
                     * falla, no se vuelve a insertar la categoría al reintentar.
                     */
                    Item.CategoriaAlbumBotanicoId = categoriaId;
                    Mode = FormMode.FormModeSelect.Edit;

                    mensajePrincipal = string.IsNullOrWhiteSpace(result.Message)
                        ? "Categoría guardada correctamente."
                        : result.Message;
                }
                else
                {
                    ApiResult<bool> result =
                        await apiService.ActualizarCategoriaAsync(Item);

                    if (!result.Success)
                    {
                        await MostrarErrorAsync(result.Message);
                        return;
                    }

                    categoriaId = Item.CategoriaAlbumBotanicoId;
                    mensajePrincipal = string.IsNullOrWhiteSpace(result.Message)
                        ? "Categoría actualizada correctamente."
                        : result.Message;
                }

                if (archivoSeleccionado != null)
                {
                    ApiResult<PortadaCategoriaData> imageResult =
                        await apiService.SubirPortadaCategoriaAsync(
                            categoriaId,
                            archivoSeleccionado);

                    if (!imageResult.Success)
                    {
                        await MostrarAdvertenciaAsync(
                            "La categoría se guardó, pero la portada no pudo cargarse. " +
                            imageResult.Message);
                    }
                    else if (!string.IsNullOrWhiteSpace(imageResult.Message))
                    {
                        await MostrarExitoAsync(imageResult.Message);
                    }
                }

                archivoSeleccionado = null;
                OnPropertyChanged(nameof(ArchivoSeleccionadoTexto));

                AlbumBotanicoRefreshState.MarcarCambio();

                await GoToAsyncParameters(AppRoutes.Regresar);

                await MostrarExitoAsync(mensajePrincipal);
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    Mode == FormMode.FormModeSelect.Create
                        ? "guardar la categoría"
                        : "actualizar la categoría",
                    ex);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task CancelarAsync()
        {
            if (IsBusy)
                return;

            bool hayCambios =
                !string.Equals(
                    Nombre.Trim(),
                    nombreInicial,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    Descripcion.Trim(),
                    descripcionInicial,
                    StringComparison.Ordinal) ||
                archivoSeleccionado != null;

            if (hayCambios)
            {
                bool salir = await ConfirmarSalidaSinGuardarAsync();
                if (!salir)
                    return;
            }

            await GoToAsyncParameters(AppRoutes.Regresar);
        }

        private bool ValidarCampos()
        {
            LimpiarErrores();

            Nombre = Nombre.Trim();
            Descripcion = Descripcion.Trim();

            if (string.IsNullOrWhiteSpace(Nombre))
            {
                ErrorNombre = "Ingrese el nombre de la categoría.";
            }
            else if (Nombre.Length > 100)
            {
                ErrorNombre =
                    "El nombre no puede superar los 100 caracteres.";
            }

            if (Descripcion.Length > 500)
            {
                ErrorDescripcion =
                    "La descripción no puede superar los 500 caracteres.";
            }

            return !TieneErrorNombre && !TieneErrorDescripcion;
        }

        private void LimpiarErrores()
        {
            ErrorNombre = string.Empty;
            ErrorDescripcion = string.Empty;
        }

        private void RefrescarComandos()
        {
            SeleccionarPortadaCommand.ChangeCanExecute();
            GuardarCommand.ChangeCanExecute();
            CancelarCommand.ChangeCanExecute();
        }
    }
}
