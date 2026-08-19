using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.IO;
using System.Text.RegularExpressions;

namespace CONATRADEC.ViewModels
{
    public sealed class AlbumFotosAdminViewModel : GlobalService
    {
        private readonly AlbumBotanicoApiService apiService = new();
        private int id;
        private AlbumDetalleResponse? detalle;
        private FileResult? archivoSeleccionado;
        private ImageSource? vistaPreviaImagen;
        private string descripcionNueva = string.Empty;
        private bool esPortadaNueva;
        private int ordenNuevo = 1;
        private bool cargando;
        private long versionCargada = -1;

        public int Id
        {
            get => id;
            set
            {
                if (id == value)
                    return;

                id = value;
                Detalle = null;
                versionCargada = -1;
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

        public ImageSource? ImagenSeleccionadaPreview => vistaPreviaImagen;
        public bool TieneArchivoSeleccionado => archivoSeleccionado != null;
        public bool SinArchivoSeleccionado => !TieneArchivoSeleccionado;
        public bool TieneFotos => Detalle?.Fotos.Count > 0;
        public bool SinFotos => !TieneFotos;

        public bool PuedeSubir =>
            CanView && CanAdd && Detalle?.Activo == true;

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
                new Command(async () => await RegresarAsync());
            SeleccionarArchivoCommand =
                new Command(async () => await SeleccionarArchivoAsync());
            SubirFotoCommand =
                new Command(async () => await SubirFotoAsync());
            GuardarFotoCommand =
                new Command<AlbumFotoResponse>(
                    async foto => await GuardarFotoAsync(foto));
            EstablecerPortadaCommand =
                new Command<AlbumFotoResponse>(
                    async foto => await EstablecerPortadaAsync(foto));
            EliminarFotoCommand =
                new Command<AlbumFotoResponse>(
                    async foto => await EliminarFotoAsync(foto));
            AbrirFotoCommand =
                new Command<AlbumFotoResponse>(
                    async foto => await AbrirFotoAsync(foto));
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

            /*
             * Abrir el visor no modifica datos. Al regresar se reutiliza el
             * detalle ya cargado; una mutación incrementa la versión y obliga
             * a obtener una única copia fresca del servidor.
             */
            if (Detalle != null &&
                versionCargada == AlbumBotanicoRefreshState.VersionActual)
            {
                return;
            }

            cargando = true;

            if (showIndicator)
                IsBusy = true;

            try
            {
                ApiResult<AlbumDetalleResponse> result =
                    await apiService.GetDetalleAsync(Id, true);

                if (!result.Success || result.Data == null)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                Detalle = result.Data;
                OrdenNuevo = Detalle.Fotos.Count == 0
                    ? 1
                    : Detalle.Fotos.Max(x => x.Orden) + 1;
                versionCargada = AlbumBotanicoRefreshState.VersionActual;
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
            if (!CanView || !CanAdd)
            {
                await MostrarToastAsync(
                    "No tiene permisos para agregar fotografías.");
                return;
            }

            if (IsBusy)
                return;

            try
            {
                FileResult? archivo = await FilePicker.Default.PickAsync(
                    new PickOptions
                    {
                        PickerTitle = "Seleccione una fotografía",
                        FileTypes = FilePickerFileType.Images
                    });

                if (archivo == null)
                    return;

                string extension = Path.GetExtension(archivo.FileName)
                    .ToLowerInvariant();

                if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                {
                    await MostrarToastAsync(
                        "Seleccione una imagen JPG, JPEG, PNG o WEBP.");
                    return;
                }

                const long tamanioMaximo = 8L * 1024 * 1024;

                await using Stream stream = await archivo.OpenReadAsync();

                if (stream.CanSeek && stream.Length > tamanioMaximo)
                {
                    await MostrarToastAsync(
                        "La imagen no puede superar los 8 MB.");
                    return;
                }

                using var memoria = new MemoryStream();
                await stream.CopyToAsync(memoria);

                if (memoria.Length > tamanioMaximo)
                {
                    await MostrarToastAsync(
                        "La imagen no puede superar los 8 MB.");
                    return;
                }

                byte[] bytesImagen = memoria.ToArray();
                archivoSeleccionado = archivo;
                vistaPreviaImagen = ImageSource.FromStream(
                    () => new MemoryStream(bytesImagen, writable: false));

                NotificarArchivoSeleccionado();
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "seleccionar la fotografía",
                    ex);
            }
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
                await MostrarToastAsync("Seleccione una fotografía.");
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
                ApiResult<FotoAlbumCreadaData> result =
                    await apiService.SubirFotoAsync(
                        Id,
                        archivoSeleccionado,
                        DescripcionNueva,
                        EsPortadaNueva,
                        OrdenNuevo);

                if (!result.Success)
                {
                    Page? page = Application.Current?.MainPage;
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
                AlbumBotanicoRefreshState.MarcarCambio();
                LimpiarArchivoSeleccionado();
                DescripcionNueva = string.Empty;
                EsPortadaNueva = false;
                await LoadAsync(false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void LimpiarArchivoSeleccionado()
        {
            archivoSeleccionado = null;
            vistaPreviaImagen = null;
            NotificarArchivoSeleccionado();
        }

        private void NotificarArchivoSeleccionado()
        {
            OnPropertyChanged(nameof(ArchivoSeleccionadoTexto));
            OnPropertyChanged(nameof(TieneArchivoSeleccionado));
            OnPropertyChanged(nameof(SinArchivoSeleccionado));
            OnPropertyChanged(nameof(ImagenSeleccionadaPreview));
        }

        private async Task AbrirFotoAsync(AlbumFotoResponse? foto)
        {
            if (foto == null || Detalle == null || Detalle.Fotos.Count == 0)
                return;

            await GoToAsyncParameters(
                AppRoutes.AlbumFotoVisor,
                new Dictionary<string, object>
                {
                    ["Fotos"] = Detalle.Fotos,
                    ["FotoSeleccionadaId"] = foto.AlbumBotanicoCafeFotoId,
                    ["TituloAlbum"] = Detalle.Titulo
                });
        }

        private async Task GuardarFotoAsync(AlbumFotoResponse? foto)
        {
            if (foto == null || IsBusy)
                return;

            if (!CanView || !CanEdit)
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
                ApiResult<bool> result = await apiService.ActualizarFotoAsync(foto);

                if (!result.Success)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                await MostrarToastAsync(result.Message);
                AlbumBotanicoRefreshState.MarcarCambio();
                await LoadAsync(false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task EstablecerPortadaAsync(AlbumFotoResponse? foto)
        {
            if (foto == null || IsBusy)
                return;

            if (!CanView || !CanEdit)
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
                ApiResult<bool> result = await apiService.EstablecerPortadaAsync(
                    foto.AlbumBotanicoCafeFotoId);

                if (!result.Success)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                await MostrarToastAsync(result.Message);
                AlbumBotanicoRefreshState.MarcarCambio();
                await LoadAsync(false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task EliminarFotoAsync(AlbumFotoResponse? foto)
        {
            if (foto == null || IsBusy)
                return;

            if (!CanView || !CanDelete)
            {
                await MostrarToastAsync(
                    "No tiene permisos para desactivar fotografías.");
                return;
            }

            Page? page = Application.Current?.MainPage;
            if (page == null)
                return;

            bool confirm = await page.DisplayAlert(
                "Desactivar fotografía",
                "¿Desea desactivar esta fotografía del Álbum Botánico?",
                "Desactivar",
                "Cancelar");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                ApiResult<bool> result = await apiService.EliminarFotoAsync(
                    foto.AlbumBotanicoCafeFotoId);

                if (!result.Success)
                {
                    if (TryParsearBloqueoInspeccion(
                            result.Message,
                            out int inspeccionId,
                            out string mensajeVisible))
                    {
                        INavigation navegacion =
                            Shell.Current?.Navigation ?? page.Navigation;

                        var dialogo = new FotografiaVinculadaInspeccionPage(
                            inspeccionId,
                            mensajeVisible);

                        await navegacion.PushModalAsync(dialogo);
                        bool irInspeccion = await dialogo.ResultadoTask;

                        if (irInspeccion)
                        {
                            await GoToAsyncParameters(
                                DiagnosticoIARoutes.CrearRutaResultado(
                                    inspeccionId,
                                    DiagnosticoIARoutes.ModoAprobador));
                        }

                        return;
                    }

                    await page.DisplayAlert(
                        "No fue posible",
                        result.Message,
                        "Aceptar");
                    return;
                }

                await MostrarToastAsync(result.Message);
                AlbumBotanicoRefreshState.MarcarCambio();
                await LoadAsync(false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static bool TryParsearBloqueoInspeccion(
            string? mensaje,
            out int inspeccionId,
            out string mensajeVisible)
        {
            inspeccionId = 0;
            mensajeVisible = mensaje?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(mensaje))
                return false;

            Match match = Regex.Match(
                mensaje,
                @"^\[\[INSPECCION_FITOSANITARIA:(\d+)\]\]\s*",
                RegexOptions.CultureInvariant);

            if (!match.Success ||
                !int.TryParse(match.Groups[1].Value, out inspeccionId) ||
                inspeccionId <= 0)
            {
                inspeccionId = 0;
                return false;
            }

            mensajeVisible = mensaje[match.Length..].Trim();
            return true;
        }

        private Task RegresarAsync() =>
            GoToAsyncParameters(AppRoutes.Regresar);
    }
}
