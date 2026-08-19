using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class PublicacionesAdminViewModel : GlobalService
    {
        private readonly PublicacionApiService apiService = new();

        // Filtros editables: cambiar estos valores no consulta la API.
        private CategoriaPublicacionResponse? categoriaSeleccionada;
        private string estadoSeleccionado = "TODOS";
        private string textoBusqueda = string.Empty;

        // Filtros aplicados: representan exactamente el listado visible.
        private int? categoriaAplicadaId;
        private string estadoAplicado = "TODOS";
        private string textoBusquedaAplicado = string.Empty;

        private string mensaje = string.Empty;
        private bool isRefreshing;
        private bool navegando;
        private bool ejecutandoOperacion;
        private bool cargado;
        private bool ultimaCargaExitosa;
        private long versionAplicada = -1;
        private bool categoriasCargadas;
        private int paginaActual = 1;
        private int totalPaginas = 1;
        private int totalRegistros;
        private int tamanoPaginaActual;
        private CancellationTokenSource? cargaCancellationTokenSource;

        public PublicacionesAdminViewModel()
        {
            tamanoPaginaActual = ObtenerTamanoPagina();

            Categorias = new ObservableCollection<
                CategoriaPublicacionResponse>();

            Estados = new ObservableCollection<string>
            {
                "TODOS",
                "BORRADOR",
                "PUBLICADA",
                "PROGRAMADA",
                "VENCIDA",
                "ARCHIVADA"
            };

            Publicaciones = new ObservableCollection<
                PublicacionListadoResponse>();

            BuscarCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    AplicarFiltrosYBuscarAsync,
                    "buscar publicaciones"),
                PuedeEjecutarAccionListado);

            LimpiarFiltrosCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    LimpiarFiltrosAsync,
                    "limpiar los filtros de publicaciones"),
                PuedeEjecutarAccionListado);

            RefrescarCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    RefrescarAsync,
                    "actualizar las publicaciones"),
                PuedeEjecutarAccionListado);

            PaginaAnteriorCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    () => IrPaginaAsync(PaginaActual - 1),
                    "cargar la página anterior de publicaciones"),
                () =>
                    PuedeEjecutarAccionListado() &&
                    PuedeIrAnterior);

            PaginaSiguienteCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    () => IrPaginaAsync(PaginaActual + 1),
                    "cargar la página siguiente de publicaciones"),
                () =>
                    PuedeEjecutarAccionListado() &&
                    PuedeIrSiguiente);

            /*
             * Los comandos de navegación pueden ejecutarse aunque exista una
             * carga en curso. La carga se cancela antes de cambiar de página.
             */
            NuevaCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    NuevaAsync,
                    "abrir una nueva publicación"),
                () =>
                    !Navegando &&
                    !EjecutandoOperacion &&
                    CanView &&
                    CanAdd);

            EditarCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await EjecutarComandoSeguroAsync(
                        () => EditarAsync(item),
                        "editar la publicación"),
                    item =>
                        item != null &&
                        !Navegando &&
                        !EjecutandoOperacion &&
                        CanView &&
                        CanEdit);

            CambiarEstadoCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await EjecutarComandoSeguroAsync(
                        () => CambiarEstadoAsync(item),
                        "cambiar el estado de la publicación"),
                    item =>
                        item != null &&
                        !IsBusy &&
                        !IsRefreshing &&
                        !Navegando &&
                        !EjecutandoOperacion &&
                        CanView &&
                        CanEdit);

            CambiarDestacadaCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await EjecutarComandoSeguroAsync(
                        () => CambiarDestacadaAsync(item),
                        "actualizar el destacado de la publicación"),
                    item =>
                        item != null &&
                        !IsBusy &&
                        !IsRefreshing &&
                        !Navegando &&
                        !EjecutandoOperacion &&
                        CanView &&
                        CanEdit);

            EliminarCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await EjecutarComandoSeguroAsync(
                        () => EliminarAsync(item),
                        "eliminar la publicación"),
                    item =>
                        item != null &&
                        !IsBusy &&
                        !IsRefreshing &&
                        !Navegando &&
                        !EjecutandoOperacion &&
                        CanView &&
                        CanDelete);

            RegresarCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    RegresarAsync,
                    "regresar al centro de noticias"),
                () => !Navegando && !EjecutandoOperacion);
        }

        public ObservableCollection<CategoriaPublicacionResponse>
            Categorias { get; }

        public ObservableCollection<string> Estados { get; }

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

        public string EstadoSeleccionado
        {
            get => estadoSeleccionado;
            set
            {
                string nuevoValor = string.IsNullOrWhiteSpace(value)
                    ? "TODOS"
                    : value;

                if (estadoSeleccionado == nuevoValor)
                    return;

                estadoSeleccionado = nuevoValor;
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

        public new bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                if (base.IsBusy == value)
                    return;

                base.IsBusy = value;
                ActualizarComandos();
                NotificarLista();
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

        private bool EjecutandoOperacion
        {
            get => ejecutandoOperacion;
            set
            {
                if (ejecutandoOperacion == value)
                    return;

                ejecutandoOperacion = value;
                ActualizarComandos();
            }
        }

        public bool TienePublicaciones =>
            Publicaciones.Count > 0;

        public bool MostrarVacio =>
            CanAdministrar &&
            cargado &&
            !TienePublicaciones &&
            !IsBusy &&
            !TieneMensaje;

        public bool SeHaListado => cargado;

        public bool UltimaCargaExitosa => ultimaCargaExitosa;

        public bool RequiereRecargaPorCambios =>
            cargado &&
            PublicacionListadoEstadoService
                .HayCambiosDesde(versionAplicada);

        public int PaginaActual =>
            Math.Max(1, paginaActual);

        public int TotalPaginas =>
            Math.Max(1, totalPaginas);

        public bool PuedeIrAnterior =>
            cargado && PaginaActual > 1;

        public bool PuedeIrSiguiente =>
            cargado && PaginaActual < TotalPaginas;

        public bool MostrarPaginacion =>
            CanAdministrar &&
            cargado &&
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

                int inicio =
                    ((PaginaActual - 1) *
                     Math.Max(1, tamanoPaginaActual)) + 1;

                int fin =
                    Math.Min(
                        inicio + Publicaciones.Count - 1,
                        totalRegistros);

                return $"Mostrando {inicio}-{fin} de {totalRegistros}";
            }
        }

        public string TotalTexto =>
            totalRegistros == 1
                ? "1 publicación"
                : $"{totalRegistros} publicaciones";

        public bool CanAdministrar =>
            CanView &&
            (CanAdd || CanEdit || CanDelete);

        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }
        public Command NuevaCommand { get; }
        public Command<PublicacionListadoResponse> EditarCommand { get; }
        public Command<PublicacionListadoResponse> CambiarEstadoCommand { get; }
        public Command<PublicacionListadoResponse> CambiarDestacadaCommand { get; }
        public Command<PublicacionListadoResponse> EliminarCommand { get; }
        public Command RegresarCommand { get; }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("noticiasPage");
            OnPropertyChanged(nameof(CanAdministrar));
            NotificarLista();
            ActualizarComandos();
        }

        /// <summary>
        /// Reinicia solamente el estado de una visita administrativa nueva.
        /// Crear, editar y la papelera no ejecutan este reinicio al regresar.
        /// </summary>
        public async Task IniciarNuevaVisitaAsync()
        {
            if (!CanAdministrar || Navegando)
                return;

            CancelarCarga();

            cargado = false;
            ultimaCargaExitosa = false;
            categoriasCargadas = false;
            versionAplicada = -1;
            paginaActual = 1;
            totalPaginas = 1;
            totalRegistros = 0;
            tamanoPaginaActual = ObtenerTamanoPagina();
            Mensaje = string.Empty;

            textoBusqueda = string.Empty;
            estadoSeleccionado = "TODOS";
            categoriaSeleccionada = null;

            textoBusquedaAplicado = string.Empty;
            estadoAplicado = "TODOS";
            categoriaAplicadaId = null;

            Publicaciones.Clear();
            Categorias.Clear();

            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(EstadoSeleccionado));
            OnPropertyChanged(nameof(CategoriaSeleccionada));
            NotificarLista();

            await CargarInicialAsync();
        }

        /// <summary>
        /// Conserva compatibilidad con el ciclo de vida de la página. Dentro de
        /// una misma visita sólo recarga si una mutación cambió publicaciones.
        /// </summary>
        public async Task InicializarAsync()
        {
            if (!CanAdministrar || IsBusy || Navegando)
                return;

            if (!cargado)
            {
                await CargarInicialAsync();
                return;
            }

            if (RequiereRecargaPorCambios)
                await RecargarPaginaActualAsync();
        }

        public Task RecargarPaginaActualAsync() =>
            CargarPaginaAsync(
                PaginaActual,
                mostrarBloqueo: true);

        /// <summary>
        /// Cancela la petición activa sin convertir la navegación interna en
        /// una visita nueva. La tarea propietaria dispone su token al finalizar.
        /// </summary>
        public void CancelarCarga()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(
                    ref cargaCancellationTokenSource,
                    null);

            CancelarSeguro(source);

            if (!EjecutandoOperacion)
                IsBusy = false;

            IsRefreshing = false;
        }

        private bool PuedeEjecutarAccionListado() =>
            !IsBusy &&
            !IsRefreshing &&
            !Navegando &&
            !EjecutandoOperacion &&
            CanAdministrar;

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

            estadoAplicado =
                string.IsNullOrWhiteSpace(EstadoSeleccionado)
                    ? "TODOS"
                    : EstadoSeleccionado.Trim();

            textoBusquedaAplicado =
                TextoBusqueda.Trim();
        }

        private async Task LimpiarFiltrosAsync()
        {
            textoBusqueda = string.Empty;
            estadoSeleccionado = "TODOS";
            categoriaSeleccionada =
                Categorias.FirstOrDefault();

            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(EstadoSeleccionado));
            OnPropertyChanged(nameof(CategoriaSeleccionada));

            textoBusquedaAplicado = string.Empty;
            estadoAplicado = "TODOS";
            categoriaAplicadaId = null;

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
                PrepararCarga();

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
                    publicacionesTask =
                        ConsultarPaginaAsync(
                            1,
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
                // Cierre esperado del stream durante navegación rápida.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaActual(source))
                {
                    Mensaje =
                        "No fue posible cargar las publicaciones.";

                    await MostrarErrorInesperadoAsync(
                        "cargar las publicaciones",
                        ex);
                }
            }
            finally
            {
                if (EsCargaActual(source))
                    IsBusy = false;

                LiberarCarga(source);
                ActualizarComandos();
                NotificarLista();
            }
        }

        private async Task CargarPaginaAsync(
            int pagina,
            bool mostrarBloqueo)
        {
            if (!CanAdministrar || Navegando)
                return;

            if (IsBusy ||
                (!IsRefreshing && EjecutandoOperacion))
            {
                return;
            }

            CancellationTokenSource source =
                PrepararCarga();

            try
            {
                ultimaCargaExitosa = false;
                Mensaje = string.Empty;

                if (mostrarBloqueo)
                    IsBusy = true;

                ApiResult<PublicacionPaginadaResponse> result =
                    await ConsultarPaginaAsync(
                        Math.Max(1, pagina),
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

                int totalDisponible =
                    Math.Max(1, paginaRecibida.TotalPaginas);

                /*
                 * Si una mutación redujo el total y la página actual dejó de
                 * existir, se consulta una sola vez la última página válida.
                 */
                if (paginaRecibida.TotalRegistros > 0 &&
                    paginaRecibida.Pagina > totalDisponible)
                {
                    ApiResult<PublicacionPaginadaResponse> normalizado =
                        await ConsultarPaginaAsync(
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
                // El stream se cerró porque la página cambió.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaActual(source))
                {
                    Mensaje =
                        "No fue posible cargar las publicaciones.";

                    await MostrarErrorInesperadoAsync(
                        "cargar las publicaciones",
                        ex);
                }
            }
            finally
            {
                if (EsCargaActual(source) && mostrarBloqueo)
                    IsBusy = false;

                LiberarCarga(source);
                ActualizarComandos();
                NotificarLista();
            }
        }

        private Task<ApiResult<PublicacionPaginadaResponse>>
            ConsultarPaginaAsync(
                int pagina,
                CancellationToken cancellationToken) =>
            apiService.GetAdministracionAsync(
                categoriaAplicadaId,
                estadoAplicado,
                textoBusquedaAplicado,
                Math.Max(1, pagina),
                ObtenerTamanoPagina(),
                cancellationToken);

        private void AplicarCategorias(
            IEnumerable<CategoriaPublicacionResponse> items)
        {
            List<CategoriaPublicacionResponse> categorias =
                items
                    .OrderBy(x => x.Orden)
                    .ToList();

            int? seleccionAnterior =
                CategoriaSeleccionada?.CategoriaPublicacionId;

            Categorias.Clear();
            Categorias.Add(
                CategoriaPublicacionResponse.Todas());

            foreach (CategoriaPublicacionResponse categoria
                     in categorias)
            {
                Categorias.Add(categoria);
            }

            CategoriaSeleccionada =
                Categorias.FirstOrDefault(x =>
                    x.CategoriaPublicacionId == seleccionAnterior)
                ?? Categorias.FirstOrDefault();

            categoriasCargadas = true;

            PublicacionesAdminVisitaService
                .GuardarCategorias(categorias);
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

            totalPaginas = Math.Max(1, pagina.TotalPaginas);
            totalRegistros = Math.Max(0, pagina.TotalRegistros);
            tamanoPaginaActual =
                pagina.TamanoPagina > 0
                    ? pagina.TamanoPagina
                    : ObtenerTamanoPagina();

            paginaActual = totalRegistros == 0
                ? 1
                : Math.Clamp(
                    Math.Max(1, pagina.Pagina),
                    1,
                    totalPaginas);

            cargado = true;
            ultimaCargaExitosa = true;
            versionAplicada =
                PublicacionListadoEstadoService.VersionActual;

            Mensaje = string.Empty;
            NotificarLista();
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
                ? 16
                : 8;

        private CancellationTokenSource PrepararCarga()
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
                // La tarea propietaria terminó y dispuso el token primero.
            }
        }

        private async Task NuevaAsync()
        {
            if (!CanView || !CanAdd || Navegando || EjecutandoOperacion)
                return;

            await NavegarSeguroAsync(
                AppRoutes.PublicacionFormulario,
                new Dictionary<string, object>
                {
                    ["PublicacionId"] = 0
                });
        }

        private async Task EditarAsync(
            PublicacionListadoResponse? item)
        {
            if (!CanView ||
                !CanEdit ||
                item == null ||
                Navegando ||
                EjecutandoOperacion)
            {
                return;
            }

            await NavegarSeguroAsync(
                AppRoutes.PublicacionFormulario,
                new Dictionary<string, object>
                {
                    ["PublicacionId"] = item.PublicacionId
                });
        }

        private Task RegresarAsync() =>
            NavegarSeguroAsync(AppRoutes.Regresar);

        private async Task NavegarSeguroAsync(
            string ruta,
            IDictionary<string, object>? parametros = null)
        {
            if (Navegando)
                return;

            Navegando = true;

            try
            {
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

        private async Task CambiarEstadoAsync(
            PublicacionListadoResponse? item)
        {
            if (!CanView ||
                !CanEdit ||
                item == null ||
                IsBusy ||
                IsRefreshing ||
                Navegando ||
                EjecutandoOperacion)
            {
                return;
            }

            string nuevoEstado = string.Equals(
                item.EstadoPublicacion,
                "PUBLICADA",
                StringComparison.OrdinalIgnoreCase)
                ? "ARCHIVADA"
                : "PUBLICADA";

            bool confirmar = await ConfirmarAsync(
                nuevoEstado == "PUBLICADA"
                    ? "Publicar"
                    : "Archivar",
                nuevoEstado == "PUBLICADA"
                    ? $"¿Desea publicar “{item.Titulo}”?"
                    : $"¿Desea archivar “{item.Titulo}”?",
                nuevoEstado == "PUBLICADA"
                    ? "Publicar"
                    : "Archivar",
                "Cancelar");

            if (!confirmar)
                return;

            bool actualizado = false;
            string mensajeExito = string.Empty;

            try
            {
                EjecutandoOperacion = true;
                IsBusy = true;

                ApiResult<bool> result =
                    await apiService.CambiarEstadoAsync(
                        item.PublicacionId,
                        nuevoEstado);

                if (!result.Success)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                mensajeExito = result.Message;
                PublicacionListadoEstadoService.MarcarActualizacion();
                actualizado = true;
            }
            finally
            {
                IsBusy = false;
                EjecutandoOperacion = false;
            }

            if (!actualizado)
                return;

            await RecargarPaginaActualAsync();
            await MostrarExitoAsync(mensajeExito);
        }

        private async Task CambiarDestacadaAsync(
            PublicacionListadoResponse? item)
        {
            if (!CanView ||
                !CanEdit ||
                item == null ||
                IsBusy ||
                IsRefreshing ||
                Navegando ||
                EjecutandoOperacion)
            {
                return;
            }

            bool actualizado = false;
            string mensajeExito = string.Empty;

            try
            {
                EjecutandoOperacion = true;
                IsBusy = true;

                ApiResult<bool> result =
                    await apiService.CambiarDestacadaAsync(
                        item.PublicacionId,
                        !item.Destacada);

                if (!result.Success)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                mensajeExito = result.Message;
                PublicacionListadoEstadoService.MarcarActualizacion();
                actualizado = true;
            }
            finally
            {
                IsBusy = false;
                EjecutandoOperacion = false;
            }

            if (!actualizado)
                return;

            await RecargarPaginaActualAsync();
            await MostrarExitoAsync(mensajeExito);
        }

        private async Task EliminarAsync(
            PublicacionListadoResponse? item)
        {
            if (!CanView ||
                !CanDelete ||
                item == null ||
                IsBusy ||
                IsRefreshing ||
                Navegando ||
                EjecutandoOperacion)
            {
                return;
            }

            bool confirmar = await ConfirmarEliminacionAsync(
                $"la publicación “{item.Titulo}”");

            if (!confirmar)
                return;

            bool eliminado = false;
            string mensajeExito = string.Empty;

            try
            {
                EjecutandoOperacion = true;
                IsBusy = true;

                ApiResult<bool> result =
                    await apiService.EliminarAsync(
                        item.PublicacionId);

                if (!result.Success)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                mensajeExito = result.Message;
                PublicacionListadoEstadoService.MarcarActualizacion();
                eliminado = true;
            }
            finally
            {
                IsBusy = false;
                EjecutandoOperacion = false;
            }

            if (!eliminado)
                return;

            await RecargarPaginaActualAsync();
            await MostrarExitoAsync(mensajeExito);
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

        private void NotificarLista()
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
            OnPropertyChanged(nameof(CanAdministrar));
        }

        private void ActualizarComandos()
        {
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            PaginaAnteriorCommand.ChangeCanExecute();
            PaginaSiguienteCommand.ChangeCanExecute();
            NuevaCommand.ChangeCanExecute();
            EditarCommand.ChangeCanExecute();
            CambiarEstadoCommand.ChangeCanExecute();
            CambiarDestacadaCommand.ChangeCanExecute();
            EliminarCommand.ChangeCanExecute();
            RegresarCommand.ChangeCanExecute();
        }
    }
}
