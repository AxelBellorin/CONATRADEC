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
        private long versionCargada = -1;
        private CancellationTokenSource? cargaCts;

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
            CanView && (CanAdd || CanEdit || CanDelete);

        public Command RegresarCommand { get; }
        public Command EditarCommand { get; }
        public Command AdministrarFotosCommand { get; }
        public Command CambiarEstadoCommand { get; }
        public Command<AlbumFotoResponse> AbrirFotoCommand { get; }

        public AlbumDetalleViewModel()
        {
            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar));

            EditarCommand = new Command(async () => await EditarAsync());
            AdministrarFotosCommand =
                new Command(async () => await AdministrarFotosAsync());
            CambiarEstadoCommand =
                new Command(async () => await CambiarEstadoAsync());
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

            /*
             * Visor y otros subflujos de solo lectura no invalidan el detalle.
             * Después de una mutación AlbumBotanicoRefreshState cambia y esta
             * misma instancia vuelve a consultar una única vez.
             */
            if (Detalle != null &&
                versionCargada == AlbumBotanicoRefreshState.VersionActual)
            {
                return;
            }

            cargando = true;
            CancellationTokenSource cts = RenovarCarga();
            CancellationToken token = cts.Token;

            if (showIndicator)
                IsBusy = true;

            try
            {
                ApiResult<AlbumDetalleResponse> result =
                    await apiService.GetDetalleAsync(
                        Id,
                        incluirInactivos: false,
                        cancellationToken: token);

                if ((!result.Success || result.Data == null) &&
                    CanView &&
                    (CanEdit || CanDelete) &&
                    EstadoConexionService.Instance.HayInternet)
                {
                    result = await apiService.GetDetalleAsync(
                        Id,
                        incluirInactivos: true,
                        cancellationToken: token);
                }

                if (token.IsCancellationRequested)
                    return;

                if (!result.Success || result.Data == null)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                Detalle = result.Data;
                versionCargada = AlbumBotanicoRefreshState.VersionActual;
            }
            finally
            {
                cargando = false;

                if (showIndicator)
                    IsBusy = false;
            }
        }

        public void CancelarCarga()
        {
            CancellationTokenSource? anterior = cargaCts;
            cargaCts = null;

            if (anterior == null)
                return;

            try
            {
                anterior.Cancel();
            }
            catch
            {
            }
            finally
            {
                anterior.Dispose();
            }
        }

        private CancellationTokenSource RenovarCarga()
        {
            CancelarCarga();
            cargaCts = new CancellationTokenSource();
            return cargaCts;
        }

        private async Task AbrirFotoAsync(AlbumFotoResponse? foto)
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
                    ["FotoSeleccionadaId"] = foto.AlbumBotanicoCafeFotoId,
                    ["TituloAlbum"] = Detalle.Titulo
                });
        }

        private async Task EditarAsync()
        {
            if (!CanView || !CanEdit)
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
                    ["CategoriaId"] = Detalle?.CategoriaAlbumBotanicoId ?? 0
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

            if (!CanView || !CanDelete)
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
                nuevoEstado ? "Activar registro" : "Desactivar registro",
                $"¿Desea {(nuevoEstado ? "activar" : "desactivar")} " +
                $"'{Detalle.Titulo}'?",
                "Sí",
                "No");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                ApiResult<bool> result =
                    await apiService.CambiarEstadoRegistroAsync(Id, nuevoEstado);

                if (!result.Success)
                {
                    await page.DisplayAlert(
                        "No fue posible",
                        result.Message,
                        "Aceptar");
                    return;
                }

                AlbumBotanicoRefreshState.MarcarCambio();
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
