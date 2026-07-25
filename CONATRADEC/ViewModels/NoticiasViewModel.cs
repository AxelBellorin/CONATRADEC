using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

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
        private bool navegando;
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
                async () => await EjecutarComandoSeguroAsync(
                    () => CargarAsync(reiniciar: true),
                    "buscar noticias"),
                () => !IsBusy && !Navegando && CanView);

            LimpiarFiltrosCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    LimpiarFiltrosAsync,
                    "limpiar los filtros de noticias"),
                () => !IsBusy && !Navegando && CanView);

            RefrescarCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    RefrescarAsync,
                    "actualizar las noticias"),
                () => !IsBusy && !Navegando && CanView);

            CargarMasCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    CargarMasAsync,
                    "cargar más noticias"),
                () =>
                    !IsBusy &&
                    !CargandoMas &&
                    !Navegando &&
                    PuedeCargarMas &&
                    CanView);

            AbrirDetalleCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await EjecutarComandoSeguroAsync(
                        () => AbrirDetalleAsync(item),
                        "abrir la noticia"),
                    item =>
                        item != null &&
                        !IsBusy &&
                        !Navegando &&
                        CanView);

            /*
             * Este comando permanece disponible aunque el feed todavía esté
             * cargando. Al pulsarlo se cancela la solicitud anterior de forma
             * segura antes de navegar a la administración.
             */
            AbrirAdministracionCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    AbrirAdministracionAsync,
                    "abrir la administración de publicaciones"),
                () => !Navegando && CanAdministrar);
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
                string nuevoValor = value ?? string.Empty;

                if (textoBusqueda == nuevoValor)
                    return;

                textoBusqueda = nuevoValor;
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
                NotificarEstadoLista();
            }
        }

        public bool Navegando
        {
            get => navegando;
            private set
            {
                if (navegando == value)
                    return;

                navegando = value;
                OnPropertyChanged();
                ActualizarComandos();
            }
        }

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                string nuevoValor = value ?? string.Empty;

                if (mensaje == nuevoValor)
                    return;

                mensaje = nuevoValor;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(Mensaje);

        public bool TienePublicaciones =>
            Publicaciones.Count > 0;

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
            if (!CanView || IsBusy || Navegando)
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
            if (!CanView || Navegando)
                return;

            if (reiniciar && IsBusy)
                return;

            if (!reiniciar &&
                (CargandoMas || !PuedeCargarMas))
            {
                return;
            }

            CancellationTokenSource source =
                PrepararNuevaCarga();

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

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source))
                {
                    return;
                }

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
                // Cancelación normal al navegar o reemplazar la consulta.
            }
            catch (ObjectDisposedException)
            {
                // Puede ocurrir si Android cierra el stream mientras se navega.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaActual(source))
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
                if (EsCargaActual(source))
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
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        /// <summary>
        /// Cancela la solicitud de la pantalla sin disponer inmediatamente el
        /// CancellationTokenSource. La tarea propietaria lo dispone al terminar.
        /// Esto evita ObjectDisposedException durante una navegación rápida.
        /// </summary>
        public void CancelarCarga()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(
                    ref cargaCancellationTokenSource,
                    null);

            CancelarSeguro(source);

            IsBusy = false;
            IsRefreshing = false;
            CargandoMas = false;
        }

        private async Task CargarInicialAsync()
        {
            CancellationTokenSource source =
                PrepararNuevaCarga();

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

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source))
                {
                    return;
                }

                ApiResult<List<CategoriaPublicacionResponse>>
                    categoriasResult = await categoriasTask;

                ApiResult<PublicacionPaginadaResponse>
                    publicacionesResult = await publicacionesTask;

                if (!categoriasCargadas)
                {
                    if (!categoriasResult.Success ||
                        categoriasResult.Data == null)
                    {
                        if (!EsMensajeCancelacion(categoriasResult.Message))
                            Mensaje = categoriasResult.Message;

                        return;
                    }

                    AplicarCategorias(categoriasResult.Data);
                }

                if (!publicacionesResult.Success ||
                    publicacionesResult.Data == null)
                {
                    if (!EsMensajeCancelacion(publicacionesResult.Message))
                        Mensaje = publicacionesResult.Message;

                    return;
                }

                AplicarPagina(
                    publicacionesResult.Data,
                    reiniciar: true);
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al salir de la pantalla.
            }
            catch (ObjectDisposedException)
            {
                // El stream se cerró porque la navegación canceló la petición.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaActual(source))
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
                if (EsCargaActual(source))
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

        private CancellationTokenSource PrepararNuevaCarga()
        {
            var source = new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref cargaCancellationTokenSource,
                    source);

            CancelarSeguro(anterior);
            return source;
        }

        private bool EsCargaActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(ref cargaCancellationTokenSource),
                source);

        private void LiberarCarga(
            CancellationTokenSource source)
        {
            Interlocked.CompareExchange(
                ref cargaCancellationTokenSource,
                null,
                source);

            source.Dispose();
        }

        private static void CancelarSeguro(
            CancellationTokenSource? source)
        {
            if (source == null)
                return;

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Otra continuación terminó y dispuso el token primero.
            }
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

            await CargarAsync(reiniciar: true);
        }

        private async Task RefrescarAsync()
        {
            try
            {
                IsRefreshing = true;
                await CargarAsync(reiniciar: true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task CargarMasAsync() =>
            CargarAsync(reiniciar: false);

        private async Task AbrirDetalleAsync(
            PublicacionListadoResponse? item)
        {
            if (item == null || Navegando)
                return;

            await NavegarSeguroAsync(
                AppRoutes.NoticiaDetalle,
                new Dictionary<string, object>
                {
                    ["PublicacionId"] = item.PublicacionId
                });
        }

        private async Task AbrirAdministracionAsync()
        {
            if (!CanAdministrar || Navegando)
                return;

            await NavegarSeguroAsync(
                AppRoutes.PublicacionesAdmin);
        }

        private async Task NavegarSeguroAsync(
            string ruta,
            IDictionary<string, object>? parametros = null)
        {
            if (Navegando)
                return;

            Navegando = true;

            try
            {
                /*
                 * Primero se separa y cancela la carga actual. No se dispone
                 * aquí; su propia continuación la liberará al finalizar.
                 */
                CancelarCarga();
                await Task.Yield();

                await GoToAsyncParameters(
                    ruta,
                    parametros);
            }
            finally
            {
                Navegando = false;
            }
        }

        private async Task EjecutarComandoSeguroAsync(
            Func<Task> accion,
            string operacion)
        {
            try
            {
                await accion();
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada por navegación o nueva consulta.
            }
            catch (ObjectDisposedException)
            {
                // Cierre esperado del stream o token durante navegación rápida.
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    operacion,
                    ex);
            }
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
