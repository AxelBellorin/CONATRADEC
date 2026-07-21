using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    public sealed class AlbumFotosAdminViewModel :
        GlobalService
    {
        private readonly AlbumBotanicoApiService apiService = new();
        private int id;
        private AlbumDetalleResponse? detalle;
        private FileResult? archivoSeleccionado;
        private string descripcionNueva = string.Empty;
        private bool esPortadaNueva;
        private int ordenNuevo = 1;
        private bool cargando;

        public int Id
        {
            get => id;
            set
            {
                id = value;
                OnPropertyChanged();
            }
        }

        public AlbumDetalleResponse? Detalle
        {
            get => detalle;
            private set
            {
                detalle = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneFotos));
                OnPropertyChanged(nameof(SinFotos));
                OnPropertyChanged(nameof(PuedeSubir));
            }
        }

        public string DescripcionNueva
        {
            get => descripcionNueva;
            set
            {
                descripcionNueva = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public bool EsPortadaNueva
        {
            get => esPortadaNueva;
            set
            {
                esPortadaNueva = value;
                OnPropertyChanged();
            }
        }

        public int OrdenNuevo
        {
            get => ordenNuevo;
            set
            {
                ordenNuevo = value;
                OnPropertyChanged();
            }
        }

        public string ArchivoSeleccionadoTexto =>
            archivoSeleccionado == null
                ? "Seleccione una imagen para cargar."
                : archivoSeleccionado.FileName;

        public bool TieneArchivoSeleccionado =>
            archivoSeleccionado != null;

        public bool TieneFotos =>
            Detalle?.Fotos.Count > 0;

        public bool SinFotos => !TieneFotos;

        public bool PuedeSubir =>
            CanAdd && Detalle?.Activo == true;

        public Command RegresarCommand { get; }
        public Command SeleccionarArchivoCommand { get; }
        public Command SubirFotoCommand { get; }
        public Command<AlbumFotoResponse> GuardarFotoCommand { get; }
        public Command<AlbumFotoResponse> EstablecerPortadaCommand { get; }
        public Command<AlbumFotoResponse> EliminarFotoCommand { get; }
        public Command<AlbumFotoResponse> AbrirFotoCommand { get; }

        public AlbumFotosAdminViewModel()
        {
            RegresarCommand =
                new Command(async () =>
                    await RegresarAsync());

            SeleccionarArchivoCommand =
                new Command(async () =>
                    await SeleccionarArchivoAsync());

            SubirFotoCommand =
                new Command(async () =>
                    await SubirFotoAsync());

            GuardarFotoCommand =
                new Command<AlbumFotoResponse>(
                    async foto =>
                        await GuardarFotoAsync(foto));

            EstablecerPortadaCommand =
                new Command<AlbumFotoResponse>(
                    async foto =>
                        await EstablecerPortadaAsync(foto));

            EliminarFotoCommand =
                new Command<AlbumFotoResponse>(
                    async foto =>
                        await EliminarFotoAsync(foto));

            AbrirFotoCommand =
                new Command<AlbumFotoResponse>(
                    async foto =>
                        await AbrirFotoAsync(foto));
        }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("albumFotosPage");
            OnPropertyChanged(nameof(PuedeSubir));
        }

        public async Task LoadAsync(bool showIndicator)
        {
            if (Id <= 0 || cargando)
                return;

            cargando = true;

            if (showIndicator)
                IsBusy = true;

            try
            {
                var result =
                    await apiService.GetDetalleAsync(
                        Id,
                        true);

                if (!result.Success ||
                    result.Data == null)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                Detalle = result.Data;
                OrdenNuevo =
                    Detalle.Fotos.Count == 0
                        ? 1
                        : Detalle.Fotos.Max(x => x.Orden) + 1;
            }
            finally
            {
                cargando = false;

                if (showIndicator)
                    IsBusy = false;
            }
        }

        private async Task SeleccionarArchivoAsync()
        {
            if (!CanAdd)
            {
                await MostrarToastAsync(
                    "No tiene permisos para agregar fotografías.");
                return;
            }

            FileResult? archivo =
                await FilePicker.Default.PickAsync(
                    new PickOptions
                    {
                        PickerTitle =
                            "Seleccione una fotografía",
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
            OnPropertyChanged(
                nameof(TieneArchivoSeleccionado));
        }

        private async Task SubirFotoAsync()
        {
            if (IsBusy)
                return;

            if (!PuedeSubir)
            {
                await MostrarToastAsync(
                    Detalle?.Activo == false
                        ? "Active el registro antes de agregar fotografías."
                        : "No tiene permisos para agregar fotografías.");
                return;
            }

            if (archivoSeleccionado == null)
            {
                await MostrarToastAsync(
                    "Seleccione una fotografía.");
                return;
            }

            if (DescripcionNueva.Trim().Length > 500)
            {
                await MostrarToastAsync(
                    "La descripción no puede superar los 500 caracteres.");
                return;
            }

            IsBusy = true;

            try
            {
                var result =
                    await apiService.SubirFotoAsync(
                        Id,
                        archivoSeleccionado,
                        DescripcionNueva,
                        EsPortadaNueva,
                        OrdenNuevo);

                if (!result.Success)
                {
                    Page? page =
                        Application.Current?.MainPage;

                    if (page != null)
                    {
                        await page.DisplayAlert(
                            "No fue posible",
                            result.Message,
                            "Aceptar");
                    }

                    return;
                }

                await MostrarToastAsync(result.Message);

                archivoSeleccionado = null;
                DescripcionNueva = string.Empty;
                EsPortadaNueva = false;

                OnPropertyChanged(
                    nameof(ArchivoSeleccionadoTexto));
                OnPropertyChanged(
                    nameof(TieneArchivoSeleccionado));

                await LoadAsync(false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task AbrirFotoAsync(
            AlbumFotoResponse? foto)
        {
            if (foto == null ||
                Detalle == null ||
                Detalle.Fotos.Count == 0)
            {
                return;
            }

            await GoToAsyncParameters(
                AppRoutes.AlbumFotoVisor,
                new Dictionary<string, object>
                {
                    ["Fotos"] = Detalle.Fotos,
                    ["FotoSeleccionadaId"] =
                        foto.AlbumBotanicoCafeFotoId,
                    ["TituloAlbum"] = Detalle.Titulo
                });
        }

        private async Task GuardarFotoAsync(
            AlbumFotoResponse? foto)
        {
            if (foto == null || IsBusy)
                return;

            if (!CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permisos para editar fotografías.");
                return;
            }

            if ((foto.DescripcionFoto?.Length ?? 0) > 500)
            {
                await MostrarToastAsync(
                    "La descripción no puede superar los 500 caracteres.");
                return;
            }

            IsBusy = true;

            try
            {
                var result =
                    await apiService
                        .ActualizarFotoAsync(foto);

                if (!result.Success)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                await MostrarToastAsync(result.Message);
                await LoadAsync(false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task EstablecerPortadaAsync(
            AlbumFotoResponse? foto)
        {
            if (foto == null || IsBusy)
                return;

            if (!CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permisos para cambiar la portada.");
                return;
            }

            if (foto.EsPortada)
            {
                await MostrarToastAsync(
                    "Esta fotografía ya es la portada.");
                return;
            }

            IsBusy = true;

            try
            {
                var result =
                    await apiService
                        .EstablecerPortadaAsync(
                            foto.AlbumBotanicoCafeFotoId);

                if (!result.Success)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                await MostrarToastAsync(result.Message);
                await LoadAsync(false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task EliminarFotoAsync(
            AlbumFotoResponse? foto)
        {
            if (foto == null || IsBusy)
                return;

            if (!CanDelete)
            {
                await MostrarToastAsync(
                    "No tiene permisos para eliminar fotografías.");
                return;
            }

            Page? page = Application.Current?.MainPage;

            if (page == null)
                return;

            bool confirm = await page.DisplayAlert(
                "Eliminar fotografía",
                "¿Desea eliminar esta fotografía del álbum?",
                "Eliminar",
                "Cancelar");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                var result =
                    await apiService.EliminarFotoAsync(
                        foto.AlbumBotanicoCafeFotoId);

                if (!result.Success)
                {
                    await page.DisplayAlert(
                        "No fue posible",
                        result.Message,
                        "Aceptar");
                    return;
                }

                await MostrarToastAsync(result.Message);
                await LoadAsync(false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task RegresarAsync() =>
            GoToAsyncParameters(
                AppRoutes.AlbumDetalle,
                new Dictionary<string, object>
                {
                    ["RegistroId"] = Id
                });
    }
}
