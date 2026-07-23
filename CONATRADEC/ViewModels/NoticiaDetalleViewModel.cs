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

        public NoticiaDetalleViewModel()
        {
            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar),
                () => !IsBusy);

            AbrirEnlaceCommand = new Command(
                async () => await AbrirEnlaceAsync(),
                () => !IsBusy && Publicacion?.TieneEnlace == true);

            EditarCommand = new Command(
                async () => await EditarAsync(),
                () => !IsBusy && CanEdit && Publicacion != null);
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

            publicacionId = id;
            await CargarAsync();
        }

        private async Task CargarAsync()
        {
            if (!CanView || publicacionId <= 0 || IsBusy)
                return;

            if (!await ValidarInternetAsync())
                return;

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;
                Publicacion = null;

                ApiResult<PublicacionDetalleResponse> result =
                    await apiService.GetDetalleAsync(publicacionId);

                if (!result.Success || result.Data == null)
                {
                    Mensaje = result.Message;
                    return;
                }

                Publicacion = result.Data;
            }
            catch (Exception ex)
            {
                Mensaje = "No fue posible cargar la publicación.";
                await MostrarErrorInesperadoAsync(
                    "cargar la publicación",
                    ex);
            }
            finally
            {
                IsBusy = false;
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
