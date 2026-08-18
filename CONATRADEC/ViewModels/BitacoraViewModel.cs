using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class BitacoraViewModel : GlobalService
    {
        private const int TamanoPagina = 25;
        private const int MaximoBusqueda = 200;

        private readonly BitacoraApiService apiService = new();
        private readonly List<BitacoraUsuarioFiltro> opcionesUsuarios = new();

        private ObservableCollection<BitacoraListadoItem> registros = new();
        private ObservableCollection<string> acciones = new();
        private ObservableCollection<string> modulos = new();
        private ObservableCollection<string> usuarios = new();

        private DateTime fechaDesde = DateTime.Today.AddDays(-7);
        private DateTime fechaHasta = DateTime.Today;
        private string accionSeleccionada = "Todas";
        private string moduloSeleccionado = "Todos";
        private int usuarioSeleccionadoIndex;
        private string estadoSeleccionado = "Todos";
        private string textoBusqueda = string.Empty;
        private int pagina = 1;
        private int totalPaginas = 1;
        private int totalRegistros;
        private bool catalogosCargados;
        private bool consultaRealizada;
        private bool inicializado;
        private DateTime? corteConsultaUtc;
        private BitacoraFiltrosConsulta? filtrosAplicados;
        private CancellationTokenSource? cargaCts;

        public BitacoraViewModel()
        {
            LoadPagePermissions("bitacoraPage");

            BuscarCommand = new Command(
                async () => await BuscarAsync(),
                () => !IsBusy && CanView);

            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => !IsBusy && CanView && consultaRealizada);

            LimpiarCommand = new Command(
                async () => await LimpiarFiltrosAsync(),
                () => !IsBusy && CanView);

            AnteriorCommand = new Command(
                async () => await CambiarPaginaAsync(-1),
                () => PuedeAnterior);

            SiguienteCommand = new Command(
                async () => await CambiarPaginaAsync(1),
                () => PuedeSiguiente);

            VerDetalleCommand = new Command<BitacoraListadoItem>(
                async item => await VerDetalleAsync(item),
                item => item != null && !IsBusy && CanView);
        }

        public event EventHandler? SolicitarScrollInicio;

        public ObservableCollection<BitacoraListadoItem> Registros
        {
            get => registros;
            private set
            {
                registros = value ?? new ObservableCollection<BitacoraListadoItem>();
                OnPropertyChanged();
                ActualizarEstadoResultados();
            }
        }

        public ObservableCollection<string> Acciones
        {
            get => acciones;
            private set
            {
                acciones = value ?? new ObservableCollection<string>();
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> Modulos
        {
            get => modulos;
            private set
            {
                modulos = value ?? new ObservableCollection<string>();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// El Picker muestra texto, pero el identificador real del usuario se
        /// conserva en opcionesUsuarios utilizando el mismo índice.
        /// </summary>
        public ObservableCollection<string> Usuarios
        {
            get => usuarios;
            private set
            {
                usuarios = value ?? new ObservableCollection<string>();
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> Estados { get; } = new()
        {
            "Todos",
            "Correctos",
            "Con error"
        };

        public DateTime FechaDesde
        {
            get => fechaDesde;
            set
            {
                if (fechaDesde == value)
                    return;

                fechaDesde = value;
                OnPropertyChanged();
            }
        }

        public DateTime FechaHasta
        {
            get => fechaHasta;
            set
            {
                if (fechaHasta == value)
                    return;

                fechaHasta = value;
                OnPropertyChanged();
            }
        }

        public string AccionSeleccionada
        {
            get => accionSeleccionada;
            set
            {
                string nuevo = value ?? "Todas";
                if (accionSeleccionada == nuevo)
                    return;

                accionSeleccionada = nuevo;
                OnPropertyChanged();
            }
        }

        public string ModuloSeleccionado
        {
            get => moduloSeleccionado;
            set
            {
                string nuevo = value ?? "Todos";
                if (moduloSeleccionado == nuevo)
                    return;

                moduloSeleccionado = nuevo;
                OnPropertyChanged();
            }
        }

        public int UsuarioSeleccionadoIndex
        {
            get => usuarioSeleccionadoIndex;
            set
            {
                int indice = value;

                if (indice < 0 && Usuarios.Count > 0)
                    indice = 0;

                if (usuarioSeleccionadoIndex == indice)
                    return;

                usuarioSeleccionadoIndex = indice;
                OnPropertyChanged();
            }
        }

        public string EstadoSeleccionado
        {
            get => estadoSeleccionado;
            set
            {
                string nuevo = value ?? "Todos";
                if (estadoSeleccionado == nuevo)
                    return;

                estadoSeleccionado = nuevo;
                OnPropertyChanged();
            }
        }

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

        public int Pagina
        {
            get => pagina;
            private set
            {
                int nueva = Math.Max(1, value);
                if (pagina == nueva)
                    return;

                pagina = nueva;
                OnPropertyChanged();
                ActualizarPaginacion();
            }
        }

        public int TotalPaginas
        {
            get => totalPaginas;
            private set
            {
                int nuevo = Math.Max(1, value);
                if (totalPaginas == nuevo)
                    return;

                totalPaginas = nuevo;
                OnPropertyChanged();
                ActualizarPaginacion();
            }
        }

        public int TotalRegistros
        {
            get => totalRegistros;
            private set
            {
                int nuevo = Math.Max(0, value);
                if (totalRegistros == nuevo)
                    return;

                totalRegistros = nuevo;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResumenResultados));
            }
        }

        public bool HayRegistros => Registros.Count > 0;
        public bool TieneConsultaRealizada => consultaRealizada;

        public bool MostrarSinResultados =>
            consultaRealizada &&
            !HayRegistros &&
            !IsBusy;

        public bool MostrarAyudaDetalle =>
            consultaRealizada &&
            HayRegistros &&
            !IsBusy;

        public bool MostrarResumenResultados =>
            consultaRealizada &&
            !IsBusy;

        public bool MostrarPaginacion =>
            consultaRealizada &&
            HayRegistros &&
            TotalPaginas > 1 &&
            !IsBusy;

        public bool PuedeAnterior =>
            MostrarPaginacion &&
            Pagina > 1;

        public bool PuedeSiguiente =>
            MostrarPaginacion &&
            Pagina < TotalPaginas;

        public string ResumenPagina =>
            $"Página {Pagina} de {TotalPaginas}";

        public string ResumenResultados =>
            TotalRegistros == 1
                ? "1 registro encontrado"
                : $"{TotalRegistros:N0} registros encontrados";

        public Command BuscarCommand { get; }
        public Command ActualizarCommand { get; }
        public Command LimpiarCommand { get; }
        public Command AnteriorCommand { get; }
        public Command SiguienteCommand { get; }
        public Command<BitacoraListadoItem> VerDetalleCommand { get; }

        public async Task InicializarAsync()
        {
            LoadPagePermissions("bitacoraPage");
            OnPropertyChanged(nameof(CanView));
            ActualizarComandos();

            if (!CanView)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para consultar la bitácora.");

                await GoToAsyncParameters(AppRoutes.Regresar);
                return;
            }

            // Regresar desde el detalle pertenece a la misma visita.
            if (inicializado)
                return;

            if (!await ValidarInternetAsync())
                return;

            using CancellationTokenSource cts = CrearCargaCts();
            EstablecerBusy(true);

            try
            {
                if (!catalogosCargados)
                {
                    bool catalogosOk = await CargarCatalogosInternoAsync(
                        cts.Token);

                    if (!catalogosOk)
                        return;
                }

                if (!IntentarCrearFiltrosDesdeControles(
                        out BitacoraFiltrosConsulta filtros,
                        out string mensaje))
                {
                    await MostrarAdvertenciaAsync(mensaje);
                    return;
                }

                BitacoraPaginadaResponse? respuesta =
                    await ConsultarInternoAsync(
                        filtros,
                        1,
                        null,
                        cts.Token);

                if (respuesta == null)
                    return;

                filtrosAplicados = filtros;
                corteConsultaUtc = respuesta.CorteConsultaUtc;
                inicializado = true;
            }
            catch (OperationCanceledException)
                when (cts.IsCancellationRequested)
            {
                // La página se abandonó durante la consulta.
            }
            finally
            {
                LiberarCargaCts(cts);
                EstablecerBusy(false);
            }
        }

        public void CancelarCarga()
        {
            CancellationTokenSource? cts = cargaCts;
            cargaCts = null;

            if (cts == null)
                return;

            try
            {
                cts.Cancel();
            }
            catch
            {
            }
        }

        private async Task BuscarAsync()
        {
            if (IsBusy || !CanView)
                return;

            if (!IntentarCrearFiltrosDesdeControles(
                    out BitacoraFiltrosConsulta filtros,
                    out string mensaje))
            {
                await MostrarAdvertenciaAsync(mensaje);
                return;
            }

            bool ok = await EjecutarConsultaAsync(
                filtros,
                1,
                corte: null,
                limpiarResultadosSiFalla: true);

            if (ok)
                filtrosAplicados = filtros;
        }

        private async Task ActualizarAsync()
        {
            if (IsBusy || !CanView)
                return;

            BitacoraFiltrosConsulta? filtros = filtrosAplicados;

            if (filtros == null)
            {
                if (!IntentarCrearFiltrosDesdeControles(
                        out BitacoraFiltrosConsulta nuevos,
                        out string mensaje))
                {
                    await MostrarAdvertenciaAsync(mensaje);
                    return;
                }

                filtros = nuevos;
            }

            bool ok = await EjecutarConsultaAsync(
                filtros,
                Pagina,
                corte: null,
                limpiarResultadosSiFalla: false);

            if (ok && filtrosAplicados == null)
                filtrosAplicados = filtros;
        }

        private async Task LimpiarFiltrosAsync()
        {
            if (IsBusy || !CanView)
                return;

            FechaDesde = DateTime.Today.AddDays(-7);
            FechaHasta = DateTime.Today;
            AccionSeleccionada = Acciones.FirstOrDefault() ?? "Todas";
            ModuloSeleccionado = Modulos.FirstOrDefault() ?? "Todos";
            SeleccionarPrimerUsuario();
            EstadoSeleccionado = Estados.FirstOrDefault() ?? "Todos";
            TextoBusqueda = string.Empty;

            if (!IntentarCrearFiltrosDesdeControles(
                    out BitacoraFiltrosConsulta filtros,
                    out string mensaje))
            {
                await MostrarAdvertenciaAsync(mensaje);
                return;
            }

            bool ok = await EjecutarConsultaAsync(
                filtros,
                1,
                corte: null,
                limpiarResultadosSiFalla: true);

            if (ok)
                filtrosAplicados = filtros;
        }

        private async Task CambiarPaginaAsync(int incremento)
        {
            if (IsBusy || filtrosAplicados == null)
                return;

            int nuevaPagina = Pagina + incremento;

            if (nuevaPagina < 1 || nuevaPagina > TotalPaginas)
                return;

            bool ok = await EjecutarConsultaAsync(
                filtrosAplicados,
                nuevaPagina,
                corteConsultaUtc,
                limpiarResultadosSiFalla: false);

            if (ok)
                SolicitarScrollInicio?.Invoke(this, EventArgs.Empty);
        }

        private async Task<bool> EjecutarConsultaAsync(
            BitacoraFiltrosConsulta filtros,
            int paginaSolicitada,
            DateTime? corte,
            bool limpiarResultadosSiFalla)
        {
            if (!await ValidarInternetAsync())
                return false;

            using CancellationTokenSource cts = CrearCargaCts();
            EstablecerBusy(true);

            try
            {
                BitacoraPaginadaResponse? respuesta =
                    await ConsultarInternoAsync(
                        filtros,
                        paginaSolicitada,
                        corte,
                        cts.Token);

                if (respuesta == null)
                {
                    if (limpiarResultadosSiFalla)
                        LimpiarResultadosConsultaFallida();

                    return false;
                }

                corteConsultaUtc = respuesta.CorteConsultaUtc;
                return true;
            }
            catch (OperationCanceledException)
                when (cts.IsCancellationRequested)
            {
                return false;
            }
            finally
            {
                LiberarCargaCts(cts);
                EstablecerBusy(false);
            }
        }

        private async Task<BitacoraPaginadaResponse?> ConsultarInternoAsync(
            BitacoraFiltrosConsulta filtros,
            int paginaSolicitada,
            DateTime? corte,
            CancellationToken cancellationToken)
        {
            ApiResult<BitacoraPaginadaResponse> resultado =
                await apiService.ListarAsync(
                    ConvertirInicioDiaUtc(filtros.FechaDesde),
                    ConvertirFinDiaUtc(filtros.FechaHasta),
                    filtros.UsuarioId,
                    filtros.Accion,
                    filtros.Modulo,
                    filtros.Exitoso,
                    filtros.Buscar,
                    paginaSolicitada,
                    TamanoPagina,
                    corte,
                    cancellationToken);

            if (!resultado.Success || resultado.Data == null)
            {
                await MostrarErrorAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "No fue posible consultar la bitácora."
                        : resultado.Message);
                return null;
            }

            Registros = new ObservableCollection<BitacoraListadoItem>(
                resultado.Data.Items ?? new List<BitacoraListadoItem>());

            Pagina = resultado.Data.Pagina;
            TotalPaginas = resultado.Data.TotalPaginas;
            TotalRegistros = resultado.Data.TotalRegistros;
            consultaRealizada = true;
            ActualizarEstadoResultados();
            ActualizarPaginacion();

            return resultado.Data;
        }

        private async Task<bool> CargarCatalogosInternoAsync(
            CancellationToken cancellationToken)
        {
            ApiResult<BitacoraCatalogosResponse> resultado =
                await apiService.CatalogosAsync(cancellationToken);

            if (!resultado.Success || resultado.Data == null)
            {
                await MostrarErrorAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "No fue posible cargar los filtros de la bitácora."
                        : resultado.Message);
                return false;
            }

            Acciones = new ObservableCollection<string>(
                new[] { "Todas" }
                    .Concat(resultado.Data.Acciones ?? new List<string>())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.OrdinalIgnoreCase));

            Modulos = new ObservableCollection<string>(
                new[] { "Todos" }
                    .Concat(resultado.Data.Modulos ?? new List<string>())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.OrdinalIgnoreCase));

            ConstruirCatalogoUsuarios(resultado.Data.Usuarios);

            AccionSeleccionada = Acciones.FirstOrDefault() ?? "Todas";
            ModuloSeleccionado = Modulos.FirstOrDefault() ?? "Todos";
            SeleccionarPrimerUsuario();
            EstadoSeleccionado = Estados.FirstOrDefault() ?? "Todos";
            catalogosCargados = true;
            return true;
        }

        private void ConstruirCatalogoUsuarios(
            IEnumerable<BitacoraUsuarioFiltro>? usuariosApi)
        {
            opcionesUsuarios.Clear();
            opcionesUsuarios.Add(new BitacoraUsuarioFiltro
            {
                UsuarioId = null,
                Nombre = "Todos"
            });

            IEnumerable<BitacoraUsuarioFiltro> usuariosValidos =
                (usuariosApi ?? Enumerable.Empty<BitacoraUsuarioFiltro>())
                .Where(item =>
                    item.UsuarioId.HasValue &&
                    item.UsuarioId.Value > 0 &&
                    !string.IsNullOrWhiteSpace(item.Nombre))
                .GroupBy(item => item.UsuarioId)
                .Select(grupo => grupo.First())
                .OrderBy(item => item.Nombre)
                .ThenBy(item => item.UsuarioId);

            opcionesUsuarios.AddRange(usuariosValidos);

            HashSet<string> nombresRepetidos = opcionesUsuarios
                .Where(item => item.UsuarioId.HasValue)
                .GroupBy(
                    item => item.Nombre.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Where(grupo => grupo.Count() > 1)
                .Select(grupo => grupo.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Usuarios = new ObservableCollection<string>(
                opcionesUsuarios.Select(item =>
                {
                    string nombre = item.Nombre.Trim();

                    return item.UsuarioId.HasValue &&
                           nombresRepetidos.Contains(nombre)
                        ? $"{nombre} (ID {item.UsuarioId.Value})"
                        : nombre;
                }));
        }

        private void SeleccionarPrimerUsuario()
        {
            usuarioSeleccionadoIndex = Usuarios.Count > 0 ? 0 : -1;
            OnPropertyChanged(nameof(UsuarioSeleccionadoIndex));
        }

        private int? ObtenerUsuarioSeleccionadoId()
        {
            int indice = UsuarioSeleccionadoIndex;

            return indice >= 0 && indice < opcionesUsuarios.Count
                ? opcionesUsuarios[indice].UsuarioId
                : null;
        }

        private bool IntentarCrearFiltrosDesdeControles(
            out BitacoraFiltrosConsulta filtros,
            out string mensaje)
        {
            filtros = new BitacoraFiltrosConsulta();

            if (FechaHasta.Date < FechaDesde.Date)
            {
                mensaje = "La fecha final no puede ser menor que la fecha inicial.";
                return false;
            }

            string buscar = TextoBusqueda.Trim();
            if (buscar.Length > MaximoBusqueda)
            {
                mensaje = $"La búsqueda no puede superar {MaximoBusqueda} caracteres.";
                return false;
            }

            filtros = new BitacoraFiltrosConsulta
            {
                FechaDesde = FechaDesde.Date,
                FechaHasta = FechaHasta.Date,
                UsuarioId = ObtenerUsuarioSeleccionadoId(),
                Accion = AccionSeleccionada == "Todas"
                    ? null
                    : AccionSeleccionada,
                Modulo = ModuloSeleccionado == "Todos"
                    ? null
                    : ModuloSeleccionado,
                Exitoso = EstadoSeleccionado switch
                {
                    "Correctos" => true,
                    "Con error" => false,
                    _ => null
                },
                Buscar = buscar
            };

            mensaje = string.Empty;
            return true;
        }

        private async Task VerDetalleAsync(BitacoraListadoItem? item)
        {
            if (item == null || IsBusy || !CanView)
                return;

            await GoToAsyncParameters(
                AppRoutes.BitacoraDetalle,
                new Dictionary<string, object>
                {
                    ["BitacoraId"] = item.BitacoraId
                });
        }

        private static DateTime ConvertirInicioDiaUtc(DateTime fecha) =>
            DateTime.SpecifyKind(
                    fecha.Date,
                    DateTimeKind.Local)
                .ToUniversalTime();

        private static DateTime ConvertirFinDiaUtc(DateTime fecha) =>
            DateTime.SpecifyKind(
                    fecha.Date.AddDays(1).AddTicks(-1),
                    DateTimeKind.Local)
                .ToUniversalTime();

        private void LimpiarResultadosConsultaFallida()
        {
            consultaRealizada = false;
            Registros = new ObservableCollection<BitacoraListadoItem>();
            Pagina = 1;
            TotalPaginas = 1;
            TotalRegistros = 0;
            corteConsultaUtc = null;
            ActualizarEstadoResultados();
            ActualizarPaginacion();
        }

        private CancellationTokenSource CrearCargaCts()
        {
            CancelarCarga();
            cargaCts = new CancellationTokenSource();
            return cargaCts;
        }

        private void LiberarCargaCts(CancellationTokenSource cts)
        {
            if (ReferenceEquals(cargaCts, cts))
                cargaCts = null;
        }

        private void EstablecerBusy(bool valor)
        {
            IsBusy = valor;
            ActualizarEstadoResultados();
            ActualizarPaginacion();
            ActualizarComandos();
        }

        private void ActualizarEstadoResultados()
        {
            OnPropertyChanged(nameof(HayRegistros));
            OnPropertyChanged(nameof(TieneConsultaRealizada));
            OnPropertyChanged(nameof(MostrarSinResultados));
            OnPropertyChanged(nameof(MostrarAyudaDetalle));
            OnPropertyChanged(nameof(MostrarResumenResultados));
            OnPropertyChanged(nameof(MostrarPaginacion));
        }

        private void ActualizarPaginacion()
        {
            OnPropertyChanged(nameof(PuedeAnterior));
            OnPropertyChanged(nameof(PuedeSiguiente));
            OnPropertyChanged(nameof(ResumenPagina));
            OnPropertyChanged(nameof(MostrarPaginacion));
            AnteriorCommand?.ChangeCanExecute();
            SiguienteCommand?.ChangeCanExecute();
        }

        private void ActualizarComandos()
        {
            BuscarCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            LimpiarCommand.ChangeCanExecute();
            VerDetalleCommand.ChangeCanExecute();
            AnteriorCommand.ChangeCanExecute();
            SiguienteCommand.ChangeCanExecute();
        }

        private sealed class BitacoraFiltrosConsulta
        {
            public DateTime FechaDesde { get; init; }
            public DateTime FechaHasta { get; init; }
            public int? UsuarioId { get; init; }
            public string? Accion { get; init; }
            public string? Modulo { get; init; }
            public bool? Exitoso { get; init; }
            public string Buscar { get; init; } = string.Empty;
        }
    }
}
