using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    [QueryProperty(
        nameof(ModoSeleccionTexto),
        "ModoSeleccion")]
    public sealed class PropietariosViewModel : GlobalService
    {
        private readonly PropietarioApiService service = new();
        private readonly PropietarioCrudApiService crudService = new();

        private CancellationTokenSource? cargaCts;
        private string textoBusqueda = string.Empty;
        private string textoBusquedaAplicado = string.Empty;
        private string? modoSeleccionTexto;
        private bool pantallaCargada;
        private bool isRefreshing;
        private bool navegando;
        private bool mostrandoRelay;
        private string tituloRelay = "Procesando...";
        private string detalleRelay = "Espere un momento.";
        private int paginaActual = 1;
        private int totalPaginas = 1;
        private int totalRegistros;
        private int tamanoPaginaActual;

        public PropietariosViewModel()
        {
            tamanoPaginaActual =
                ObtenerTamanoPagina();

            BuscarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        AplicarBusquedaAsync,
                        "buscar propietarios"),
                    () =>
                        PuedeConsultarListado &&
                        !IsBusy &&
                        !Navegando);

            LimpiarFiltrosCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        LimpiarFiltrosAsync,
                        "limpiar la búsqueda"),
                    () =>
                        PuedeConsultarListado &&
                        !IsBusy &&
                        !Navegando);

            ActualizarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        RefrescarAsync,
                        "actualizar propietarios"),
                    () =>
                        PuedeConsultarListado &&
                        !IsBusy &&
                        !Navegando);

            PaginaAnteriorCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        IrPaginaAnteriorAsync,
                        "cargar la página anterior"),
                    () =>
                        PuedeConsultarListado &&
                        PuedeIrAnterior &&
                        !IsBusy &&
                        !Navegando);

            PaginaSiguienteCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        IrPaginaSiguienteAsync,
                        "cargar la página siguiente"),
                    () =>
                        PuedeConsultarListado &&
                        PuedeIrSiguiente &&
                        !IsBusy &&
                        !Navegando);

            RegresarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        RegresarAsync,
                        "regresar"),
                    () =>
                        !IsBusy &&
                        !Navegando);

            NuevoCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        NuevoAsync,
                        "abrir el formulario de propietario"),
                    () =>
                        CanAdd &&
                        !IsBusy &&
                        !Navegando);

            AbrirCommand =
                new Command<PropietarioResponse>(
                    async propietario =>
                        await EjecutarSeguroAsync(
                            () => AbrirAsync(propietario),
                            "abrir el propietario"),
                    propietario =>
                        propietario != null &&
                        !IsBusy &&
                        !Navegando);

            VerCommand =
                new Command<PropietarioResponse>(
                    async propietario =>
                        await EjecutarSeguroAsync(
                            () => VerAsync(propietario),
                            "consultar el propietario"),
                    propietario =>
                        propietario != null &&
                        CanView &&
                        !EsModoSeleccion &&
                        !IsBusy &&
                        !Navegando);

            EditarCommand =
                new Command<PropietarioResponse>(
                    async propietario =>
                        await EjecutarSeguroAsync(
                            () => EditarAsync(propietario),
                            "editar el propietario"),
                    propietario =>
                        propietario != null &&
                        propietario.Activo &&
                        CanEdit &&
                        !EsModoSeleccion &&
                        !IsBusy &&
                        !Navegando);

            EliminarCommand =
                new Command<PropietarioResponse>(
                    async propietario =>
                        await EjecutarSeguroAsync(
                            () => EliminarAsync(propietario),
                            "eliminar el propietario"),
                    propietario =>
                        propietario != null &&
                        propietario.Activo &&
                        CanDelete &&
                        !EsModoSeleccion &&
                        !IsBusy &&
                        !Navegando);

            VerTerrenosCommand =
                new Command<PropietarioResponse>(
                    async propietario =>
                        await EjecutarSeguroAsync(
                            () => VerTerrenosAsync(propietario),
                            "consultar los terrenos del propietario"),
                    propietario =>
                        propietario != null &&
                        propietario.TotalTerrenos > 0 &&
                        CanView &&
                        !EsModoSeleccion &&
                        !IsBusy &&
                        !Navegando);
        }

        public ObservableCollection<PropietarioResponse>
            Propietarios { get; } = new();

        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command ActualizarCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }
        public Command RegresarCommand { get; }
        public Command NuevoCommand { get; }
        public Command<PropietarioResponse> AbrirCommand { get; }
        public Command<PropietarioResponse> VerCommand { get; }
        public Command<PropietarioResponse> EditarCommand { get; }
        public Command<PropietarioResponse> EliminarCommand { get; }
        public Command<PropietarioResponse> VerTerrenosCommand { get; }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                string nuevo =
                    value ??
                    string.Empty;

                if (textoBusqueda == nuevo)
                    return;

                textoBusqueda = nuevo;
                OnPropertyChanged();
            }
        }

        public string? ModoSeleccionTexto
        {
            get => modoSeleccionTexto;
            set
            {
                if (modoSeleccionTexto == value)
                    return;

                modoSeleccionTexto = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(EsModoSeleccion));
                OnPropertyChanged(nameof(MostrarAccionesAdministracion));
                OnPropertyChanged(nameof(Titulo));
                OnPropertyChanged(nameof(TextoRegresar));
                OnPropertyChanged(nameof(PuedeConsultarListado));
                OnPropertyChanged(nameof(PuedeMostrarContenido));
                OnPropertyChanged(nameof(MostrarAccesoDenegado));

                ActualizarComandos();
                ActualizarEstadoLista();
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

        public bool MostrandoRelay
        {
            get => mostrandoRelay;
            private set
            {
                if (mostrandoRelay == value)
                    return;

                mostrandoRelay = value;
                OnPropertyChanged();
            }
        }

        public string TituloRelay
        {
            get => tituloRelay;
            private set
            {
                string nuevo =
                    value ??
                    string.Empty;

                if (tituloRelay == nuevo)
                    return;

                tituloRelay = nuevo;
                OnPropertyChanged();
            }
        }

        public string DetalleRelay
        {
            get => detalleRelay;
            private set
            {
                string nuevo =
                    value ??
                    string.Empty;

                if (detalleRelay == nuevo)
                    return;

                detalleRelay = nuevo;
                OnPropertyChanged();
            }
        }

        public bool EsModoSeleccion =>
            bool.TryParse(
                ModoSeleccionTexto,
                out bool valor) &&
            valor;

        public bool MostrarAccionesAdministracion =>
            !EsModoSeleccion;

        public string Titulo =>
            EsModoSeleccion
                ? "Seleccionar propietario"
                : "Propietarios";

        public string TextoRegresar =>
            EsModoSeleccion
                ? "Cancelar selección"
                : "Configuración";

        public new bool CanView =>
            PermissionService.Instance.HasRead(
                InterfazCodigos.Propietarios);

        public new bool CanAdd =>
            PermissionService.Instance.HasAdd(
                InterfazCodigos.Propietarios);

        public new bool CanEdit =>
            PermissionService.Instance.HasUpdate(
                InterfazCodigos.Propietarios);

        public new bool CanDelete =>
            PermissionService.Instance.HasDelete(
                InterfazCodigos.Propietarios);

        /*
         * El selector de Terrenos utiliza un endpoint específico y no exige
         * permiso administrativo de lectura de Propietarios.
         */
        public bool PuedeConsultarListado =>
            EsModoSeleccion ||
            CanView;

        public bool PuedeMostrarContenido =>
            PuedeConsultarListado;

        public bool MostrarAccesoDenegado =>
            !EsModoSeleccion &&
            !CanView;

        public int TotalRegistros
        {
            get => totalRegistros;
            private set
            {
                if (totalRegistros == value)
                    return;

                totalRegistros = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResumenResultados));
                OnPropertyChanged(nameof(RangoPaginaTexto));
                OnPropertyChanged(nameof(MostrarPaginacion));
            }
        }

        public int PaginaActual =>
            paginaActual;

        public int TotalPaginas =>
            totalPaginas;

        public bool PuedeIrAnterior =>
            pantallaCargada &&
            paginaActual > 1;

        public bool PuedeIrSiguiente =>
            pantallaCargada &&
            paginaActual < totalPaginas;

        public bool MostrarPaginacion =>
            PuedeConsultarListado &&
            pantallaCargada &&
            Propietarios.Count > 0;

        public string PaginaTexto =>
            $"Página {Math.Max(1, paginaActual)} de {Math.Max(1, totalPaginas)}";

        public string RangoPaginaTexto
        {
            get
            {
                if (TotalRegistros <= 0 ||
                    Propietarios.Count == 0)
                {
                    return
                        "Sin registros en esta página";
                }

                int inicio =
                    ((Math.Max(
                        1,
                        paginaActual) - 1) *
                     Math.Max(
                         1,
                         tamanoPaginaActual)) + 1;

                int fin =
                    Math.Min(
                        inicio +
                        Propietarios.Count - 1,
                        TotalRegistros);

                return
                    $"Mostrando {inicio}-{fin} de {TotalRegistros}";
            }
        }

        public string ResumenResultados =>
            TotalRegistros == 1
                ? "1 propietario encontrado"
                : $"{TotalRegistros:N0} propietarios encontrados";

        public bool MostrarListaVacia =>
            PuedeConsultarListado &&
            pantallaCargada &&
            !IsBusy &&
            Propietarios.Count == 0;

        public bool TienePaginaCargada =>
            pantallaCargada;

        public void ActualizarPermisos()
        {
            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanAdd));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanDelete));
            OnPropertyChanged(nameof(PuedeConsultarListado));
            OnPropertyChanged(nameof(PuedeMostrarContenido));
            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            OnPropertyChanged(nameof(MostrarAccionesAdministracion));

            ActualizarComandos();
            ActualizarEstadoLista();
        }

        /// <summary>
        /// Entrada desde otro módulo: limpia filtros y datos anteriores y
        /// consulta únicamente la primera página del servidor.
        /// </summary>
        public async Task IniciarNuevaVisitaAsync()
        {
            if (!PuedeConsultarListado ||
                Navegando)
            {
                return;
            }

            CancelarCarga();

            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;
            paginaActual = 1;
            totalPaginas = 1;
            TotalRegistros = 0;
            tamanoPaginaActual = ObtenerTamanoPagina();
            pantallaCargada = false;

            Propietarios.Clear();
            ActualizarEstadoLista();

            await CargarPaginaAsync(
                1,
                true,
                "Cargando propietarios...",
                "Consultando información actual del servidor");
        }

        public Task InicializarAsync() =>
            pantallaCargada
                ? Task.CompletedTask
                : CargarPaginaAsync(
                    1,
                    true,
                    "Cargando propietarios...",
                    "Consultando información actual del servidor");

        public Task RecargarPaginaActualAsync() =>
            CargarPaginaAsync(
                Math.Max(
                    1,
                    paginaActual),
                false,
                "Actualizando propietarios...",
                "Aplicando los cambios realizados dentro del módulo");

        /// <summary>
        /// Aplica un alta/edición confirmada por el servidor sin consultar
        /// nuevamente cuando la página visible no puede cambiar de composición.
        /// Devuelve true únicamente si hace falta un GET.
        /// </summary>
        public bool AplicarMutacionPendiente(
            PropietarioMutacionListado mutacion)
        {
            if (mutacion == null ||
                mutacion.Actual.PropietarioId <= 0 ||
                !pantallaCargada)
            {
                return true;
            }

            return mutacion.Tipo switch
            {
                PropietarioMutacionListadoTipo.Actualizado =>
                    !AplicarActualizacionLocal(
                        mutacion),

                PropietarioMutacionListadoTipo.Creado =>
                    !AplicarCreacionLocal(
                        mutacion.Actual),

                _ =>
                    true
            };
        }

        public void CancelarCarga()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(
                    ref cargaCts,
                    null);

            CancelarSeguro(source);

            IsBusy = false;
            IsRefreshing = false;
            OcultarRelay();

            ActualizarComandos();
            ActualizarEstadoLista();
        }

        private async Task AplicarBusquedaAsync()
        {
            textoBusquedaAplicado =
                (TextoBusqueda ??
                 string.Empty)
                    .Trim();

            await CargarPaginaAsync(
                1,
                false,
                "Buscando propietarios...",
                "Consultando los registros que coinciden con la búsqueda");
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;

            await CargarPaginaAsync(
                1,
                false,
                "Actualizando propietarios...",
                "Quitando filtros y consultando la primera página");
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;

            try
            {
                await CargarPaginaAsync(
                    Math.Max(
                        1,
                        paginaActual),
                    false,
                    "Actualizando propietarios...",
                    "Consultando nuevamente la página actual");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task IrPaginaAnteriorAsync()
        {
            if (!PuedeIrAnterior)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                paginaActual - 1,
                false,
                "Cargando página anterior...",
                "Consultando la página anterior de propietarios");
        }

        private Task IrPaginaSiguienteAsync()
        {
            if (!PuedeIrSiguiente)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                paginaActual + 1,
                false,
                "Cargando página siguiente...",
                "Consultando la siguiente página de propietarios");
        }

        private async Task CargarPaginaAsync(
            int paginaSolicitada,
            bool cargaInicial = false,
            string? tituloOperacion = null,
            string? detalleOperacion = null)
        {
            if (!PuedeConsultarListado ||
                Navegando)
            {
                return;
            }

            paginaSolicitada =
                Math.Max(
                    1,
                    paginaSolicitada);

            CancellationTokenSource source =
                PrepararCarga();

            try
            {
                MostrarRelay(
                    tituloOperacion ??
                        (cargaInicial
                            ? "Cargando propietarios..."
                            : "Actualizando propietarios..."),
                    detalleOperacion ??
                        "Consultando información actual del servidor");

                IsBusy = true;
                ActualizarComandos();

                ApiResult<PropietarioPaginaResponse> result =
                    await service.BuscarPaginadoAsync(
                        textoBusquedaAplicado,
                        incluirInactivos: false,
                        paraSeleccionTerreno: EsModoSeleccion,
                        pagina: paginaSolicitada,
                        tamanoPagina: ObtenerTamanoPagina(),
                        cancellationToken: source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source))
                {
                    return;
                }

                if (!result.Success ||
                    result.Data == null)
                {
                    if (!EsCancelacion(
                            result.Message))
                    {
                        await MostrarErrorAsync(
                            result.Message);
                    }

                    return;
                }

                PropietarioPaginaResponse pagina =
                    result.Data;

                int paginasServidor =
                    Math.Max(
                        1,
                        pagina.TotalPaginas);

                if (paginaSolicitada >
                        paginasServidor &&
                    pagina.TotalRegistros > 0)
                {
                    result =
                        await service.BuscarPaginadoAsync(
                            textoBusquedaAplicado,
                            incluirInactivos: false,
                            paraSeleccionTerreno: EsModoSeleccion,
                            pagina: paginasServidor,
                            tamanoPagina: ObtenerTamanoPagina(),
                            cancellationToken: source.Token);

                    if (source.IsCancellationRequested ||
                        !EsCargaActual(source))
                    {
                        return;
                    }

                    if (!result.Success ||
                        result.Data == null)
                    {
                        if (!EsCancelacion(
                                result.Message))
                        {
                            await MostrarErrorAsync(
                                result.Message);
                        }

                        return;
                    }

                    pagina = result.Data;
                }

                AplicarPagina(pagina);
                pantallaCargada = true;
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al navegar o reemplazar una consulta.
            }
            catch (ObjectDisposedException)
            {
                // La solicitud terminó mientras se abandonaba la pantalla.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaActual(source))
                {
                    await MostrarErrorInesperadoAsync(
                        "cargar los propietarios",
                        ex);
                }
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    IsBusy = false;
                    IsRefreshing = false;
                    OcultarRelay();
                }

                LiberarCarga(source);
                ActualizarComandos();
                ActualizarEstadoLista();
            }
        }

        /// <summary>
        /// La colección contiene exclusivamente la página visible.
        /// </summary>
        private void AplicarPagina(
            PropietarioPaginaResponse pagina)
        {
            Propietarios.Clear();

            foreach (PropietarioResponse item
                     in pagina.Items)
            {
                if (item.PropietarioId > 0 &&
                    item.Activo)
                {
                    Propietarios.Add(item);
                }
            }

            paginaActual =
                Math.Max(
                    1,
                    pagina.Pagina);

            totalPaginas =
                Math.Max(
                    1,
                    pagina.TotalPaginas);

            tamanoPaginaActual =
                pagina.TamanoPagina > 0
                    ? pagina.TamanoPagina
                    : ObtenerTamanoPagina();

            TotalRegistros =
                Math.Max(
                    0,
                    pagina.TotalRegistros);

            ActualizarEstadoLista();
        }

        private async Task RegresarAsync()
        {
            if (IsBusy ||
                Navegando)
            {
                return;
            }

            if (EsModoSeleccion)
            {
                await NavegarAsync(
                    "..",
                    null,
                    "Regresando...",
                    "Cancelando la selección de propietario");
                return;
            }

            await NavegarAsync(
                AppRoutes.Configuracion,
                null,
                "Regresando...",
                "Volviendo a Configuración");
        }

        private async Task NuevoAsync()
        {
            if (!CanAdd)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para crear propietarios.");
                return;
            }

            await NavegarAsync(
                AppRoutes.PropietarioFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] =
                        FormMode.FormModeSelect.Create,
                    ["ModoSeleccion"] =
                        EsModoSeleccion.ToString()
                },
                "Abriendo propietario...",
                "Preparando el formulario de creación");
        }

        private async Task AbrirAsync(
            PropietarioResponse? propietario)
        {
            if (propietario == null)
                return;

            if (EsModoSeleccion)
            {
                if (!propietario.Activo)
                {
                    await MostrarAdvertenciaAsync(
                        "No puede asignar un propietario eliminado.");
                    return;
                }

                PropietarioSeleccionService.Seleccionar(
                    propietario);

                await NavegarAsync(
                    "..",
                    null,
                    "Asignando propietario...",
                    "Regresando al formulario de terreno");

                return;
            }

            await VerAsync(propietario);
        }

        /// <summary>
        /// Ver reutiliza directamente el DTO completo de la página visible.
        /// No ejecuta GET por ID.
        /// </summary>
        private async Task VerAsync(
            PropietarioResponse? propietario)
        {
            if (propietario == null)
                return;

            if (!CanView)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para visualizar propietarios.");
                return;
            }

            await NavegarAsync(
                AppRoutes.PropietarioFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] =
                        FormMode.FormModeSelect.View,
                    ["Propietario"] =
                        ClonarPropietario(
                            propietario),
                    ["ModoSeleccion"] =
                        "False"
                },
                "Abriendo propietario...",
                "Preparando la información para consulta");
        }

        /// <summary>
        /// Editar reutiliza directamente el DTO completo de la página visible.
        /// No ejecuta GET por ID.
        /// </summary>
        private async Task EditarAsync(
            PropietarioResponse? propietario)
        {
            if (propietario == null)
                return;

            if (!CanEdit)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para editar propietarios.");
                return;
            }

            if (!propietario.Activo)
            {
                await MostrarAdvertenciaAsync(
                    "El propietario ya no se encuentra activo.");
                return;
            }

            await NavegarAsync(
                AppRoutes.PropietarioFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] =
                        FormMode.FormModeSelect.Edit,
                    ["Propietario"] =
                        ClonarPropietario(
                            propietario),
                    ["ModoSeleccion"] =
                        "False"
                },
                "Abriendo propietario...",
                "Preparando la información para edición");
        }

        private async Task VerTerrenosAsync(
            PropietarioResponse? propietario)
        {
            if (propietario == null)
                return;

            if (!CanView)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para visualizar propietarios.");
                return;
            }

            if (propietario.TotalTerrenos <= 0)
            {
                await MostrarInformacionAsync(
                    "El propietario no tiene terrenos vinculados.");
                return;
            }

            await NavegarAsync(
                AppRoutes.PropietarioTerrenos,
                new Dictionary<string, object>
                {
                    ["Propietario"] =
                        propietario
                },
                "Abriendo terrenos...",
                "Consultando los terrenos vinculados");
        }

        private async Task EliminarAsync(
            PropietarioResponse? propietario)
        {
            if (propietario == null ||
                IsBusy)
            {
                return;
            }

            if (!CanDelete)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para eliminar propietarios.");
                return;
            }

            if (propietario.TotalTerrenos > 0)
            {
                await MostrarAdvertenciaAsync(
                    "No se puede eliminar el propietario porque tiene terrenos vinculados. " +
                    "Utilice Ver terrenos para reasignarlos antes de continuar.");
                return;
            }

            bool confirmar =
                await ConfirmarAsync(
                    "Eliminar propietario",
                    $"¿Desea eliminar a {propietario.TextoPrincipal}? " +
                    "El registro quedará disponible en Propietarios eliminados.",
                    "Eliminar",
                    "Cancelar");

            if (!confirmar)
                return;

            bool eliminado = false;

            try
            {
                MostrarRelay(
                    "Eliminando propietario...",
                    "Actualizando el estado en el servidor");

                IsBusy = true;
                ActualizarComandos();

                ApiResult<bool> resultado =
                    await crudService
                        .EliminarPropietarioResultAsync(
                            propietario.PropietarioId);

                if (!resultado.Success ||
                    resultado.Data != true)
                {
                    await MostrarErrorAsync(
                        resultado.Message);
                    return;
                }

                eliminado = true;

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(
                        resultado.Message)
                            ? "Propietario eliminado correctamente."
                            : resultado.Message);
            }
            finally
            {
                IsBusy = false;
                OcultarRelay();
                ActualizarComandos();
            }

            if (!eliminado)
                return;

            /*
             * Si es la última página no existe un registro posterior que deba
             * desplazarse para completar el hueco: se resuelve localmente.
             * En páginas intermedias se hace un único GET porque hay que traer
             * el primer registro de la página siguiente.
             */
            bool requiereRecarga =
                paginaActual <
                totalPaginas;

            if (!requiereRecarga)
            {
                int paginaAntesEliminar =
                    paginaActual;

                Propietarios.Remove(
                    propietario);

                TotalRegistros =
                    Math.Max(
                        0,
                        TotalRegistros - 1);

                RecalcularPaginasLocales();

                /*
                 * Si desapareció el único registro de una página posterior,
                 * necesitamos cargar la nueva última página. Se conserva la
                 * página anterior al recálculo porque éste puede reducirla.
                 */
                if (Propietarios.Count == 0 &&
                    TotalRegistros > 0 &&
                    paginaAntesEliminar > 1)
                {
                    requiereRecarga = true;
                }
                else
                {
                    ActualizarEstadoLista();
                }
            }

            if (requiereRecarga)
            {
                int paginaDestino =
                    Math.Min(
                        Math.Max(
                            1,
                            paginaActual),
                        Math.Max(
                            1,
                            totalPaginas));

                await CargarPaginaAsync(
                    paginaDestino,
                    false,
                    "Actualizando propietarios...",
                    "Ajustando la página después de la eliminación");
            }
        }

        private bool AplicarActualizacionLocal(
            PropietarioMutacionListado mutacion)
        {
            PropietarioResponse? anterior =
                mutacion.Anterior;

            if (anterior == null)
                return false;

            PropietarioResponse actual =
                mutacion.Actual;

            int indice =
                EncontrarIndice(
                    actual.PropietarioId);

            if (indice < 0)
                return false;

            /*
             * Nombre e identificación determinan el ORDER BY del backend.
             * Si cambian, la posición global puede cruzar páginas.
             */
            if (!Iguales(
                    anterior.NombreCompleto,
                    actual.NombreCompleto) ||
                !Iguales(
                    anterior.Identificacion,
                    actual.Identificacion))
            {
                return false;
            }

            /*
             * Con búsqueda activa, el correo también participa en el filtro.
             * Si alguno de los campos buscables cambia, se reconstruye una vez.
             */
            if (!string.IsNullOrWhiteSpace(
                    textoBusquedaAplicado) &&
                (!Iguales(
                    anterior.NombreCompleto,
                    actual.NombreCompleto) ||
                 !Iguales(
                    anterior.Identificacion,
                    actual.Identificacion) ||
                 !Iguales(
                    anterior.Correo,
                    actual.Correo)))
            {
                return false;
            }

            Propietarios[indice] =
                ClonarPropietario(
                    actual);

            ActualizarEstadoLista();
            return true;
        }

        private bool AplicarCreacionLocal(
            PropietarioResponse creado)
        {
            bool coincideFiltro =
                CoincideBusqueda(
                    creado,
                    textoBusquedaAplicado);

            /*
             * Si el nuevo propietario no coincide con la búsqueda aplicada, la
             * página filtrada no cambia y no hace falta ningún GET.
             */
            if (!coincideFiltro)
                return true;

            /*
             * Solo es completamente seguro insertar localmente cuando toda la
             * colección filtrada cabe en una única página conocida.
             */
            bool unicaPaginaCompletaEnMemoria =
                paginaActual == 1 &&
                totalPaginas <= 1 &&
                TotalRegistros <
                    Math.Max(
                        1,
                        tamanoPaginaActual);

            if (!unicaPaginaCompletaEnMemoria)
                return false;

            Propietarios.Add(
                ClonarPropietario(
                    creado));

            OrdenarPaginaLocal();

            TotalRegistros =
                TotalRegistros + 1;

            RecalcularPaginasLocales();
            ActualizarEstadoLista();

            return true;
        }

        private void OrdenarPaginaLocal()
        {
            List<PropietarioResponse> ordenados =
                Propietarios
                    .OrderBy(
                        item =>
                            item.NombreCompleto,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(
                        item =>
                            item.Identificacion,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(
                        item =>
                            item.PropietarioId)
                    .ToList();

            Propietarios.Clear();

            foreach (PropietarioResponse item
                     in ordenados)
            {
                Propietarios.Add(item);
            }
        }

        private void RecalcularPaginasLocales()
        {
            int tamano =
                Math.Max(
                    1,
                    tamanoPaginaActual);

            totalPaginas =
                TotalRegistros == 0
                    ? 1
                    : (int)Math.Ceiling(
                        TotalRegistros /
                        (double)tamano);

            paginaActual =
                Math.Min(
                    Math.Max(
                        1,
                        paginaActual),
                    Math.Max(
                        1,
                        totalPaginas));

            ActualizarEstadoLista();
        }

        private int EncontrarIndice(
            int propietarioId)
        {
            for (int i = 0;
                 i < Propietarios.Count;
                 i++)
            {
                if (Propietarios[i].PropietarioId ==
                    propietarioId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool CoincideBusqueda(
            PropietarioResponse item,
            string filtro)
        {
            if (string.IsNullOrWhiteSpace(
                    filtro))
            {
                return true;
            }

            return Contiene(
                       item.Identificacion,
                       filtro) ||
                   Contiene(
                       item.NombreCompleto,
                       filtro) ||
                   Contiene(
                       item.Correo,
                       filtro);
        }

        private static bool Contiene(
            string? valor,
            string filtro) =>
            (valor ??
             string.Empty)
                .Contains(
                    filtro,
                    StringComparison.OrdinalIgnoreCase);

        private static bool Iguales(
            string? izquierda,
            string? derecha) =>
            string.Equals(
                izquierda?.Trim() ??
                    string.Empty,
                derecha?.Trim() ??
                    string.Empty,
                StringComparison.Ordinal);

        private static PropietarioResponse
            ClonarPropietario(
                PropietarioResponse item)
        {
            return new PropietarioResponse
            {
                PropietarioId =
                    item.PropietarioId,

                Identificacion =
                    item.Identificacion,

                NombreCompleto =
                    item.NombreCompleto,

                Telefono =
                    item.Telefono,

                Correo =
                    item.Correo,

                Direccion =
                    item.Direccion,

                Activo =
                    item.Activo,

                FechaRegistroUtc =
                    item.FechaRegistroUtc,

                TotalTerrenos =
                    item.TotalTerrenos,

                UsuarioPortalId =
                    item.UsuarioPortalId,

                UsuarioPortal =
                    item.UsuarioPortal
            };
        }

        private async Task NavegarAsync(
            string ruta,
            IDictionary<string, object>? parametros,
            string titulo,
            string detalle)
        {
            if (Navegando)
                return;

            Navegando = true;

            try
            {
                CancelarCarga();
                MostrarRelay(
                    titulo,
                    detalle);

                await Task.Yield();

                if (parametros == null)
                {
                    await GoToAsyncParameters(
                        ruta);
                }
                else
                {
                    await GoToAsyncParameters(
                        ruta,
                        parametros);
                }
            }
            finally
            {
                OcultarRelay();
                Navegando = false;
            }
        }

        private async Task EjecutarSeguroAsync(
            Func<Task> accion,
            string descripcion)
        {
            try
            {
                await accion();
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    descripcion,
                    ex);
            }
        }

        private void MostrarRelay(
            string titulo,
            string detalle)
        {
            TituloRelay = titulo;
            DetalleRelay = detalle;
            MostrandoRelay = true;
        }

        private void OcultarRelay()
        {
            MostrandoRelay = false;
        }

        private void ActualizarComandos()
        {
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            PaginaAnteriorCommand.ChangeCanExecute();
            PaginaSiguienteCommand.ChangeCanExecute();
            RegresarCommand.ChangeCanExecute();
            NuevoCommand.ChangeCanExecute();
            AbrirCommand.ChangeCanExecute();
            VerCommand.ChangeCanExecute();
            EditarCommand.ChangeCanExecute();
            EliminarCommand.ChangeCanExecute();
            VerTerrenosCommand.ChangeCanExecute();
        }

        private void ActualizarEstadoLista()
        {
            OnPropertyChanged(nameof(MostrarListaVacia));
            OnPropertyChanged(nameof(PuedeIrAnterior));
            OnPropertyChanged(nameof(PuedeIrSiguiente));
            OnPropertyChanged(nameof(MostrarPaginacion));
            OnPropertyChanged(nameof(PaginaActual));
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(PaginaTexto));
            OnPropertyChanged(nameof(RangoPaginaTexto));
            OnPropertyChanged(nameof(ResumenResultados));
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Current.Platform ==
            DevicePlatform.WinUI
                ? 36
                : 16;

        private CancellationTokenSource PrepararCarga()
        {
            var source =
                new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref cargaCts,
                    source);

            CancelarSeguro(anterior);
            return source;
        }

        private bool EsCargaActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(
                    ref cargaCts),
                source);

        private void LiberarCarga(
            CancellationTokenSource source)
        {
            Interlocked.CompareExchange(
                ref cargaCts,
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
            }
            finally
            {
                source.Dispose();
            }
        }

        private static bool EsCancelacion(
            string? mensaje) =>
            !string.IsNullOrWhiteSpace(
                mensaje) &&
            mensaje.Contains(
                "cancel",
                StringComparison.OrdinalIgnoreCase);
    }
}
