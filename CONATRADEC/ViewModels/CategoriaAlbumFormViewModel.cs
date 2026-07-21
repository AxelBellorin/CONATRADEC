using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    public sealed class CategoriaAlbumFormViewModel :
        GlobalService
    {
        private readonly AlbumBotanicoApiService apiService = new();
        private CategoriaAlbumBotanicoRequest item = new();
        private FormMode.FormModeSelect mode;
        private string nombre = string.Empty;
        private string descripcion = string.Empty;
        private FileResult? archivoSeleccionado;

        public CategoriaAlbumBotanicoRequest Item
        {
            get => item;
            set
            {
                item = value ??
                    new CategoriaAlbumBotanicoRequest();
                Nombre = item.NombreCategoria;
                Descripcion = item.Descripcion ?? string.Empty;
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
            }
        }

        public string Nombre
        {
            get => nombre;
            set
            {
                nombre = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Descripcion
        {
            get => descripcion;
            set
            {
                descripcion = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string? ImagenActual =>
            Item.RutaImagenPortadaActual;

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
            Mode != FormMode.FormModeSelect.View;

        public Command SeleccionarPortadaCommand { get; }
        public Command GuardarCommand { get; }
        public Command CancelarCommand { get; }

        public CategoriaAlbumFormViewModel()
        {
            SeleccionarPortadaCommand =
                new Command(async () =>
                    await SeleccionarPortadaAsync());

            GuardarCommand =
                new Command(async () => await GuardarAsync());

            CancelarCommand =
                new Command(async () => await CancelarAsync());
        }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("albumFotosPage");
        }

        private async Task SeleccionarPortadaAsync()
        {
            if (IsBusy)
                return;

            FileResult? archivo =
                await FilePicker.Default.PickAsync(
                    new PickOptions
                    {
                        PickerTitle =
                            "Seleccione la portada de la categoría",
                        FileTypes = FilePickerFileType.Images
                    });

            if (archivo == null)
                return;

            string extension =
                Path.GetExtension(archivo.FileName)
                    .ToLowerInvariant();

            if (extension is not
                (".jpg" or ".jpeg" or ".png" or ".webp"))
            {
                await MostrarToastAsync(
                    "Seleccione una imagen JPG, JPEG, PNG o WEBP.");
                return;
            }

            archivoSeleccionado = archivo;
            OnPropertyChanged(
                nameof(ArchivoSeleccionadoTexto));
        }

        private async Task GuardarAsync()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(Nombre))
            {
                await MostrarToastAsync(
                    "Ingrese el nombre de la categoría.");
                return;
            }

            if (Nombre.Trim().Length > 100)
            {
                await MostrarToastAsync(
                    "El nombre no puede superar los 100 caracteres.");
                return;
            }

            if (Descripcion.Trim().Length > 500)
            {
                await MostrarToastAsync(
                    "La descripción no puede superar los 500 caracteres.");
                return;
            }

            Page? page = Application.Current?.MainPage;

            if (page == null)
                return;

            bool confirm = await page.DisplayAlert(
                "Guardar categoría",
                "¿Desea guardar la información de la categoría?",
                "Guardar",
                "Cancelar");

            if (!confirm)
                return;

            Item.NombreCategoria = Nombre.Trim();
            Item.Descripcion = string.IsNullOrWhiteSpace(
                    Descripcion)
                ? null
                : Descripcion.Trim();

            IsBusy = true;

            try
            {
                int categoriaId;

                if (Mode == FormMode.FormModeSelect.Create)
                {
                    var result =
                        await apiService.CrearCategoriaAsync(Item);

                    if (!result.Success ||
                        result.Data == null)
                    {
                        await page.DisplayAlert(
                            "No fue posible",
                            result.Message,
                            "Aceptar");
                        return;
                    }

                    categoriaId =
                        result.Data.CategoriaAlbumBotanicoId;
                    await MostrarToastAsync(result.Message);
                }
                else
                {
                    var result =
                        await apiService
                            .ActualizarCategoriaAsync(Item);

                    if (!result.Success)
                    {
                        await page.DisplayAlert(
                            "No fue posible",
                            result.Message,
                            "Aceptar");
                        return;
                    }

                    categoriaId =
                        Item.CategoriaAlbumBotanicoId;
                    await MostrarToastAsync(result.Message);
                }

                if (archivoSeleccionado != null)
                {
                    var imageResult =
                        await apiService
                            .SubirPortadaCategoriaAsync(
                                categoriaId,
                                archivoSeleccionado);

                    if (!imageResult.Success)
                    {
                        await page.DisplayAlert(
                            "Categoría guardada",
                            "La categoría se guardó, pero la portada no pudo cargarse. " +
                            imageResult.Message,
                            "Aceptar");
                    }
                    else
                    {
                        await MostrarToastAsync(
                            imageResult.Message);
                    }
                }

                await GoToAsyncParameters(
                    AppRoutes.AlbumFotos);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CancelarAsync()
        {
            if (IsBusy)
                return;

            Page? page = Application.Current?.MainPage;

            if (page != null &&
                (!string.IsNullOrWhiteSpace(Nombre) ||
                 !string.IsNullOrWhiteSpace(Descripcion) ||
                 archivoSeleccionado != null))
            {
                bool salir = await page.DisplayAlert(
                    "Cancelar",
                    "¿Desea salir sin guardar los cambios?",
                    "Salir",
                    "Continuar editando");

                if (!salir)
                    return;
            }

            await GoToAsyncParameters(AppRoutes.AlbumFotos);
        }
    }
}
