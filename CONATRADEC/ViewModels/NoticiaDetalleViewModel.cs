using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;

namespace CONATRADEC.ViewModels
{
    public sealed class NoticiaDetalleViewModel : GlobalService
    {
        private readonly PublicacionApiService apiService = new();
        private int publicacionId;
        private PublicacionDetalleResponse? publicacion;
        private string mensaje = string.Empty;
        private long versionAplicada = -1;
        private CancellationTokenSource? cargaCancellationTokenSource;

        public NoticiaDetalleViewModel()
        {
            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(
                    AppRoutes.Regresar),
                () => !IsBusy);

            AbrirEnlaceCommand = new Command(
                async () => await AbrirEnlaceAsync(),
                () => !IsBusy &&
                      Publicacion?.TieneEnlace == true);

            EditarCommand = new Command(
                async () => await EditarAsync(),
                () => !IsBusy &&
                      CanEdit &&
                      Publicacion != null);
        }

        public PublicacionDetalleResponse? Publicacion
        {
            get => publicacion;
            private set
            {
                publicacion = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TienePublicacion));
                AbrirEnlaceCommand.ChangeCanExecute();
                EditarCommand.ChangeCanExecute();
            }
        }

        public bool TienePublicacion => Publicacion != null;

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                mensaje = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(Mensaje);

        public Command RegresarCommand { get; }
        public Command AbrirEnlaceCommand { get; }
        public Command EditarCommand { get; }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("noticiasPage");
            EditarCommand.ChangeCanExecute();
        }

        public async Task InicializarAsync(int id)
        {
            if (id <= 0 || IsBusy)
                return;

            bool mismoRegistro =
                publicacionId == id &&
                Publicacion != null;

            publicacionId = id;

            if (mismoRegistro &&
                !PublicacionListadoEstadoService
                    .HayCambiosDesde(versionAplicada))
            {
                return;
            }

            await CargarAsync();
        }

        public void CancelarCarga()
        {
            cargaCancellationTokenSource?.Cancel();
        }

        private async Task CargarAsync()
        {
            if (!CanView || publicacionId <= 0 || IsBusy)
                return;

            cargaCancellationTokenSource?.Cancel();
            cargaCancellationTokenSource?.Dispose();

            var source = new CancellationTokenSource();
            cargaCancellationTokenSource = source;

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;

                ApiResult<PublicacionDetalleResponse> result =
                    await apiService.GetDetalleAsync(
                        publicacionId,
                        source.Token);

                if (source.IsCancellationRequested)
                    return;

                if (!result.Success || result.Data == null)
                {
                    if (!string.Equals(
                            result.Message,
                            "La operación fue cancelada.",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        Mensaje = result.Message;
                    }

                    return;
                }

                result.Data.ImagenPortadaUrl =
                    ImagenMiniaturaUrlService.Crear(
                        result.Data.ImagenPortadaUrl,
                        ancho: 1200,
                        alto: 900,
                        calidad: 76);

                Publicacion = result.Data;
                versionAplicada =
                    PublicacionListadoEstadoService.VersionActual;
            }
            catch (OperationCanceledException)
            {
                // Se canceló al navegar fuera del detalle.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested)
                {
                    Mensaje =
                        "No fue posible cargar la publicación.";

                    await MostrarErrorInesperadoAsync(
                        "cargar la publicación",
                        ex);
                }
            }
            finally
            {
                IsBusy = false;

                if (ReferenceEquals(
                        cargaCancellationTokenSource,
                        source))
                {
                    cargaCancellationTokenSource.Dispose();
                    cargaCancellationTokenSource = null;
                }
                else
                {
                    source.Dispose();
                }

                RegresarCommand.ChangeCanExecute();
                AbrirEnlaceCommand.ChangeCanExecute();
                EditarCommand.ChangeCanExecute();
            }
        }

        private async Task AbrirEnlaceAsync()
        {
            string? enlace = Publicacion?.EnlaceExterno;

            if (string.IsNullOrWhiteSpace(enlace))
                return;

            try
            {
                await Launcher.Default.OpenAsync(enlace);
            }
            catch
            {
                await MostrarAdvertenciaAsync(
                    "No fue posible abrir el enlace de la publicación.");
            }
        }

        private async Task EditarAsync()
        {
            if (!CanEdit || Publicacion == null)
                return;

            await GoToAsyncParameters(
                AppRoutes.PublicacionFormulario,
                new Dictionary<string, object>
                {
                    ["PublicacionId"] = Publicacion.PublicacionId
                });
        }
    }
}
