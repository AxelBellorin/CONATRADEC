using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class NoticiasViewModel : GlobalService
    {
        private readonly PublicacionApiService apiService = new();

        private CategoriaPublicacionResponse? categoriaSeleccionada;
        private string textoBusqueda = string.Empty;
        private bool soloDestacadas;
        private bool soloEventos;
        private bool isRefreshing;
        private string mensaje = string.Empty;
        private int paginaActual = 1;
        private int totalPaginas = 1;
        private bool categoriasCargadas;
        private bool pantallaCargada;
        private CancellationTokenSource? cargaCancellationTokenSource;

        public NoticiasViewModel()
        {
            Categorias = new ObservableCollection<
                CategoriaPublicacionResponse>();

            Publicaciones = new ObservableCollection<
                PublicacionListadoResponse>();

            BuscarCommand = new Command(
                async () => await CargarAsync(true),
                () => !IsBusy && CanView);

            LimpiarFiltrosCommand = new Command(
                async () => await LimpiarFiltrosAsync(),
                () => !IsBusy && CanView);

            RefrescarCommand = new Command(
                async () => await RefrescarAsync(),
                () => !IsBusy && CanView);

            CargarMasCommand = new Command(
                async () => await CargarMasAsync(),
                () => !IsBusy && PuedeCargarMas && CanView);

            AbrirDetalleCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await AbrirDetalleAsync(item),
                    item => !IsBusy && item != null && CanView);

            AbrirAdministracionCommand = new Command(
                async () => await AbrirAdministracionAsync(),
                () => !IsBusy && CanAdministrar);
        }

        public ObservableCollection<CategoriaPublicacionResponse>
            Categorias { get; }

        public ObservableCollection<PublicacionListadoResponse>
            Publicaciones { get; }

        public CategoriaPublicacionResponse? CategoriaSeleccionada
        {
            get => categoriaSeleccionada;
            set
            {
                if (ReferenceEquals(categoriaSeleccionada, value))
                    return;

                categoriaSeleccionada = value;
                OnPropertyChanged();
            }
        }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                textoBusqueda = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public bool SoloDestacadas
        {
            get => soloDestacadas;
            set
            {
                if (soloDestacadas == value)
                    return;

                soloDestacadas = value;
                OnPropertyChanged();
            }
        }

        public bool SoloEventos
        {
            get => soloEventos;
            set
            {
                if (soloEventos == value)
                    return;

                soloEventos = value;
                OnPropertyChanged();
            }
        }

        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                if (isRefreshing == value)
                    return;

                isRefreshing = value;
                OnPropertyChanged();
            }
        }

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

        public bool TienePublicaciones =>
            Publicaciones.Count > 0;

        public bool MostrarVacio =>
            pantallaCargada && !TienePublicaciones && !IsBusy;

        public bool PuedeCargarMas =>
            paginaActual < totalPaginas;

        public bool CanAdministrar =>
            CanAdd || CanEdit || CanDelete;

        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command CargarMasCommand { get; }
        public Command<PublicacionListadoResponse> AbrirDetalleCommand { get; }
        public Command AbrirAdministracionCommand { get; }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("noticiasPage");
            OnPropertyChanged(nameof(CanAdministrar));
            ActualizarComandos();
        }

        public async Task InicializarAsync()
        {
            if (!CanView || IsBusy)
                return;

            bool debeRecargar =
                !pantallaCargada ||
                PublicacionListadoEstadoService.HayActualizacionPendiente;

            if (debeRecargar)
            {
                await CargarAsync(true);
                PublicacionListadoEstadoService.ConfirmarActualizacion();
            }
        }

        public async Task CargarAsync(bool reiniciar)
        {
            if (!CanView || IsBusy)
                return;

            if (!await ValidarInternetAsync())
                return;

            cargaCancellationTokenSource?.Cancel();
            cargaCancellationTokenSource?.Dispose();

            var currentCancellationTokenSource =
                new CancellationTokenSource();

            cargaCancellationTokenSource =
                currentCancellationTokenSource;

            CancellationToken cancellationToken =
                currentCancellationTokenSource.Token;

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;

                if (!categoriasCargadas)
                {
                    await CargarCategoriasAsync(cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                int paginaSolicitada = reiniciar
                    ? 1
                    : paginaActual;

                ApiResult<PublicacionPaginadaResponse> result =
                    await apiService.GetFeedAsync(
                        CategoriaSeleccionada?.CategoriaPublicacionId,
                        TextoBusqueda,
                        SoloDestacadas,
                        SoloEventos,
                        paginaSolicitada,
                        12,
                        cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                if (!result.Success || result.Data == null)
                {
                    Mensaje = result.Message;
                    return;
                }

                if (reiniciar)
                    Publicaciones.Clear();

                foreach (PublicacionListadoResponse item
                         in result.Data.Items)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    Publicaciones.Add(item);
                }

                paginaActual = result.Data.Pagina;
                totalPaginas = result.Data.TotalPaginas;
                pantallaCargada = true;

                NotificarEstadoLista();
            }
            catch (OperationCanceledException)
            {
                // La carga se cancela al salir de la pantalla.
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Mensaje =
                        "No fue posible cargar las noticias en este momento.";

                    await MostrarErrorInesperadoAsync(
                        "cargar las noticias",
                        ex);
                }
            }
            finally
            {
                if (ReferenceEquals(
                        cargaCancellationTokenSource,
                        currentCancellationTokenSource))
                {
                    cargaCancellationTokenSource.Dispose();
                    cargaCancellationTokenSource = null;
                }

                IsBusy = false;
                IsRefreshing = false;
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        public void CancelarCarga()
        {
            cargaCancellationTokenSource?.Cancel();
        }

        private async Task CargarCategoriasAsync(
            CancellationToken cancellationToken)
        {
            ApiResult<List<CategoriaPublicacionResponse>> result =
                await apiService.GetCategoriasAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            if (!result.Success || result.Data == null)
            {
                Mensaje = result.Message;
                return;
            }

            Categorias.Clear();
            CategoriaPublicacionResponse todas =
                CategoriaPublicacionResponse.Todas();

            Categorias.Add(todas);

            foreach (CategoriaPublicacionResponse categoria
                     in result.Data.OrderBy(x => x.Orden))
            {
                Categorias.Add(categoria);
            }

            CategoriaSeleccionada ??= todas;
            categoriasCargadas = true;
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda = string.Empty;
            SoloDestacadas = false;
            SoloEventos = false;
            CategoriaSeleccionada = Categorias.FirstOrDefault();
            await CargarAsync(true);
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;
            await CargarAsync(true);
        }

        private async Task CargarMasAsync()
        {
            if (!PuedeCargarMas)
                return;

            paginaActual++;
            await CargarAsync(false);
        }

        private async Task AbrirDetalleAsync(
            PublicacionListadoResponse? item)
        {
            if (item == null || IsBusy)
                return;

            await GoToAsyncParameters(
                AppRoutes.NoticiaDetalle,
                new Dictionary<string, object>
                {
                    ["PublicacionId"] = item.PublicacionId
                });
        }

        private async Task AbrirAdministracionAsync()
        {
            if (!CanAdministrar || IsBusy)
                return;

            await GoToAsyncParameters(
                AppRoutes.PublicacionesAdmin);
        }

        private void NotificarEstadoLista()
        {
            OnPropertyChanged(nameof(TienePublicaciones));
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(PuedeCargarMas));
        }

        private void ActualizarComandos()
        {
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            CargarMasCommand.ChangeCanExecute();
            AbrirDetalleCommand.ChangeCanExecute();
            AbrirAdministracionCommand.ChangeCanExecute();
        }
    }
}
