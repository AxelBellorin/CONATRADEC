using CONATRADEC.Models;
using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    public sealed class AlbumDetalleViewModel : GlobalService
    {
        private readonly AlbumBotanicoApiService apiService = new();
        private int id;
        private AlbumDetalleResponse? detalle;
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
                OnPropertyChanged(nameof(TieneDetalle));
                OnPropertyChanged(nameof(TieneFotos));
                OnPropertyChanged(nameof(SinFotos));
                OnPropertyChanged(nameof(PuedeAdministrarFotos));
            }
        }

        public bool TieneDetalle => Detalle != null;
        public bool TieneFotos => Detalle?.Fotos.Count > 0;
        public bool SinFotos => !TieneFotos;

        public bool PuedeAdministrarFotos =>
            CanAdd || CanEdit || CanDelete;

        public Command RegresarCommand { get; }
        public Command EditarCommand { get; }
        public Command AdministrarFotosCommand { get; }
        public Command CambiarEstadoCommand { get; }
        public Command<AlbumFotoResponse> AbrirFotoCommand { get; }

        public AlbumDetalleViewModel()
        {
            /*
             * Regresa a la instancia del álbum que está debajo
             * en la pila de navegación.
             */
            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(
                    AppRoutes.Regresar));

            EditarCommand = new Command(
                async () => await EditarAsync());

            AdministrarFotosCommand = new Command(
                async () => await AdministrarFotosAsync());

            CambiarEstadoCommand = new Command(
                async () => await CambiarEstadoAsync());

            AbrirFotoCommand = new Command<AlbumFotoResponse>(
                async foto => await AbrirFotoAsync(foto));
        }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("albumFotosPage");
            OnPropertyChanged(nameof(PuedeAdministrarFotos));
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
                var result = await apiService.GetDetalleAsync(
                    Id,
                    CanEdit || CanDelete);

                if (!result.Success || result.Data == null)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                Detalle = result.Data;
            }
            finally
            {
                cargando = false;

                if (showIndicator)
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

        private async Task EditarAsync()
        {
            if (!CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permisos para editar este registro.");
                return;
            }

            await GoToAsyncParameters(
                AppRoutes.AlbumRegistroFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Edit,
                    ["RegistroId"] = Id,
                    ["CategoriaId"] =
                        Detalle?.CategoriaAlbumBotanicoId ?? 0
                });
        }

        private async Task AdministrarFotosAsync()
        {
            if (!PuedeAdministrarFotos)
            {
                await MostrarToastAsync(
                    "No tiene permisos para administrar fotografías.");
                return;
            }

            await GoToAsyncParameters(
                AppRoutes.AlbumFotosAdministrar,
                new Dictionary<string, object>
                {
                    ["RegistroId"] = Id
                });
        }

        private async Task CambiarEstadoAsync()
        {
            if (Detalle == null || IsBusy)
                return;

            if (!CanDelete)
            {
                await MostrarToastAsync(
                    "No tiene permisos para cambiar el estado.");
                return;
            }

            bool nuevoEstado = !Detalle.Activo;
            Page? page = Application.Current?.MainPage;

            if (page == null)
                return;

            bool confirm = await page.DisplayAlert(
                nuevoEstado
                    ? "Activar registro"
                    : "Desactivar registro",
                $"¿Desea {(nuevoEstado ? "activar" : "desactivar")} " +
                $"'{Detalle.Titulo}'?",
                "Sí",
                "No");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                var result =
                    await apiService.CambiarEstadoRegistroAsync(
                        Id,
                        nuevoEstado);

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
    }
}
