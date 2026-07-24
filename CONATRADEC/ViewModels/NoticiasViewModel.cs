using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
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
        private bool cargandoMas;
        private string mensaje = string.Empty;
        private int paginaActual;
        private int totalPaginas = 1;
        private int totalRegistros;
        private bool categoriasCargadas;
        private bool pantallaCargada;
        private bool ultimaCargaExitosa;
        private long versionAplicada = -1;
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
                () => !IsBusy && !CargandoMas &&
                      PuedeCargarMas && CanView);

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

        public new bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                if (base.IsBusy == value)
                    return;

                base.IsBusy = value;
                ActualizarComandos();
                NotificarEstadoLista();
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

        public bool CargandoMas
        {
            get => cargandoMas;
            private set
            {
                if (cargandoMas == value)
                    return;

                cargandoMas = value;
                OnPropertyChanged();
                ActualizarComandos();
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

        public bool TienePublicaciones => Publicaciones.Count > 0;

        public bool MostrarVacio =>
            pantallaCargada &&
            !TienePublicaciones &&
            !IsBusy;

        public bool PuedeCargarMas =>
            paginaActual < totalPaginas;

        public bool MostrarFinLista =>
            pantallaCargada &&
            TienePublicaciones &&
            !PuedeCargarMas &&
            !CargandoMas;

        public string TotalTexto =>
            totalRegistros == 1
                ? "1 publicación"
                : $"{totalRegistros} publicaciones";

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

            bool hayCambios =
                PublicacionListadoEstadoService
                    .HayCambiosDesde(versionAplicada);

            bool debeRecargar =
                !pantallaCargada || hayCambios;

            if (!debeRecargar)
                return;

            if (hayCambios)
                categoriasCargadas = false;

            await CargarInicialAsync();
            if (ultimaCargaExitosa)
            {
                versionAplicada =
                    PublicacionListadoEstadoService.VersionActual;
            }
        }

        public async Task CargarAsync(bool reiniciar)
        {
            if (!CanView)
                return;

            if (reiniciar && IsBusy)
                return;

            if (!reiniciar &&
                (CargandoMas || !PuedeCargarMas))
            {
                return;
            }

            CancellationTokenSource source =
                PrepararNuevaCarga(reiniciar);

            try
            {
                if (reiniciar)
                {
                    ultimaCargaExitosa = false;
                    IsBusy = true;
                    Mensaje = string.Empty;
                }
                else
                {
                    CargandoMas = true;
                }

                int pagina = reiniciar
                    ? 1
                    : paginaActual + 1;

                ApiResult<PublicacionPaginadaResponse> result =
                    await apiService.GetFeedAsync(
                        ObtenerCategoriaId(),
                        TextoBusqueda,
                        SoloDestacadas,
                        SoloEventos,
                        pagina,
                        ObtenerTamanoPagina(),
                        source.Token);

                if (source.IsCancellationRequested)
                    return;

                if (!result.Success || result.Data == null)
                {
                    if (!EsMensajeCancelacion(result.Message))
                        Mensaje = result.Message;

                    return;
                }

                AplicarPagina(
                    result.Data,
                    reiniciar);
            }
            catch (OperationCanceledException)
            {
                // La pantalla se cerró o una nueva consulta reemplazó esta.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested)
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
                if (reiniciar)
                {
                    IsBusy = false;
                    IsRefreshing = false;
                }
                else
                {
                    CargandoMas = false;
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        public void CancelarCarga()
        {
            cargaCancellationTokenSource?.Cancel();
        }

        private async Task CargarInicialAsync()
        {
            CancellationTokenSource source =
                PrepararNuevaCarga(true);

            try
            {
                ultimaCargaExitosa = false;
                IsBusy = true;
                Mensaje = string.Empty;

                Task<ApiResult<List<CategoriaPublicacionResponse>>>
                    categoriasTask = categoriasCargadas
                        ? Task.FromResult(
                            ApiResult<List<CategoriaPublicacionResponse>>
                                .Ok(new List<CategoriaPublicacionResponse>()))
                        : apiService.GetCategoriasAsync(source.Token);

                Task<ApiResult<PublicacionPaginadaResponse>>
                    publicacionesTask = apiService.GetFeedAsync(
                        ObtenerCategoriaId(),
                        TextoBusqueda,
                        SoloDestacadas,
                        SoloEventos,
                        1,
                        ObtenerTamanoPagina(),
                        source.Token);

                await Task.WhenAll(
                    categoriasTask,
                    publicacionesTask);

                if (source.IsCancellationRequested)
                    return;

                ApiResult<List<CategoriaPublicacionResponse>>
                    categoriasResult = await categoriasTask;

                ApiResult<PublicacionPaginadaResponse>
                    publicacionesResult = await publicacionesTask;

                if (!categoriasCargadas)
                {
                    if (!categoriasResult.Success ||
                        categoriasResult.Data == null)
                    {
                        Mensaje = categoriasResult.Message;
                        return;
                    }

                    AplicarCategorias(categoriasResult.Data);
                }

                if (!publicacionesResult.Success ||
                    publicacionesResult.Data == null)
                {
                    Mensaje = publicacionesResult.Message;
                    return;
                }

                AplicarPagina(
                    publicacionesResult.Data,
                    reiniciar: true);
            }
            catch (OperationCanceledException)
            {
                // Se canceló al navegar.
            }
            finally
            {
                IsBusy = false;
                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        private void AplicarCategorias(
            IEnumerable<CategoriaPublicacionResponse> items)
        {
            int? seleccionAnterior =
                CategoriaSeleccionada?.CategoriaPublicacionId;

            Categorias.Clear();
            Categorias.Add(
                CategoriaPublicacionResponse.Todas());

            foreach (CategoriaPublicacionResponse categoria
                     in items.OrderBy(x => x.Orden))
            {
                Categorias.Add(categoria);
            }

            CategoriaSeleccionada =
                Categorias.FirstOrDefault(x =>
                    x.CategoriaPublicacionId == seleccionAnterior)
                ?? Categorias.FirstOrDefault();

            categoriasCargadas = true;
        }

        private void AplicarPagina(
            PublicacionPaginadaResponse pagina,
            bool reiniciar)
        {
            if (reiniciar)
                Publicaciones.Clear();

            foreach (PublicacionListadoResponse item
                     in pagina.Items)
            {
                if (Publicaciones.Any(x =>
                        x.PublicacionId == item.PublicacionId))
                {
                    continue;
                }

                item.ImagenPortadaUrl =
                    ImagenMiniaturaUrlService.Crear(
                        item.ImagenPortadaUrl,
                        ancho: 720,
                        alto: 480,
                        calidad: 68);

                Publicaciones.Add(item);
            }

            paginaActual = pagina.Pagina;
            totalPaginas = Math.Max(1, pagina.TotalPaginas);
            totalRegistros = pagina.TotalRegistros;
            pantallaCargada = true;
            ultimaCargaExitosa = true;
            versionAplicada =
                PublicacionListadoEstadoService.VersionActual;
            NotificarEstadoLista();
        }

        private int? ObtenerCategoriaId()
        {
            int? id =
                CategoriaSeleccionada?
                    .CategoriaPublicacionId;

            return id.HasValue && id.Value > 0
                ? id
                : null;
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Current.Platform == DevicePlatform.WinUI
                ? 12
                : 6;

        private CancellationTokenSource PrepararNuevaCarga(
            bool cancelarAnterior)
        {
            if (cancelarAnterior)
            {
                cargaCancellationTokenSource?.Cancel();
                cargaCancellationTokenSource?.Dispose();
                cargaCancellationTokenSource = null;
            }

            var source = new CancellationTokenSource();
            cargaCancellationTokenSource = source;
            return source;
        }

        private void LiberarCarga(
            CancellationTokenSource source)
        {
            if (!ReferenceEquals(
                    cargaCancellationTokenSource,
                    source))
            {
                source.Dispose();
                return;
            }

            cargaCancellationTokenSource.Dispose();
            cargaCancellationTokenSource = null;
        }

        private async Task LimpiarFiltrosAsync()
        {
            textoBusqueda = string.Empty;
            soloDestacadas = false;
            soloEventos = false;
            categoriaSeleccionada =
                Categorias.FirstOrDefault();

            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(SoloDestacadas));
            OnPropertyChanged(nameof(SoloEventos));
            OnPropertyChanged(nameof(CategoriaSeleccionada));

            await CargarAsync(true);
        }

        private async Task RefrescarAsync()
        {
            try
            {
                IsRefreshing = true;
                await CargarAsync(true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task CargarMasAsync()
        {
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

        private static bool EsMensajeCancelacion(
            string? message) =>
            string.Equals(
                message,
                "La operación fue cancelada.",
                StringComparison.OrdinalIgnoreCase);

        private void NotificarEstadoLista()
        {
            OnPropertyChanged(nameof(TienePublicaciones));
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(PuedeCargarMas));
            OnPropertyChanged(nameof(MostrarFinLista));
            OnPropertyChanged(nameof(TotalTexto));
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
