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

        // Filtros editables: cambian en pantalla sin consultar la API.
        private CategoriaPublicacionResponse? categoriaSeleccionada;
        private string textoBusqueda = string.Empty;
        private bool soloDestacadas;
        private bool soloEventos;

        // Filtros aplicados: representan exactamente el listado visible.
        private int? categoriaAplicadaId;
        private string textoBusquedaAplicado = string.Empty;
        private bool soloDestacadasAplicado;
        private bool soloEventosAplicado;

        private bool isRefreshing;
        private bool cargandoListado;
        private bool navegando;
        private string mensaje = string.Empty;
        private int paginaActual = 1;
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
                    AplicarFiltrosYBuscarAsync,
                    "buscar noticias"),
                PuedeEjecutarAccionListado);

            LimpiarFiltrosCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    LimpiarFiltrosAsync,
                    "limpiar los filtros de noticias"),
                PuedeEjecutarAccionListado);

            RefrescarCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    RefrescarAsync,
                    "actualizar las noticias"),
                PuedeEjecutarAccionListado);

            PaginaAnteriorCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    () => IrPaginaAsync(PaginaActual - 1),
                    "cargar la página anterior de noticias"),
                () =>
                    PuedeEjecutarAccionListado() &&
                    PuedeIrAnterior);

            PaginaSiguienteCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    () => IrPaginaAsync(PaginaActual + 1),
                    "cargar la página siguiente de noticias"),
                () =>
                    PuedeEjecutarAccionListado() &&
                    PuedeIrSiguiente);

            AbrirDetalleCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await EjecutarComandoSeguroAsync(
                        () => AbrirDetalleAsync(item),
                        "abrir la noticia"),
                    item =>
                        item != null &&
                        PuedeEjecutarAccionListado());

            /*
             * La administración es un módulo independiente. El servicio de
             * visita detecta esta navegación y finaliza la visita pública de
             * Noticias antes de abrir la pantalla administrativa.
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
                ActualizarComandos();
            }
        }

        public bool CargandoListado
        {
            get => cargandoListado;
            private set
            {
                if (cargandoListado == value)
                    return;

                cargandoListado = value;
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
            !IsBusy &&
            !CargandoListado;

        public bool SeHaListado => pantallaCargada;

        /// <summary>
        /// Indica si la última consulta del listado terminó correctamente.
        /// </summary>
        public bool UltimaCargaExitosa => ultimaCargaExitosa;

        /// <summary>
        /// Permite a la página distinguir un simple retorno desde Detalle de
        /// una mutación realizada dentro de la misma visita.
        /// </summary>
        public bool RequiereRecargaPorCambios =>
            pantallaCargada &&
            PublicacionListadoEstadoService
                .HayCambiosDesde(versionAplicada);

        public int PaginaActual =>
            Math.Max(1, paginaActual);

        public int TotalPaginas =>
            Math.Max(1, totalPaginas);

        public bool PuedeIrAnterior =>
            pantallaCargada &&
            PaginaActual > 1;

        public bool PuedeIrSiguiente =>
            pantallaCargada &&
            PaginaActual < TotalPaginas;

        public bool MostrarPaginacion =>
            CanView &&
            pantallaCargada &&
            (Publicaciones.Count > 0 || TotalPaginas > 1);

        public string PaginaTexto =>
            $"Página {PaginaActual} de {TotalPaginas}";

        public string RangoPaginaTexto
        {
            get
            {
                if (totalRegistros <= 0 ||
                    Publicaciones.Count == 0)
                {
                    return "Sin publicaciones en esta página";
                }

                return Publicaciones.Count == 1
                    ? $"1 publicación en esta página · {totalRegistros} en total"
                    : $"{Publicaciones.Count} publicaciones en esta página · {totalRegistros} en total";
            }
        }

        public string TotalTexto =>
            totalRegistros == 1
                ? "1 publicación"
                : $"{totalRegistros} publicaciones";

        public bool CanAdministrar =>
            CanAdd || CanEdit || CanDelete;

        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }
        public Command<PublicacionListadoResponse> AbrirDetalleCommand { get; }
        public Command AbrirAdministracionCommand { get; }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("noticiasPage");
            OnPropertyChanged(nameof(CanAdministrar));
            NotificarEstadoLista();
            ActualizarComandos();
        }

        /// <summary>
        /// Reinicia exclusivamente el estado de la visita pública y obtiene
        /// datos frescos del servidor (o de la copia local cuando la sesión es
        /// offline). Los filtros de una visita anterior nunca se reutilizan.
        /// </summary>
        public async Task IniciarNuevaVisitaAsync()
        {
            if (!CanView || Navegando)
                return;

            CancelarCarga();

            pantallaCargada = false;
            ultimaCargaExitosa = false;
            categoriasCargadas = false;
            versionAplicada = -1;
            paginaActual = 1;
            totalPaginas = 1;
            totalRegistros = 0;
            Mensaje = string.Empty;

            textoBusqueda = string.Empty;
            soloDestacadas = false;
            soloEventos = false;
            categoriaSeleccionada = null;

            textoBusquedaAplicado = string.Empty;
            soloDestacadasAplicado = false;
            soloEventosAplicado = false;
            categoriaAplicadaId = null;

            Publicaciones.Clear();
            Categorias.Clear();

            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(SoloDestacadas));
            OnPropertyChanged(nameof(SoloEventos));
            OnPropertyChanged(nameof(CategoriaSeleccionada));
            NotificarEstadoLista();

            await CargarInicialAsync();
        }

        /// <summary>
        /// Conserva compatibilidad con el contrato previo del code-behind.
        /// Dentro de una misma visita solo consulta si hubo una mutación.
        /// </summary>
        public async Task InicializarAsync()
        {
            if (!CanView || IsBusy || CargandoListado || Navegando)
                return;

            if (!pantallaCargada)
            {
                await CargarInicialAsync();
                return;
            }

            if (RequiereRecargaPorCambios)
                await RecargarPaginaActualAsync();
        }

        /// <summary>
        /// Recarga la página que el usuario estaba viendo con los filtros que
        /// realmente pertenecen al listado actual.
        /// </summary>
        public Task RecargarPaginaActualAsync() =>
            CargarPaginaAsync(
                PaginaActual,
                mostrarBloqueo: true);

        public void CancelarCarga()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(
                    ref cargaCancellationTokenSource,
                    null);

            CancelarSeguro(source);

            IsBusy = false;
            IsRefreshing = false;
            CargandoListado = false;
        }

        private bool PuedeEjecutarAccionListado() =>
            !IsBusy &&
            !IsRefreshing &&
            !CargandoListado &&
            !Navegando &&
            CanView;

        private async Task AplicarFiltrosYBuscarAsync()
        {
            AplicarFiltrosEditados();
            await CargarPaginaAsync(
                1,
                mostrarBloqueo: true);
        }

        private void AplicarFiltrosEditados()
        {
            categoriaAplicadaId =
                ObtenerCategoriaId(
                    CategoriaSeleccionada);

            textoBusquedaAplicado =
                TextoBusqueda.Trim();

            soloDestacadasAplicado = SoloDestacadas;
            soloEventosAplicado = SoloEventos;
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

            categoriaAplicadaId = null;
            textoBusquedaAplicado = string.Empty;
            soloDestacadasAplicado = false;
            soloEventosAplicado = false;

            await CargarPaginaAsync(
                1,
                mostrarBloqueo: true);
        }

        private async Task RefrescarAsync()
        {
            if (!PuedeEjecutarAccionListado())
                return;

            try
            {
                IsRefreshing = true;

                await CargarPaginaAsync(
                    PaginaActual,
                    mostrarBloqueo: false);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task IrPaginaAsync(int pagina)
        {
            int destino = Math.Clamp(
                pagina,
                1,
                TotalPaginas);

            if (destino == PaginaActual)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                destino,
                mostrarBloqueo: true);
        }

        private async Task CargarInicialAsync()
        {
            CancellationTokenSource source =
                PrepararNuevaCarga();

            try
            {
                ultimaCargaExitosa = false;
                CargandoListado = true;
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
                        categoriaAplicadaId,
                        textoBusquedaAplicado,
                        soloDestacadasAplicado,
                        soloEventosAplicado,
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

                AplicarPagina(publicacionesResult.Data);
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
                {
                    IsBusy = false;
                    CargandoListado = false;
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        private async Task CargarPaginaAsync(
            int pagina,
            bool mostrarBloqueo)
        {
            if (!CanView || Navegando)
                return;

            if (CargandoListado ||
                (mostrarBloqueo && IsBusy))
            {
                return;
            }

            CancellationTokenSource source =
                PrepararNuevaCarga();

            try
            {
                ultimaCargaExitosa = false;
                CargandoListado = true;
                Mensaje = string.Empty;

                if (mostrarBloqueo)
                    IsBusy = true;

                ApiResult<PublicacionPaginadaResponse> result =
                    await ConsultarPaginaAsync(
                        pagina,
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

                PublicacionPaginadaResponse paginaRecibida =
                    result.Data;

                /*
                 * Normalización excepcional: si otros usuarios redujeron el
                 * número de páginas entre consultas, se obtiene la última página
                 * válida. En el flujo normal cada cambio de página hace un GET.
                 */
                int totalDisponible =
                    Math.Max(1, paginaRecibida.TotalPaginas);

                if (paginaRecibida.TotalRegistros > 0 &&
                    paginaRecibida.Pagina > totalDisponible)
                {
                    ApiResult<PublicacionPaginadaResponse>
                        normalizado = await ConsultarPaginaAsync(
                            totalDisponible,
                            source.Token);

                    if (source.IsCancellationRequested ||
                        !EsCargaActual(source))
                    {
                        return;
                    }

                    if (!normalizado.Success ||
                        normalizado.Data == null)
                    {
                        if (!EsMensajeCancelacion(normalizado.Message))
                            Mensaje = normalizado.Message;

                        return;
                    }

                    paginaRecibida = normalizado.Data;
                }

                AplicarPagina(paginaRecibida);
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al navegar o reemplazar la consulta.
            }
            catch (ObjectDisposedException)
            {
                // Puede ocurrir si Android cierra el stream durante navegación.
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
                    if (mostrarBloqueo)
                        IsBusy = false;

                    CargandoListado = false;
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        private Task<ApiResult<PublicacionPaginadaResponse>>
            ConsultarPaginaAsync(
                int pagina,
                CancellationToken cancellationToken) =>
            apiService.GetFeedAsync(
                categoriaAplicadaId,
                textoBusquedaAplicado,
                soloDestacadasAplicado,
                soloEventosAplicado,
                Math.Max(1, pagina),
                ObtenerTamanoPagina(),
                cancellationToken);

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
            PublicacionPaginadaResponse pagina)
        {
            Publicaciones.Clear();

            foreach (PublicacionListadoResponse item
                     in pagina.Items)
            {
                item.ImagenPortadaUrl =
                    ImagenMiniaturaUrlService.Crear(
                        item.ImagenPortadaUrl,
                        ancho: 720,
                        alto: 480,
                        calidad: 68);

                Publicaciones.Add(item);
            }

            paginaActual = Math.Max(1, pagina.Pagina);
            totalPaginas = Math.Max(1, pagina.TotalPaginas);
            totalRegistros = Math.Max(0, pagina.TotalRegistros);
            pantallaCargada = true;
            ultimaCargaExitosa = true;
            versionAplicada =
                PublicacionListadoEstadoService.VersionActual;

            NotificarEstadoLista();
        }

        private static int? ObtenerCategoriaId(
            CategoriaPublicacionResponse? categoria)
        {
            int? id = categoria?.CategoriaPublicacionId;

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
                 * Se cancela una consulta en curso antes de navegar. Su propia
                 * continuación es responsable de disponer el token.
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
            OnPropertyChanged(nameof(SeHaListado));
            OnPropertyChanged(nameof(UltimaCargaExitosa));
            OnPropertyChanged(nameof(RequiereRecargaPorCambios));
            OnPropertyChanged(nameof(PaginaActual));
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(PuedeIrAnterior));
            OnPropertyChanged(nameof(PuedeIrSiguiente));
            OnPropertyChanged(nameof(MostrarPaginacion));
            OnPropertyChanged(nameof(PaginaTexto));
            OnPropertyChanged(nameof(RangoPaginaTexto));
            OnPropertyChanged(nameof(TotalTexto));
        }

        private void ActualizarComandos()
        {
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            PaginaAnteriorCommand.ChangeCanExecute();
            PaginaSiguienteCommand.ChangeCanExecute();
            AbrirDetalleCommand.ChangeCanExecute();
            AbrirAdministracionCommand.ChangeCanExecute();
        }
    }
}
