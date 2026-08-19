using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Administra exclusivamente las subcategorías/fichas inactivas del Álbum.
    /// Mantiene una sola página en memoria y nunca mezcla eliminados con el
    /// listado activo de la visita principal.
    /// </summary>
    public sealed class AlbumEliminadosViewModel : GlobalService
    {
        private readonly AlbumAdministracionApiService apiService = new();
        private readonly ObservableCollection<AlbumGaleriaJerarquiaItemResponse>
            registros = new();
        private CancellationTokenSource? cargaCts;
        private string textoBusqueda = string.Empty;
        private string textoBusquedaAplicado = string.Empty;
        private bool isRefreshing;
        private bool cargando;
        private bool inicializado;
        private int paginaActual = 1;
        private int totalPaginas;
        private int totalRegistros;
        private long versionCargada = -1;

        private int TamanoPagina =>
            DeviceInfo.Platform == DevicePlatform.WinUI ? 12 : 8;

        public ObservableCollection<AlbumGaleriaJerarquiaItemResponse>
            Registros => registros;

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                textoBusqueda = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                isRefreshing = value;
                OnPropertyChanged();
            }
        }

        public int PaginaActual
        {
            get => paginaActual;
            private set
            {
                if (paginaActual == value)
                    return;

                paginaActual = value;
                OnPropertyChanged();
                NotificarPaginacion();
            }
        }

        public int TotalPaginas => Math.Max(1, totalPaginas);
        public bool PuedeIrAnterior => totalRegistros > 0 && PaginaActual > 1;
        public bool PuedeIrSiguiente =>
            totalRegistros > 0 && PaginaActual < totalPaginas;
        public bool MostrarPaginacion => totalRegistros > 0;
        public bool HayRegistros => Registros.Count > 0;
        public bool SinRegistros => !HayRegistros;
        public bool RequiereRecargaPorCambios =>
            inicializado &&
            versionCargada != AlbumBotanicoRefreshState.VersionActual;
        public string PaginaTexto => $"Página {PaginaActual} de {TotalPaginas}";

        public string RangoPaginaTexto
        {
            get
            {
                if (totalRegistros == 0)
                    return "0 registros";

                int inicio = ((PaginaActual - 1) * TamanoPagina) + 1;
                int fin = Math.Min(inicio + Registros.Count - 1, totalRegistros);
                return $"{inicio}-{fin} de {totalRegistros}";
            }
        }

        public Command BuscarCommand { get; }
        public Command LimpiarCommand { get; }
        public Command RefrescarCommand { get; }
        public Command<AlbumGaleriaJerarquiaItemResponse> ReactivarCommand { get; }
        public Command CategoriasEliminadasCommand { get; }
        public Command CerrarCommand { get; }

        public AlbumEliminadosViewModel()
        {
            BuscarCommand = new Command(async () => await BuscarAsync());
            LimpiarCommand = new Command(async () => await LimpiarAsync());
            RefrescarCommand = new Command(async () => await RefrescarAsync());
            ReactivarCommand =
                new Command<AlbumGaleriaJerarquiaItemResponse>(
                    async item => await ReactivarAsync(item));
            CategoriasEliminadasCommand =
                new Command(async () => await AbrirCategoriasEliminadasAsync());
            CerrarCommand = new Command(async () => await CerrarAsync());
        }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("albumFotosPage");
            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanEdit));
        }

        public Task InicializarAsync()
        {
            if (inicializado)
                return Task.CompletedTask;

            inicializado = true;
            return CargarPaginaAsync(1, true);
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

        public Task RecargarPaginaActualAsync() =>
            CargarPaginaAsync(PaginaActual, false);

        public async Task<bool> IrPaginaAnteriorAsync()
        {
            if (!PuedeIrAnterior || cargando)
                return false;

            int origen = PaginaActual;
            await CargarPaginaAsync(PaginaActual - 1, true);
            return PaginaActual != origen;
        }

        public async Task<bool> IrPaginaSiguienteAsync()
        {
            if (!PuedeIrSiguiente || cargando)
                return false;

            int origen = PaginaActual;
            await CargarPaginaAsync(PaginaActual + 1, true);
            return PaginaActual != origen;
        }

        private async Task BuscarAsync()
        {
            textoBusquedaAplicado = TextoBusqueda.Trim();
            await CargarPaginaAsync(1, true);
        }

        private async Task LimpiarAsync()
        {
            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;
            await CargarPaginaAsync(1, true);
        }

        private async Task RefrescarAsync()
        {
            if (cargando)
                return;

            IsRefreshing = true;
            try
            {
                await CargarPaginaAsync(PaginaActual, false);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task CargarPaginaAsync(int pagina, bool showIndicator)
        {
            if (!CanView || cargando)
                return;

            cargando = true;
            if (showIndicator)
                IsBusy = true;

            CancellationTokenSource cts = RenovarCarga();
            CancellationToken token = cts.Token;

            try
            {
                ApiResult<AlbumGaleriaJerarquiaPaginaResponse> result =
                    await apiService.GetEliminadosAsync(
                        textoBusquedaAplicado,
                        pagina,
                        TamanoPagina,
                        token);

                if (token.IsCancellationRequested)
                    return;

                if (!result.Success || result.Data == null)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                AplicarPagina(result.Data);
            }
            finally
            {
                cargando = false;
                if (showIndicator)
                    IsBusy = false;
            }
        }

        private void AplicarPagina(AlbumGaleriaJerarquiaPaginaResponse pagina)
        {
            Registros.Clear();
            foreach (AlbumGaleriaJerarquiaItemResponse item in pagina.Items)
                Registros.Add(item);

            totalPaginas = pagina.TotalPaginas;
            totalRegistros = pagina.TotalRegistros;
            PaginaActual = Math.Max(1, pagina.PaginaActual);

            OnPropertyChanged(nameof(HayRegistros));
            OnPropertyChanged(nameof(SinRegistros));
            versionCargada = AlbumBotanicoRefreshState.VersionActual;
            NotificarPaginacion();
        }

        private void NotificarPaginacion()
        {
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(PuedeIrAnterior));
            OnPropertyChanged(nameof(PuedeIrSiguiente));
            OnPropertyChanged(nameof(MostrarPaginacion));
            OnPropertyChanged(nameof(PaginaTexto));
            OnPropertyChanged(nameof(RangoPaginaTexto));
        }

        private CancellationTokenSource RenovarCarga()
        {
            CancelarCarga();
            cargaCts = new CancellationTokenSource();
            return cargaCts;
        }

        private async Task ReactivarAsync(
            AlbumGaleriaJerarquiaItemResponse? item)
        {
            if (item == null || IsBusy || !CanView || !CanEdit)
                return;

            Page? page = Application.Current?.MainPage;
            if (page == null)
                return;

            bool confirmar = await page.DisplayAlert(
                "Reactivar subcategoría",
                $"¿Desea reactivar '{item.Titulo}'? Se conservarán su identificador, información técnica y fotografías históricas.",
                "Reactivar",
                "Cancelar");

            if (!confirmar)
                return;

            IsBusy = true;
            try
            {
                ApiResult<bool> result = await apiService.ReactivarAsync(
                    item.AlbumBotanicoCafeId);

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

                // El servidor normaliza automáticamente la página si quedó vacía.
                await CargarPaginaInternaAsync(PaginaActual);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CargarPaginaInternaAsync(int pagina)
        {
            if (!CanView || cargando)
                return;

            cargando = true;
            CancellationTokenSource cts = RenovarCarga();
            try
            {
                ApiResult<AlbumGaleriaJerarquiaPaginaResponse> result =
                    await apiService.GetEliminadosAsync(
                        textoBusquedaAplicado,
                        pagina,
                        TamanoPagina,
                        cts.Token);

                if (result.Success && result.Data != null &&
                    !cts.Token.IsCancellationRequested)
                {
                    AplicarPagina(result.Data);
                }
                else if (!cts.Token.IsCancellationRequested)
                {
                    await MostrarToastAsync(result.Message);
                }
            }
            finally
            {
                cargando = false;
            }
        }

        private async Task AbrirCategoriasEliminadasAsync()
        {
            if (!CanView)
                return;

            if (!CatalogoEliminadoCodigos.TryGet(
                    CatalogoEliminadoCodigos.CategoriaAlbum,
                    out CatalogoEliminadoConfiguracion configuracion))
            {
                await MostrarToastAsync(
                    "No fue posible abrir las categorías eliminadas.");
                return;
            }

            await CatalogoEliminadosLauncher.AbrirAsync(configuracion);
        }

        private async Task CerrarAsync()
        {
            if (IsBusy || Shell.Current?.Navigation == null)
                return;

            await Shell.Current.Navigation.PopModalAsync();
        }
    }
}
