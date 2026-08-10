using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
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

        private string textoBusqueda = string.Empty;
        private string mensaje = string.Empty;
        private string errorRangoFecha = string.Empty;
        private bool usarFiltroRangoFecha;
        private DateTime fechaDesde = DateTime.Today.AddDays(-6);
        private DateTime fechaHasta = DateTime.Today;
        private bool esAdministrador;
        private bool seHaListado;
        private bool isRefreshing;
        private bool cargandoListado;
        private bool cargandoMas;
        private bool cargandoUsuarios;
        private bool usuariosCargados;
        private bool ultimaCargaExitosa;
        private int paginaActual;
        private int totalPaginas = 1;
        private int totalRegistros;
        private string alcanceListadoSeleccionado = AlcanceTodos;
        private UsuarioFiltroAnalisis? usuarioFiltroSeleccionado;
        private CancellationTokenSource? cargaCancellationTokenSource;
        private CancellationTokenSource? filtroCancellationTokenSource;
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
                async () => await ListarManualmenteAsync(),
                () => !IsBusy && !CargandoListado && CanView);

            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => !IsBusy && !CargandoListado && SeHaListado && CanView);

            BuscarCommand = new Command(
                async () => await RecargarPorFiltroAsync(),
                () => !IsBusy && !CargandoListado && SeHaListado && CanView);

            CargarMasCommand = new Command(
                async () => await CargarMasAsync(),
                () => !IsBusy && !CargandoListado && !CargandoMas &&
                      PuedeCargarMas && CanView);

            VisualizarCommand =
                new Command<AnalisisGuardadoResumen>(
                    async item => await VisualizarAsync(item),
                    item => !IsBusy && !CargandoListado &&
                            item != null && CanView);

            EditarCommand =
                new Command<AnalisisGuardadoResumen>(
                    async item => await EditarAsync(item),
                    item => !IsBusy && !CargandoListado &&
                            item != null && CanEdit);

            EliminarCommand =
                new Command<AnalisisGuardadoResumen>(
                    async item => await EliminarAsync(item),
                    item => !IsBusy && !CargandoListado &&
                            item != null && CanDelete);

            LimpiarFiltrosCommand = new Command(
                async () => await LimpiarFiltrosAsync(),
                () => !IsBusy && !CargandoListado && SeHaListado);

            NuevoAnalisisCommand = new Command(
                async () => await NuevoAnalisisAsync(),
                () => !IsBusy && !CargandoListado && CanAdd);
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
        public Command CargarMasCommand { get; }
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
                ProgramarRecargaFiltros();
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
                OnPropertyChanged();
                ProgramarRecargaFiltros();
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
                OnPropertyChanged();

                if (UsarFiltroRangoFecha)
                    ProgramarRecargaFiltros();
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
                OnPropertyChanged();

                if (UsarFiltroRangoFecha)
                    ProgramarRecargaFiltros();
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
         * XAML existente. Desde esta versión significa "puede ver análisis de
         * todos los usuarios" y depende exclusivamente del permiso dedicado.
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
                    ? AlcanceTodos
                    : value;

                if (alcanceListadoSeleccionado == nuevo)
                    return;

                alcanceListadoSeleccionado = nuevo;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ListarSoloPropios));
                OnPropertyChanged(nameof(PuedeFiltrarPorUsuario));

                usuarioFiltroSeleccionado =
                    UsuariosFiltro.FirstOrDefault();

                OnPropertyChanged(nameof(UsuarioFiltroSeleccionado));

                if (SeHaListado)
                {
                    if (!ListarSoloPropios &&
                        !usuariosCargados &&
                        !CargandoUsuarios)
                    {
                        _ = CargarUsuariosFiltroAsync();
                    }

                    ProgramarRecargaFiltros();
                }
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

                if (PuedeFiltrarPorUsuario)
                    ProgramarRecargaFiltros();
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
                ActualizarComandos();
            }
        }

        public bool NoSeHaListado => !SeHaListado;

        public bool MostrarEsperaInicial =>
            NoSeHaListado && !CargandoListado;

        public bool TieneAnalisis => AnalisisGuardados.Count > 0;

        public bool MostrarListaVacia =>
            SeHaListado &&
            !TieneAnalisis &&
            !IsBusy;

        public bool PuedeCargarMas =>
            SeHaListado && paginaActual < totalPaginas;

        public bool UltimaCargaExitosa => ultimaCargaExitosa;

        public bool MostrarFinLista =>
            SeHaListado &&
            TieneAnalisis &&
            !PuedeCargarMas &&
            !CargandoMas;

        public string TextoBotonListar =>
            SeHaListado ? "Actualizar lista" : "Listar análisis";

        public string MensajeListaVacia =>
            SeHaListado
                ? "No hay análisis para mostrar"
                : "Los análisis se cargan bajo demanda";

        public string SubtituloListaVacia =>
            SeHaListado
                ? "Cambie los filtros o cree un nuevo análisis."
                : "Presione Listar análisis para cargar los primeros registros.";

        public string TotalMostradoTexto =>
            !SeHaListado
                ? "Listado bajo demanda"
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
            else if (string.Equals(
                         alcanceListadoSeleccionado,
                         AlcancePropios,
                         StringComparison.OrdinalIgnoreCase) &&
                     !SeHaListado)
            {
                /*
                 * Una nueva sesión con alcance global inicia mostrando todos.
                 * Después el usuario puede seleccionar "Solo mis análisis".
                 */
                alcanceListadoSeleccionado = AlcanceTodos;
                OnPropertyChanged(nameof(AlcanceListadoSeleccionado));
                OnPropertyChanged(nameof(ListarSoloPropios));
            }

            ActualizarComandos();
        }

        public async Task CargarAnalisisAsync(
            bool mostrarIndicador = true,
            bool reiniciar = true)
        {
            if (!CanView)
                return;

            if (reiniciar && (IsBusy || CargandoListado))
                return;

            if (!reiniciar &&
                (CargandoMas || !PuedeCargarMas))
            {
                return;
            }

            if (!ValidarRangoFecha())
                return;

            CancellationTokenSource currentSource;

            if (reiniciar)
            {
                cargaCancellationTokenSource?.Cancel();
                cargaCancellationTokenSource?.Dispose();

                currentSource = new CancellationTokenSource();
                cargaCancellationTokenSource = currentSource;
            }
            else
            {
                currentSource =
                    cargaCancellationTokenSource ??
                    new CancellationTokenSource();

                cargaCancellationTokenSource ??= currentSource;
            }

            CancellationToken cancellationToken =
                currentSource.Token;

            try
            {
                if (reiniciar)
                {
                    ultimaCargaExitosa = false;
                    CargandoListado = true;
                    Mensaje = string.Empty;
                }
                else
                {
                    CargandoMas = true;
                }

                int paginaSolicitada = reiniciar
                    ? 1
                    : paginaActual + 1;

                int tamanoPagina =
                    DeviceInfo.Current.Platform == DevicePlatform.WinUI
                        ? 12
                        : 6;

                ApiResult<AnalisisListadoPaginadoResponse> result =
                    await listadoApiService.ListarAsync(
                        ListarSoloPropios,
                        PuedeFiltrarPorUsuario
                            ? UsuarioFiltroSeleccionado?.UsuarioId
                            : null,
                        TextoBusqueda,
                        UsarFiltroRangoFecha
                            ? FechaDesde
                            : null,
                        UsarFiltroRangoFecha
                            ? FechaHasta
                            : null,
                        paginaSolicitada,
                        tamanoPagina,
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

                /*
                 * El backend devuelve el permiso efectivo usando el campo
                 * histórico esAdministrador para compatibilidad. Nunca se
                 * deduce el alcance por el nombre del rol.
                 */
                EsAdministrador = data.EsAdministrador;

                if (!EsAdministrador &&
                    !string.Equals(
                        alcanceListadoSeleccionado,
                        AlcancePropios,
                        StringComparison.OrdinalIgnoreCase))
                {
                    alcanceListadoSeleccionado = AlcancePropios;
                    OnPropertyChanged(nameof(AlcanceListadoSeleccionado));
                    OnPropertyChanged(nameof(ListarSoloPropios));
                }

                if (reiniciar)
                    AnalisisGuardados.Clear();

                foreach (AnalisisGuardadoResumen item in data.Items)
                {
                    if (AnalisisGuardados.Any(x =>
                            x.AnalisisSueloCalculoId ==
                            item.AnalisisSueloCalculoId))
                    {
                        continue;
                    }

                    AnalisisGuardados.Add(item);
                }

                paginaActual = data.Pagina;
                totalPaginas = Math.Max(1, data.TotalPaginas);
                totalRegistros = data.TotalRegistros;
                SeHaListado = true;
                ultimaCargaExitosa = true;

                if (data.Usuarios.Count > 0)
                {
                    ConfigurarUsuarios(data.Usuarios);
                    usuariosCargados = true;
                }
                else if (data.EsAdministrador &&
                         !ListarSoloPropios &&
                         !usuariosCargados &&
                         !CargandoUsuarios)
                {
                    _ = CargarUsuariosFiltroAsync();
                }

                NotificarEstadoLista();
            }
            catch (OperationCanceledException)
            {
                // La pantalla se cerró o un filtro reemplazó la consulta.
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
                if (reiniciar)
                {
                    CargandoListado = false;
                    IsRefreshing = false;
                }
                else
                {
                    CargandoMas = false;
                }

                if (ReferenceEquals(
                        cargaCancellationTokenSource,
                        currentSource))
                {
                    cargaCancellationTokenSource.Dispose();
                    cargaCancellationTokenSource = null;
                }
                else
                {
                    currentSource.Dispose();
                }

                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        public void CancelarCarga()
        {
            filtroCancellationTokenSource?.Cancel();
            cargaCancellationTokenSource?.Cancel();
            usuariosCancellationTokenSource?.Cancel();

            CargandoListado = false;
            CargandoMas = false;
            CargandoUsuarios = false;
            IsRefreshing = false;
        }

        private bool ValidarRangoFecha()
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
                ListarSoloPropios ||
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

                if (source.IsCancellationRequested)
                    return;

                if (!result.Success || result.Data == null)
                    return;

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

        private async Task ListarManualmenteAsync()
        {
            await CargarAnalisisAsync(
                mostrarIndicador: true,
                reiniciar: true);
        }

        private async Task ActualizarAsync()
        {
            if (!SeHaListado)
                return;

            try
            {
                IsRefreshing = true;

                await CargarAnalisisAsync(
                    mostrarIndicador: false,
                    reiniciar: true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task RecargarPorFiltroAsync()
        {
            if (!SeHaListado || IsBusy || CargandoListado)
                return;

            await CargarAnalisisAsync(
                mostrarIndicador: true,
                reiniciar: true);
        }

        private async Task CargarMasAsync()
        {
            await CargarAnalisisAsync(
                mostrarIndicador: false,
                reiniciar: false);
        }

        private async Task LimpiarFiltrosAsync()
        {
            filtroCancellationTokenSource?.Cancel();

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

            await CargarAnalisisAsync(
                mostrarIndicador: true,
                reiniciar: true);
        }

        private void ProgramarRecargaFiltros()
        {
            if (!SeHaListado || IsBusy || CargandoListado)
                return;

            filtroCancellationTokenSource?.Cancel();
            filtroCancellationTokenSource?.Dispose();

            var source = new CancellationTokenSource();
            filtroCancellationTokenSource = source;

            _ = ProgramarRecargaFiltrosAsync(source);
        }

        private async Task ProgramarRecargaFiltrosAsync(
            CancellationTokenSource source)
        {
            try
            {
                await Task.Delay(
                    500,
                    source.Token);

                if (source.IsCancellationRequested)
                    return;

                await MainThread.InvokeOnMainThreadAsync(
                    async () => await CargarAnalisisAsync(
                        mostrarIndicador: true,
                        reiniciar: true));
            }
            catch (OperationCanceledException)
            {
                // Otro cambio de filtro reemplazó esta espera.
            }
            finally
            {
                if (ReferenceEquals(
                        filtroCancellationTokenSource,
                        source))
                {
                    filtroCancellationTokenSource.Dispose();
                    filtroCancellationTokenSource = null;
                }
            }
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

                /*
                 * Permite que WinUI y Android dibujen el indicador antes de
                 * comenzar las lecturas locales y la deserialización.
                 */
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

        private static async Task
            EsperarRenderizadoIndicadorAsync()
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

                AnalisisGuardados.Remove(analisis);
                totalRegistros = Math.Max(0, totalRegistros - 1);
                NotificarEstadoLista();

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
            OnPropertyChanged(nameof(PuedeCargarMas));
            OnPropertyChanged(nameof(MostrarFinLista));
            OnPropertyChanged(nameof(TotalMostradoTexto));
        }

        private void ActualizarComandos()
        {
            ListarCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            BuscarCommand.ChangeCanExecute();
            CargarMasCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            NuevoAnalisisCommand.ChangeCanExecute();
            VisualizarCommand.ChangeCanExecute();
            EditarCommand.ChangeCanExecute();
            EliminarCommand.ChangeCanExecute();
        }
    }
}
