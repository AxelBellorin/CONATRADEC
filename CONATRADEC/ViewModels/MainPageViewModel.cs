using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class MainPageViewModel : GlobalService
    {
        private const string AlcanceTodos = "Todos los análisis";
        private const string AlcancePropios = "Solo mis análisis";

        private readonly AnalisisListadoOptimizadoApiService
            listadoApiService = new();

        private readonly GuardarTodoApiService
            guardarTodoApiService = new();

        // Filtros escritos: pueden cambiar libremente sin ejecutar HTTP.
        private string textoBusqueda = string.Empty;
        private bool usarFiltroRangoFecha;
        private DateTime fechaDesde = DateTime.Today.AddDays(-6);
        private DateTime fechaHasta = DateTime.Today;
        private string alcanceListadoSeleccionado = AlcanceTodos;
        private UsuarioFiltroAnalisis? usuarioFiltroSeleccionado;

        // Filtros aplicados: son la fuente de verdad para refrescar y paginar.
        private string textoBusquedaAplicado = string.Empty;
        private bool usarFiltroRangoFechaAplicado;
        private DateTime? fechaDesdeAplicada;
        private DateTime? fechaHastaAplicada;
        private string alcanceListadoAplicado = AlcanceTodos;
        private int? usuarioFiltroAplicado;

        private string mensaje = string.Empty;
        private string errorRangoFecha = string.Empty;
        private bool esAdministrador;
        private bool seHaListado;
        private bool isRefreshing;
        private bool cargandoListado;
        private bool cargandoUsuarios;
        private bool usuariosCargados;
        private bool ultimaCargaExitosa;
        private int paginaActual = 1;
        private int totalPaginas = 1;
        private int totalRegistros;
        private int tamanoPaginaActual;

        private CancellationTokenSource? cargaCancellationTokenSource;
        private CancellationTokenSource? usuariosCancellationTokenSource;

        public MainPageViewModel()
        {
            AnalisisGuardados = new ObservableCollection<
                AnalisisGuardadoResumen>();

            UsuariosFiltro = new ObservableCollection<
                UsuarioFiltroAnalisis>();

            OpcionesAlcanceListado = new ObservableCollection<string>
            {
                AlcanceTodos,
                AlcancePropios
            };

            ListarCommand = new Command(
                async () => await AplicarFiltrosYBuscarAsync(),
                PuedeEjecutarInteraccionListado);

            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => PuedeEjecutarConsulta() && SeHaListado);

            BuscarCommand = new Command(
                async () => await AplicarFiltrosYBuscarAsync(),
                () => PuedeEjecutarInteraccionListado() && SeHaListado);

            PaginaAnteriorCommand = new Command(
                async () => await IrPaginaAsync(paginaActual - 1),
                () => PuedeEjecutarInteraccionListado() && PuedeIrAnterior);

            PaginaSiguienteCommand = new Command(
                async () => await IrPaginaAsync(paginaActual + 1),
                () => PuedeEjecutarInteraccionListado() && PuedeIrSiguiente);

            VisualizarCommand =
                new Command<AnalisisGuardadoResumen>(
                    async item => await VisualizarAsync(item),
                    item => !IsBusy && !CargandoListado && !IsRefreshing &&
                            item != null && CanView);

            EditarCommand =
                new Command<AnalisisGuardadoResumen>(
                    async item => await EditarAsync(item),
                    item => !IsBusy && !CargandoListado && !IsRefreshing &&
                            item != null && CanEdit);

            EliminarCommand =
                new Command<AnalisisGuardadoResumen>(
                    async item => await EliminarAsync(item),
                    item => !IsBusy && !CargandoListado && !IsRefreshing &&
                            item != null && CanDelete);

            LimpiarFiltrosCommand = new Command(
                async () => await LimpiarFiltrosAsync(),
                () => PuedeEjecutarInteraccionListado() && SeHaListado);

            NuevoAnalisisCommand = new Command(
                async () => await NuevoAnalisisAsync(),
                () => !IsBusy && !CargandoListado && !IsRefreshing && CanAdd);
        }

        public ObservableCollection<AnalisisGuardadoResumen>
            AnalisisGuardados { get; }

        public ObservableCollection<UsuarioFiltroAnalisis>
            UsuariosFiltro { get; }

        public ObservableCollection<string>
            OpcionesAlcanceListado { get; }

        public Command ListarCommand { get; }
        public Command ActualizarCommand { get; }
        public Command BuscarCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command NuevoAnalisisCommand { get; }
        public Command<AnalisisGuardadoResumen> VisualizarCommand { get; }
        public Command<AnalisisGuardadoResumen> EditarCommand { get; }
        public Command<AnalisisGuardadoResumen> EliminarCommand { get; }

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
                OnPropertyChanged(nameof(MostrarEsperaInicial));
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        public bool CargandoUsuarios
        {
            get => cargandoUsuarios;
            private set
            {
                if (cargandoUsuarios == value)
                    return;

                cargandoUsuarios = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PuedeFiltrarPorUsuario));
            }
        }

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                string nuevo = value ?? string.Empty;
                if (mensaje == nuevo)
                    return;

                mensaje = nuevo;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(Mensaje);

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                string nuevo = value ?? string.Empty;
                if (textoBusqueda == nuevo)
                    return;

                textoBusqueda = nuevo;
                OnPropertyChanged();
            }
        }

        public bool UsarFiltroRangoFecha
        {
            get => usarFiltroRangoFecha;
            set
            {
                if (usarFiltroRangoFecha == value)
                    return;

                usarFiltroRangoFecha = value;
                ErrorRangoFecha = string.Empty;
                OnPropertyChanged();
            }
        }

        public DateTime FechaDesde
        {
            get => fechaDesde;
            set
            {
                DateTime nueva = value.Date;
                if (fechaDesde == nueva)
                    return;

                fechaDesde = nueva;
                ErrorRangoFecha = string.Empty;
                OnPropertyChanged();
            }
        }

        public DateTime FechaHasta
        {
            get => fechaHasta;
            set
            {
                DateTime nueva = value.Date;
                if (fechaHasta == nueva)
                    return;

                fechaHasta = nueva;
                ErrorRangoFecha = string.Empty;
                OnPropertyChanged();
            }
        }

        public string ErrorRangoFecha
        {
            get => errorRangoFecha;
            private set
            {
                errorRangoFecha = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorRangoFecha));
            }
        }

        public bool TieneErrorRangoFecha =>
            !string.IsNullOrWhiteSpace(ErrorRangoFecha);

        /*
         * Se conserva el nombre histórico EsAdministrador para no romper el
         * XAML existente. Su significado es permiso de alcance global.
         */
        public bool EsAdministrador
        {
            get => esAdministrador;
            private set
            {
                if (esAdministrador == value)
                    return;

                esAdministrador = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MostrarSelectorAlcance));
                OnPropertyChanged(nameof(PuedeFiltrarPorUsuario));
                OnPropertyChanged(nameof(ListarSoloPropios));
            }
        }

        public bool MostrarSelectorAlcance => EsAdministrador;

        public string AlcanceListadoSeleccionado
        {
            get => alcanceListadoSeleccionado;
            set
            {
                string nuevo = string.IsNullOrWhiteSpace(value)
                    ? (EsAdministrador ? AlcanceTodos : AlcancePropios)
                    : value;

                if (alcanceListadoSeleccionado == nuevo)
                    return;

                alcanceListadoSeleccionado = nuevo;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ListarSoloPropios));
                OnPropertyChanged(nameof(PuedeFiltrarPorUsuario));

                // Cambiar alcance solo modifica la interfaz. No ejecuta HTTP.
                usuarioFiltroSeleccionado =
                    UsuariosFiltro.FirstOrDefault();
                OnPropertyChanged(nameof(UsuarioFiltroSeleccionado));
            }
        }

        public bool ListarSoloPropios =>
            !EsAdministrador ||
            string.Equals(
                AlcanceListadoSeleccionado,
                AlcancePropios,
                StringComparison.OrdinalIgnoreCase);

        public bool PuedeFiltrarPorUsuario =>
            EsAdministrador &&
            !ListarSoloPropios &&
            SeHaListado &&
            usuariosCargados &&
            UsuariosFiltro.Count > 0 &&
            !CargandoUsuarios;

        public UsuarioFiltroAnalisis? UsuarioFiltroSeleccionado
        {
            get => usuarioFiltroSeleccionado;
            set
            {
                if (ReferenceEquals(usuarioFiltroSeleccionado, value))
                    return;

                usuarioFiltroSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public bool SeHaListado
        {
            get => seHaListado;
            private set
            {
                if (seHaListado == value)
                    return;

                seHaListado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NoSeHaListado));
                OnPropertyChanged(nameof(MostrarEsperaInicial));
                OnPropertyChanged(nameof(TextoBotonListar));
                OnPropertyChanged(nameof(MensajeListaVacia));
                OnPropertyChanged(nameof(SubtituloListaVacia));
                OnPropertyChanged(nameof(TotalMostradoTexto));
                OnPropertyChanged(nameof(PuedeFiltrarPorUsuario));
                OnPropertyChanged(nameof(MostrarPaginacion));
                ActualizarComandos();
            }
        }

        public bool NoSeHaListado => !SeHaListado;

        public bool MostrarEsperaInicial =>
            NoSeHaListado && !CargandoListado;

        public bool TieneAnalisis =>
            AnalisisGuardados.Count > 0;

        public bool MostrarListaVacia =>
            SeHaListado &&
            !TieneAnalisis &&
            !IsBusy &&
            !CargandoListado;

        /// <summary>
        /// Indica si la última consulta del listado terminó correctamente.
        /// MainPage la utiliza para confirmar una actualización pendiente
        /// únicamente después de recibir datos válidos del origen actual.
        /// </summary>
        public bool UltimaCargaExitosa => ultimaCargaExitosa;

        public int PaginaActual =>
            Math.Max(1, paginaActual);

        public int TotalPaginas =>
            Math.Max(1, totalPaginas);

        public bool PuedeIrAnterior =>
            SeHaListado && paginaActual > 1;

        public bool PuedeIrSiguiente =>
            SeHaListado && paginaActual < totalPaginas;

        public bool MostrarPaginacion =>
            CanView &&
            SeHaListado &&
            (AnalisisGuardados.Count > 0 || totalPaginas > 1);

        public string PaginaTexto =>
            $"Página {PaginaActual} de {TotalPaginas}";

        public string RangoPaginaTexto
        {
            get
            {
                if (totalRegistros <= 0 ||
                    AnalisisGuardados.Count == 0)
                {
                    return "Sin registros en esta página";
                }

                return AnalisisGuardados.Count == 1
                    ? $"1 análisis en esta página · {totalRegistros} en total"
                    : $"{AnalisisGuardados.Count} análisis en esta página · {totalRegistros} en total";
            }
        }

        public string TextoBotonListar =>
            SeHaListado ? "Aplicar filtros" : "Listar análisis";

        public string MensajeListaVacia =>
            SeHaListado
                ? "No hay análisis para mostrar"
                : "Preparando el listado";

        public string SubtituloListaVacia =>
            SeHaListado
                ? "Cambie los filtros o cree un nuevo análisis."
                : "Los primeros registros se cargarán al ingresar al módulo.";

        public string TotalMostradoTexto =>
            !SeHaListado
                ? "Cargando información fresca"
                : totalRegistros == 1
                    ? "1 análisis encontrado"
                    : $"{totalRegistros} análisis encontrados";

        public void PrepararPantalla()
        {
            bool puedeVerTodos =
                PermissionService.Instance.HasRead(
                    InterfazCodigos.AnalisisSueloTodos);

            EsAdministrador = puedeVerTodos;

            if (!puedeVerTodos)
            {
                alcanceListadoSeleccionado = AlcancePropios;
                OnPropertyChanged(nameof(AlcanceListadoSeleccionado));
                OnPropertyChanged(nameof(ListarSoloPropios));
            }

            ActualizarComandos();
        }

        /// <summary>
        /// Reinicia exclusivamente el estado del listado al comenzar una visita
        /// real y obtiene la primera página fresca. Los subflujos internos no
        /// llaman este método y por eso conservan filtros y página.
        /// </summary>
        public async Task IniciarNuevaVisitaAsync()
        {
            CancelarCarga();

            AnalisisGuardados.Clear();
            UsuariosFiltro.Clear();
            usuariosCargados = false;
            usuarioFiltroSeleccionado = null;

            textoBusqueda = string.Empty;
            usarFiltroRangoFecha = false;
            fechaDesde = DateTime.Today.AddDays(-6);
            fechaHasta = DateTime.Today;
            errorRangoFecha = string.Empty;

            alcanceListadoSeleccionado =
                EsAdministrador ? AlcanceTodos : AlcancePropios;

            paginaActual = 1;
            totalPaginas = 1;
            totalRegistros = 0;
            tamanoPaginaActual = ObtenerTamanoPagina();
            ultimaCargaExitosa = false;
            SeHaListado = false;
            Mensaje = string.Empty;

            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(UsarFiltroRangoFecha));
            OnPropertyChanged(nameof(FechaDesde));
            OnPropertyChanged(nameof(FechaHasta));
            OnPropertyChanged(nameof(ErrorRangoFecha));
            OnPropertyChanged(nameof(TieneErrorRangoFecha));
            OnPropertyChanged(nameof(AlcanceListadoSeleccionado));
            OnPropertyChanged(nameof(ListarSoloPropios));
            OnPropertyChanged(nameof(UsuarioFiltroSeleccionado));

            AplicarFiltrosEscritos();

            await CargarPaginaAsync(
                1,
                mostrarIndicador: true);

            if (ultimaCargaExitosa &&
                EsAdministrador &&
                !ListarSoloPropiosAplicado)
            {
                await CargarUsuariosFiltroAsync();
            }
        }

        public async Task RecargarPaginaActualAsync()
        {
            if (!SeHaListado)
                return;

            await CargarPaginaAsync(
                Math.Max(1, paginaActual),
                mostrarIndicador: true);
        }

        private bool ListarSoloPropiosAplicado =>
            !EsAdministrador ||
            string.Equals(
                alcanceListadoAplicado,
                AlcancePropios,
                StringComparison.OrdinalIgnoreCase);

        private int ObtenerTamanoPagina() =>
            DeviceInfo.Current.Platform == DevicePlatform.WinUI
                ? 12
                : 6;

        private bool PuedeEjecutarConsulta() =>
            !IsBusy &&
            !CargandoListado &&
            !IsRefreshing &&
            CanView;

        private bool PuedeEjecutarInteraccionListado() =>
            PuedeEjecutarConsulta() &&
            !IsRefreshing;

        private bool ValidarRangoFechaEscrito()
        {
            ErrorRangoFecha = string.Empty;

            if (!UsarFiltroRangoFecha)
                return true;

            if (FechaDesde.Date <= FechaHasta.Date)
                return true;

            ErrorRangoFecha =
                "La fecha Desde no puede ser mayor que la fecha Hasta.";

            return false;
        }

        private void AplicarFiltrosEscritos()
        {
            textoBusquedaAplicado =
                TextoBusqueda.Trim();

            usarFiltroRangoFechaAplicado =
                UsarFiltroRangoFecha;

            fechaDesdeAplicada =
                UsarFiltroRangoFecha
                    ? FechaDesde.Date
                    : null;

            fechaHastaAplicada =
                UsarFiltroRangoFecha
                    ? FechaHasta.Date
                    : null;

            alcanceListadoAplicado =
                EsAdministrador
                    ? AlcanceListadoSeleccionado
                    : AlcancePropios;

            usuarioFiltroAplicado =
                !ListarSoloPropiosAplicado
                    ? UsuarioFiltroSeleccionado?.UsuarioId
                    : null;
        }

        private async Task AplicarFiltrosYBuscarAsync()
        {
            if (!PuedeEjecutarInteraccionListado() ||
                !ValidarRangoFechaEscrito())
            {
                return;
            }

            AplicarFiltrosEscritos();

            await CargarPaginaAsync(
                1,
                mostrarIndicador: true);

            if (ultimaCargaExitosa &&
                EsAdministrador &&
                !ListarSoloPropiosAplicado &&
                !usuariosCargados)
            {
                await CargarUsuariosFiltroAsync();
            }
        }

        private async Task ActualizarAsync()
        {
            if (!SeHaListado || !PuedeEjecutarConsulta())
                return;

            try
            {
                IsRefreshing = true;

                await CargarPaginaAsync(
                    Math.Max(1, paginaActual),
                    mostrarIndicador: false);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task IrPaginaAsync(int pagina)
        {
            if (!PuedeEjecutarInteraccionListado() ||
                pagina < 1 ||
                pagina > Math.Max(1, totalPaginas) ||
                pagina == paginaActual)
            {
                return;
            }

            await CargarPaginaAsync(
                pagina,
                mostrarIndicador: true);
        }

        private async Task CargarPaginaAsync(
            int paginaSolicitada,
            bool mostrarIndicador)
        {
            if (!CanView || IsBusy || CargandoListado)
                return;

            cargaCancellationTokenSource?.Cancel();
            cargaCancellationTokenSource?.Dispose();

            var source = new CancellationTokenSource();
            cargaCancellationTokenSource = source;
            CancellationToken cancellationToken = source.Token;

            try
            {
                ultimaCargaExitosa = false;
                CargandoListado = mostrarIndicador;
                Mensaje = string.Empty;

                tamanoPaginaActual = ObtenerTamanoPagina();

                ApiResult<AnalisisListadoPaginadoResponse> result =
                    await listadoApiService.ListarAsync(
                        ListarSoloPropiosAplicado,
                        usuarioFiltroAplicado,
                        textoBusquedaAplicado,
                        fechaDesdeAplicada,
                        fechaHastaAplicada,
                        Math.Max(1, paginaSolicitada),
                        tamanoPaginaActual,
                        cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                if (!result.Success || result.Data == null)
                {
                    if (!string.Equals(
                            result.Message,
                            "La operación fue cancelada.",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        Mensaje = result.Message;
                    }

                    return;
                }

                AnalisisListadoPaginadoResponse data = result.Data;

                EsAdministrador = data.EsAdministrador;

                if (!EsAdministrador)
                {
                    alcanceListadoSeleccionado = AlcancePropios;
                    alcanceListadoAplicado = AlcancePropios;
                    usuarioFiltroAplicado = null;
                    OnPropertyChanged(nameof(AlcanceListadoSeleccionado));
                    OnPropertyChanged(nameof(ListarSoloPropios));
                }

                AnalisisGuardados.Clear();

                foreach (AnalisisGuardadoResumen item in data.Items)
                    AnalisisGuardados.Add(item);

                paginaActual = Math.Max(1, data.Pagina);
                totalPaginas = Math.Max(1, data.TotalPaginas);
                totalRegistros = Math.Max(0, data.TotalRegistros);
                tamanoPaginaActual = Math.Max(
                    1,
                    data.TamanoPagina > 0
                        ? data.TamanoPagina
                        : tamanoPaginaActual);

                SeHaListado = true;
                ultimaCargaExitosa = true;
                NotificarEstadoLista();
            }
            catch (OperationCanceledException)
            {
                // La pantalla se cerró o una consulta posterior reemplazó esta.
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Mensaje =
                        "No fue posible cargar los análisis en este momento.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los análisis",
                        ex);
                }
            }
            finally
            {
                CargandoListado = false;
                IsRefreshing = false;

                if (ReferenceEquals(
                        cargaCancellationTokenSource,
                        source))
                {
                    cargaCancellationTokenSource.Dispose();
                    cargaCancellationTokenSource = null;
                }
                else
                {
                    source.Dispose();
                }

                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        public void CancelarCarga()
        {
            cargaCancellationTokenSource?.Cancel();
            usuariosCancellationTokenSource?.Cancel();

            CargandoListado = false;
            CargandoUsuarios = false;
            IsRefreshing = false;
        }

        private void ConfigurarUsuarios(
            IEnumerable<UsuarioFiltroAnalisis> usuarios)
        {
            int? seleccionAnterior =
                UsuarioFiltroSeleccionado?.UsuarioId;

            UsuariosFiltro.Clear();
            UsuariosFiltro.Add(new UsuarioFiltroAnalisis
            {
                UsuarioId = null,
                NombreCompleto = "Todos los usuarios"
            });

            foreach (UsuarioFiltroAnalisis usuario in usuarios
                         .Where(x => x.UsuarioId.HasValue)
                         .GroupBy(x => x.UsuarioId)
                         .Select(x => x.First())
                         .OrderBy(x => x.NombreCompleto))
            {
                UsuariosFiltro.Add(usuario);
            }

            usuarioFiltroSeleccionado =
                UsuariosFiltro.FirstOrDefault(x =>
                    x.UsuarioId == seleccionAnterior)
                ?? UsuariosFiltro.FirstOrDefault();

            OnPropertyChanged(nameof(UsuarioFiltroSeleccionado));
            OnPropertyChanged(nameof(PuedeFiltrarPorUsuario));
        }

        private async Task CargarUsuariosFiltroAsync()
        {
            if (!EsAdministrador ||
                ListarSoloPropiosAplicado ||
                usuariosCargados ||
                CargandoUsuarios)
            {
                return;
            }

            usuariosCancellationTokenSource?.Cancel();
            usuariosCancellationTokenSource?.Dispose();

            var source = new CancellationTokenSource();
            usuariosCancellationTokenSource = source;

            try
            {
                CargandoUsuarios = true;

                ApiResult<List<UsuarioFiltroAnalisis>> result =
                    await listadoApiService.ListarUsuariosAsync(
                        source.Token);

                if (source.IsCancellationRequested ||
                    !result.Success ||
                    result.Data == null)
                {
                    return;
                }

                ConfigurarUsuarios(result.Data);
                usuariosCargados = true;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                CargandoUsuarios = false;

                if (ReferenceEquals(
                        usuariosCancellationTokenSource,
                        source))
                {
                    usuariosCancellationTokenSource.Dispose();
                    usuariosCancellationTokenSource = null;
                }
                else
                {
                    source.Dispose();
                }
            }
        }

        private async Task LimpiarFiltrosAsync()
        {
            textoBusqueda = string.Empty;
            usarFiltroRangoFecha = false;
            fechaDesde = DateTime.Today.AddDays(-6);
            fechaHasta = DateTime.Today;
            errorRangoFecha = string.Empty;
            usuarioFiltroSeleccionado =
                UsuariosFiltro.FirstOrDefault();

            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(UsarFiltroRangoFecha));
            OnPropertyChanged(nameof(FechaDesde));
            OnPropertyChanged(nameof(FechaHasta));
            OnPropertyChanged(nameof(ErrorRangoFecha));
            OnPropertyChanged(nameof(TieneErrorRangoFecha));
            OnPropertyChanged(nameof(UsuarioFiltroSeleccionado));

            AplicarFiltrosEscritos();

            await CargarPaginaAsync(
                1,
                mostrarIndicador: true);
        }

        private async Task NuevoAnalisisAsync()
        {
            if (!CanAdd || IsBusy)
                return;

            try
            {
                IsBusy = true;
                Mensaje = "Preparando un nuevo análisis...";

                await EsperarRenderizadoIndicadorAsync();

                if (!await ValidarDatosOfflineAntesDeAbrirAsync())
                    return;

                AnalisisEdicionService.Instance.Limpiar();
                InvalidarCatalogosFormulario();

                Mensaje = string.Empty;
                await GoToAsyncParameters("//NuevoAnalisisFormPage");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task VisualizarAsync(
            AnalisisGuardadoResumen? analisis)
        {
            if (analisis == null || !CanView || IsBusy)
                return;

            await GoToAsyncParameters(
                AppRoutes.AnalisisGuardadoDetalle,
                new Dictionary<string, object>
                {
                    ["analisisSueloCalculoId"] =
                        analisis.AnalisisSueloCalculoId,
                    ["resumenAnalisis"] = analisis
                });
        }

        private async Task EditarAsync(
            AnalisisGuardadoResumen? analisis)
        {
            if (analisis == null || !CanEdit || IsBusy)
                return;

            string usuarioActualTexto = Preferences.Get(
                SessionKeys.KeyUserId,
                string.Empty);

            if (!int.TryParse(
                    usuarioActualTexto,
                    out int usuarioActualId) ||
                usuarioActualId <= 0)
            {
                await MostrarInformacionAsync(
                    "No se pudo identificar al usuario de la sesión. " +
                    "Cierre sesión e ingrese nuevamente.");

                return;
            }

            if (!analisis.UsuarioId.HasValue ||
                analisis.UsuarioId.Value <= 0)
            {
                await MostrarInformacionAsync(
                    "Este análisis no tiene un usuario propietario asignado. " +
                    "No puede editarse hasta corregir ese dato.");

                return;
            }

            if (analisis.UsuarioId.Value != usuarioActualId)
            {
                await MostrarInformacionAsync(
                    $"No puede editar el análisis “{analisis.IdentificadorMostrar}” " +
                    $"porque pertenece a {analisis.UsuarioMostrar}. " +
                    "Solamente el usuario propietario puede modificarlo.");

                return;
            }

            try
            {
                IsBusy = true;
                Mensaje = "Cargando el análisis para edición...";

                await EsperarRenderizadoIndicadorAsync();

                if (!await ValidarDatosOfflineAntesDeAbrirAsync())
                    return;

                (bool success, string message) =
                    await AnalisisEdicionService.Instance.PrepararAsync(
                        analisis.AnalisisSueloCalculoId,
                        analisis);

                if (!success)
                {
                    Mensaje = message;

                    await Application.Current!.MainPage!.DisplayAlert(
                        "No se pudo abrir",
                        message,
                        "Aceptar");

                    return;
                }

                Mensaje = string.Empty;
                await GoToAsyncParameters("//NuevoAnalisisFormPage");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<bool>
            ValidarDatosOfflineAntesDeAbrirAsync()
        {
            if (!ModoSesionService.EsOffline)
                return true;

            AnalisisOfflineFormularioValidacionResultado resultado =
                await AnalisisOfflineFormularioValidacionService.Instance
                    .ValidarAsync();

            if (resultado.Success)
                return true;

            Mensaje = resultado.Message;

            await Application.Current!.MainPage!.DisplayAlert(
                "Datos offline incompletos",
                resultado.Message,
                "Aceptar");

            return false;
        }

        private static async Task EsperarRenderizadoIndicadorAsync()
        {
            await Task.Yield();
            await Task.Delay(80);
        }

        private static void InvalidarCatalogosFormulario()
        {
            AnalisisSueloApiService.LimpiarCacheTiposCultivo();
            UnidadMedidaApiService.InvalidarCache();
            ElementoQuimicoApiService.InvalidarCache();
            ConfiguracionUnidadesApiService.InvalidarCache();
        }

        private async Task EliminarAsync(
            AnalisisGuardadoResumen? analisis)
        {
            if (analisis == null || !CanDelete || IsBusy)
                return;

            bool confirmar =
                await Application.Current!.MainPage!.DisplayAlert(
                    "Eliminar análisis",
                    $"¿Desea eliminar el análisis " +
                    $"{analisis.IdentificadorMostrar}? " +
                    "Esta acción también desactivará sus cálculos relacionados.",
                    "Sí, eliminar",
                    "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;

                EliminarAnalisisResponse respuesta =
                    await guardarTodoApiService.EliminarAsync(
                        analisis.AnalisisSueloId);

                if (!respuesta.Success)
                {
                    Mensaje = string.IsNullOrWhiteSpace(
                        respuesta.Message)
                        ? "La API no pudo eliminar el análisis."
                        : respuesta.Message;

                    await Application.Current!.MainPage!.DisplayAlert(
                        "No se pudo eliminar",
                        Mensaje,
                        "Aceptar");

                    return;
                }

                /*
                 * La eliminación ya fue confirmada por el servidor. Se refleja
                 * localmente como respaldo por si la consulta fresca falla; si
                 * el GET responde, la colección se reemplaza inmediatamente.
                 */
                AnalisisGuardados.Remove(analisis);
                totalRegistros = Math.Max(0, totalRegistros - 1);
                NotificarEstadoLista();

                /*
                 * Si se eliminó el último elemento de una página posterior, se
                 * consulta directamente la página anterior. En cualquier otro
                 * caso se renueva la página visible. Esto evita calcular páginas
                 * con el total combinado de registros sincronizados y pendientes.
                 */
                int paginaDestino =
                    AnalisisGuardados.Count == 0 && paginaActual > 1
                        ? paginaActual - 1
                        : Math.Max(1, paginaActual);

                // Se libera IsBusy para permitir la consulta fresca del listado.
                IsBusy = false;

                await CargarPaginaAsync(
                    paginaDestino,
                    mostrarIndicador: true);

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(respuesta.Message)
                        ? "Análisis eliminado correctamente."
                        : respuesta.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void NotificarEstadoLista()
        {
            OnPropertyChanged(nameof(TieneAnalisis));
            OnPropertyChanged(nameof(MostrarListaVacia));
            OnPropertyChanged(nameof(PaginaActual));
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(PuedeIrAnterior));
            OnPropertyChanged(nameof(PuedeIrSiguiente));
            OnPropertyChanged(nameof(MostrarPaginacion));
            OnPropertyChanged(nameof(PaginaTexto));
            OnPropertyChanged(nameof(RangoPaginaTexto));
            OnPropertyChanged(nameof(TotalMostradoTexto));
        }

        private void ActualizarComandos()
        {
            ListarCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            BuscarCommand.ChangeCanExecute();
            PaginaAnteriorCommand.ChangeCanExecute();
            PaginaSiguienteCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            NuevoAnalisisCommand.ChangeCanExecute();
            VisualizarCommand.ChangeCanExecute();
            EditarCommand.ChangeCanExecute();
            EliminarCommand.ChangeCanExecute();
        }
    }
}
