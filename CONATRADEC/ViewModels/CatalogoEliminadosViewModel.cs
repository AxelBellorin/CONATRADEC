using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class CatalogoEliminadosViewModel : GlobalService
    {
        private readonly CatalogoEliminadoConfiguracion configuracion;
        private readonly CatalogosEliminadosApiService apiService;
        private readonly UsuariosInactivosApiService usuariosInactivosApiService;
        private readonly TerrenosInactivosApiService terrenosInactivosApiService;
        private readonly List<CatalogoEliminadoItem> originales = new();

        private CancellationTokenSource? cargaCts;
        private string textoBusqueda = string.Empty;
        private string textoBusquedaAplicado = string.Empty;
        private string mensaje = string.Empty;
        private bool isRefreshing;
        private bool cargandoInicial;
        private bool cargandoListado;
        private bool mostrandoRelay;
        private string tituloRelay = "Procesando...";
        private string detalleRelay = "Espere un momento.";
        private bool pantallaCargada;
        private int paginaActual = 1;
        private int totalPaginas = 1;
        private int totalRegistros;
        private int tamanoPaginaActual;

        public CatalogoEliminadosViewModel(
            CatalogoEliminadoConfiguracion configuracion)
            : this(
                configuracion,
                new CatalogosEliminadosApiService(),
                new UsuariosInactivosApiService(),
                new TerrenosInactivosApiService())
        {
        }

        // Se conserva la firma anterior para no romper pruebas ni consumidores
        // que construyen el ViewModel con los servicios de Usuarios.
        public CatalogoEliminadosViewModel(
            CatalogoEliminadoConfiguracion configuracion,
            CatalogosEliminadosApiService apiService,
            UsuariosInactivosApiService usuariosInactivosApiService)
            : this(
                configuracion,
                apiService,
                usuariosInactivosApiService,
                new TerrenosInactivosApiService())
        {
        }

        public CatalogoEliminadosViewModel(
            CatalogoEliminadoConfiguracion configuracion,
            CatalogosEliminadosApiService apiService,
            UsuariosInactivosApiService usuariosInactivosApiService,
            TerrenosInactivosApiService terrenosInactivosApiService)
        {
            this.configuracion = configuracion ??
                throw new ArgumentNullException(nameof(configuracion));
            this.apiService = apiService ??
                throw new ArgumentNullException(nameof(apiService));
            this.usuariosInactivosApiService = usuariosInactivosApiService ??
                throw new ArgumentNullException(nameof(usuariosInactivosApiService));
            this.terrenosInactivosApiService = terrenosInactivosApiService ??
                throw new ArgumentNullException(nameof(terrenosInactivosApiService));

            // Los catálogos paginados abren como modal. Se prepara el relay
            // desde la construcción para que la consulta inicial sea visible
            // desde el primer frame de Windows y Android.
            cargandoInicial = UsaPaginacionServidor;
            mostrandoRelay = UsaPaginacionServidor;

            if (mostrandoRelay)
            {
                tituloRelay = "Cargando registros eliminados...";
                detalleRelay = "Consultando información actual del servidor";
            }

            BuscarCommand = new Command(
                async () => await EjecutarBusquedaAsync(),
                () => CanView && !IsBusy);

            LimpiarCommand = new Command(
                async () => await LimpiarFiltroAsync(),
                () => CanView && !IsBusy);

            RefrescarCommand = new Command(
                async () => await RefrescarAsync(),
                () => CanView && !IsBusy);

            ReactivarCommand = new Command<CatalogoEliminadoItem>(
                async item => await ReactivarAsync(item),
                item => item != null && CanEdit && !IsBusy);

            PaginaAnteriorCommand = new Command(
                async () => await IrPaginaAnteriorAsync(),
                () => UsaPaginacionServidor && PuedeIrAnterior && !IsBusy);

            PaginaSiguienteCommand = new Command(
                async () => await IrPaginaSiguienteAsync(),
                () => UsaPaginacionServidor && PuedeIrSiguiente && !IsBusy);

            CerrarCommand = new Command(
                async () => await CerrarAsync(),
                () => !IsBusy);
        }

        public ObservableCollection<CatalogoEliminadoItem> Registros { get; } = new();

        public Command BuscarCommand { get; }
        public Command LimpiarCommand { get; }
        public Command RefrescarCommand { get; }
        public Command<CatalogoEliminadoItem> ReactivarCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }
        public Command CerrarCommand { get; }

        public string Titulo => configuracion.Titulo;
        public string Descripcion => configuracion.Descripcion;

        public string PlaceholderBusqueda =>
            $"Buscar {configuracion.Singular} eliminado";

        private bool EsUsuario =>
            string.Equals(
                configuracion.Codigo,
                CatalogoEliminadoCodigos.Usuario,
                StringComparison.OrdinalIgnoreCase);

        private bool EsTerreno =>
            string.Equals(
                configuracion.Codigo,
                CatalogoEliminadoCodigos.Terreno,
                StringComparison.OrdinalIgnoreCase);

        private bool UsaPaginacionServidor => EsUsuario || EsTerreno;

        private string NombrePlural =>
            EsUsuario
                ? "usuarios eliminados"
                : EsTerreno
                    ? "terrenos eliminados"
                    : "registros eliminados";

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

        public bool CargandoInicial
        {
            get => cargandoInicial;
            private set
            {
                if (cargandoInicial == value)
                    return;

                cargandoInicial = value;
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
                string nuevoValor = value ?? string.Empty;
                if (tituloRelay == nuevoValor)
                    return;

                tituloRelay = nuevoValor;
                OnPropertyChanged();
            }
        }

        public string DetalleRelay
        {
            get => detalleRelay;
            private set
            {
                string nuevoValor = value ?? string.Empty;
                if (detalleRelay == nuevoValor)
                    return;

                detalleRelay = nuevoValor;
                OnPropertyChanged();
            }
        }

        public bool TieneMensaje => !string.IsNullOrWhiteSpace(Mensaje);
        public bool MostrarAccesoDenegado => !CanView;

        public bool MostrarVacio =>
            CanView &&
            pantallaCargada &&
            !IsBusy &&
            Registros.Count == 0 &&
            !TieneMensaje;

        public int TotalRegistros
        {
            get => totalRegistros;
            private set
            {
                if (totalRegistros == value)
                    return;

                totalRegistros = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Resumen));
                OnPropertyChanged(nameof(RangoPaginaTexto));
                OnPropertyChanged(nameof(MostrarPaginacion));
            }
        }

        public int PaginaActual => paginaActual;
        public int TotalPaginas => totalPaginas;
        public bool PuedeIrAnterior => pantallaCargada && paginaActual > 1;
        public bool PuedeIrSiguiente => pantallaCargada && paginaActual < totalPaginas;

        public bool MostrarPaginacion =>
            UsaPaginacionServidor &&
            CanView &&
            pantallaCargada &&
            Registros.Count > 0;

        public string PaginaTexto =>
            $"Página {Math.Max(1, paginaActual)} de {Math.Max(1, totalPaginas)}";

        public string RangoPaginaTexto
        {
            get
            {
                if (TotalRegistros <= 0 || Registros.Count == 0)
                    return "Sin registros en esta página";

                int inicio =
                    ((Math.Max(1, paginaActual) - 1) *
                     Math.Max(1, tamanoPaginaActual)) + 1;

                int fin = Math.Min(
                    inicio + Registros.Count - 1,
                    TotalRegistros);

                return $"Mostrando {inicio}-{fin} de {TotalRegistros}";
            }
        }

        public string Resumen =>
            TotalRegistros == 1
                ? "1 registro eliminado"
                : $"{TotalRegistros} registros eliminados";

        public async Task InicializarAsync()
        {
            LoadPagePermissions(configuracion.Interfaz);

            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            ActualizarComandos();
            NotificarEstado();

            if (!CanView)
            {
                CargandoInicial = false;
                OcultarRelay();
                return;
            }

            if (pantallaCargada)
            {
                CargandoInicial = false;
                OcultarRelay();
                return;
            }

            textoBusquedaAplicado = string.Empty;
            tamanoPaginaActual = ObtenerTamanoPagina();

            if (UsaPaginacionServidor)
            {
                await CargarPaginaServidorAsync(
                    1,
                    true,
                    "Cargando registros eliminados...",
                    "Consultando información actual del servidor");
            }
            else
            {
                await CargarCatalogoCompletoAsync(
                    true,
                    "Cargando registros eliminados...",
                    "Consultando información actual del servidor");
            }
        }

        public void CancelarCarga()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(ref cargaCts, null);

            if (source == null)
                return;

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private async Task EjecutarBusquedaAsync()
        {
            if (!CanView || IsBusy)
                return;

            if (UsaPaginacionServidor)
            {
                textoBusquedaAplicado = TextoBusqueda.Trim();

                await CargarPaginaServidorAsync(
                    1,
                    false,
                    $"Buscando {NombrePlural}...",
                    "Consultando los registros que coinciden con la búsqueda");
                return;
            }

            AplicarFiltroLocal();
        }

        private async Task LimpiarFiltroAsync()
        {
            if (!CanView || IsBusy)
                return;

            TextoBusqueda = string.Empty;

            if (UsaPaginacionServidor)
            {
                textoBusquedaAplicado = string.Empty;
                await CargarPaginaServidorAsync(
                    1,
                    false,
                    $"Actualizando {NombrePlural}...",
                    "Quitando filtros y consultando la primera página");
                return;
            }

            AplicarFiltroLocal();
        }

        private async Task RefrescarAsync()
        {
            if (!CanView || IsBusy)
                return;

            IsRefreshing = true;

            try
            {
                if (UsaPaginacionServidor)
                {
                    await CargarPaginaServidorAsync(
                        Math.Max(1, paginaActual),
                        false,
                        $"Actualizando {NombrePlural}...",
                        "Consultando nuevamente la página actual");
                }
                else
                {
                    await CargarCatalogoCompletoAsync(
                        false,
                        "Actualizando registros eliminados...",
                        "Consultando nuevamente la información del servidor");
                }
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task IrPaginaAnteriorAsync()
        {
            if (!UsaPaginacionServidor || !PuedeIrAnterior)
                return Task.CompletedTask;

            return CargarPaginaServidorAsync(
                paginaActual - 1,
                false,
                "Cargando página anterior...",
                $"Consultando la página anterior de {NombrePlural}");
        }

        private Task IrPaginaSiguienteAsync()
        {
            if (!UsaPaginacionServidor || !PuedeIrSiguiente)
                return Task.CompletedTask;

            return CargarPaginaServidorAsync(
                paginaActual + 1,
                false,
                "Cargando página siguiente...",
                $"Consultando la siguiente página de {NombrePlural}");
        }

        private async Task CargarPaginaServidorAsync(
            int paginaSolicitada,
            bool cargaInicial,
            string? tituloOperacion = null,
            string? detalleOperacion = null)
        {
            if (!CanView || IsBusy)
                return;

            paginaSolicitada = Math.Max(1, paginaSolicitada);
            CancellationTokenSource source = PrepararCarga();

            try
            {
                MostrarRelay(
                    tituloOperacion ??
                        (cargaInicial
                            ? "Cargando registros eliminados..."
                            : "Actualizando registros eliminados..."),
                    detalleOperacion ??
                        "Consultando información actual del servidor");

                IsBusy = true;
                CargandoInicial = cargaInicial;
                CargandoListado = !cargaInicial;
                Mensaje = string.Empty;
                ActualizarComandos();
                NotificarEstado();

                PaginaEliminados? pagina =
                    await ConsultarPaginaServidorAsync(
                        paginaSolicitada,
                        source.Token);

                if (source.IsCancellationRequested || !EsCargaActual(source))
                    return;

                if (pagina == null)
                    return;

                AplicarPaginaServidor(pagina);
                pantallaCargada = true;
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al cerrar el modal o reemplazar la carga.
            }
            catch (Exception ex)
            {
                Mensaje =
                    $"Ocurrió un error inesperado al cargar {NombrePlural}.";
                await MostrarToastAsync("Error: " + ex.Message);
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    IsBusy = false;
                    CargandoInicial = false;
                    CargandoListado = false;
                    IsRefreshing = false;
                    OcultarRelay();
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstado();
            }
        }

        private async Task<PaginaEliminados?> ConsultarPaginaServidorAsync(
            int paginaSolicitada,
            CancellationToken cancellationToken)
        {
            if (EsUsuario)
            {
                ApiResult<UsuarioInactivoPaginaResponse> resultado =
                    await usuariosInactivosApiService.BuscarAsync(
                        textoBusquedaAplicado,
                        paginaSolicitada,
                        ObtenerTamanoPagina(),
                        cancellationToken);

                if (!resultado.Success || resultado.Data == null)
                {
                    AplicarErrorCarga(
                        resultado.Message,
                        "No fue posible cargar los usuarios inactivos.");
                    return null;
                }

                UsuarioInactivoPaginaResponse data = resultado.Data;
                return new PaginaEliminados(
                    data.Items,
                    data.PaginaActual,
                    data.TamanoPagina,
                    data.TotalRegistros,
                    data.TotalPaginas);
            }

            if (EsTerreno)
            {
                ApiResult<TerrenoInactivoPaginaResponse> resultado =
                    await terrenosInactivosApiService.BuscarAsync(
                        textoBusquedaAplicado,
                        paginaSolicitada,
                        ObtenerTamanoPagina(),
                        cancellationToken);

                if (!resultado.Success || resultado.Data == null)
                {
                    AplicarErrorCarga(
                        resultado.Message,
                        "No fue posible cargar los terrenos eliminados.");
                    return null;
                }

                TerrenoInactivoPaginaResponse data = resultado.Data;
                return new PaginaEliminados(
                    data.Items,
                    data.PaginaActual,
                    data.TamanoPagina,
                    data.TotalRegistros,
                    data.TotalPaginas);
            }

            return null;
        }

        private void AplicarErrorCarga(
            string? mensajeResultado,
            string mensajePredeterminado)
        {
            Registros.Clear();
            TotalRegistros = 0;
            paginaActual = 1;
            totalPaginas = 1;
            pantallaCargada = true;

            Mensaje = string.IsNullOrWhiteSpace(mensajeResultado)
                ? mensajePredeterminado
                : mensajeResultado;
        }

        private void AplicarPaginaServidor(PaginaEliminados pagina)
        {
            Registros.Clear();

            foreach (CatalogoEliminadoItem item in pagina.Items)
            {
                if (item.Id > 0 && !item.Activo)
                    Registros.Add(item);
            }

            paginaActual = Math.Max(1, pagina.PaginaActual);
            totalPaginas = Math.Max(1, pagina.TotalPaginas);
            tamanoPaginaActual = pagina.TamanoPagina > 0
                ? pagina.TamanoPagina
                : ObtenerTamanoPagina();
            TotalRegistros = Math.Max(0, pagina.TotalRegistros);
            Mensaje = string.Empty;
            NotificarEstado();
        }

        private async Task CargarCatalogoCompletoAsync(
            bool cargaInicial,
            string? tituloOperacion = null,
            string? detalleOperacion = null)
        {
            if (!CanView || IsBusy)
                return;

            CancellationTokenSource source = PrepararCarga();

            try
            {
                MostrarRelay(
                    tituloOperacion ??
                        (cargaInicial
                            ? "Cargando registros eliminados..."
                            : "Actualizando registros eliminados..."),
                    detalleOperacion ??
                        "Consultando información actual del servidor");

                IsBusy = true;
                CargandoInicial = cargaInicial;
                CargandoListado = !cargaInicial;
                Mensaje = string.Empty;
                ActualizarComandos();
                NotificarEstado();

                ApiResult<ObservableCollection<CatalogoEliminadoItem>> resultado =
                    await apiService.ListarAsync(
                        configuracion.Codigo,
                        source.Token);

                if (source.IsCancellationRequested || !EsCargaActual(source))
                    return;

                if (!resultado.Success || resultado.Data == null)
                {
                    originales.Clear();
                    Registros.Clear();
                    TotalRegistros = 0;
                    pantallaCargada = true;
                    Mensaje = string.IsNullOrWhiteSpace(resultado.Message)
                        ? "No fue posible cargar los registros eliminados."
                        : resultado.Message;
                    return;
                }

                originales.Clear();
                originales.AddRange(
                    resultado.Data
                        .Where(item => item.Id > 0 && !item.Activo)
                        .OrderBy(item => item.Titulo));

                pantallaCargada = true;
                AplicarFiltroLocal();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Mensaje =
                    "Ocurrió un error inesperado al cargar los registros eliminados.";
                await MostrarToastAsync("Error: " + ex.Message);
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    IsBusy = false;
                    CargandoInicial = false;
                    CargandoListado = false;
                    IsRefreshing = false;
                    OcultarRelay();
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstado();
            }
        }

        private void AplicarFiltroLocal()
        {
            string filtro = TextoBusqueda.Trim();
            IEnumerable<CatalogoEliminadoItem> consulta = originales;

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                consulta = consulta.Where(item =>
                    Contiene(item.Titulo, filtro) ||
                    Contiene(item.Subtitulo, filtro) ||
                    Contiene(item.Detalle, filtro) ||
                    Contiene(item.Codigo, filtro));
            }

            Registros.Clear();
            foreach (CatalogoEliminadoItem item in consulta)
                Registros.Add(item);

            paginaActual = 1;
            totalPaginas = 1;
            tamanoPaginaActual = Math.Max(1, Registros.Count);
            TotalRegistros = Registros.Count;
            Mensaje = string.Empty;
            NotificarEstado();
        }

        private async Task ReactivarAsync(CatalogoEliminadoItem? item)
        {
            if (item == null || item.Id <= 0 || IsBusy)
                return;

            if (!CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permiso para reactivar este registro.");
                return;
            }

            Page? pagina =
                Application.Current?
                    .Windows
                    .FirstOrDefault()?
                    .Page;

            if (pagina == null)
                return;

            bool confirmar = await pagina.DisplayAlert(
                "Reactivar registro",
                $"¿Desea reactivar '{item.Titulo}' conservando su identificador e historial?",
                "Reactivar",
                "Cancelar");

            if (!confirmar)
                return;

            bool recargarPagina = false;
            int paginaDestino = Math.Max(1, paginaActual);
            string mensajeExito = "Registro reactivado correctamente.";

            try
            {
                MostrarRelay(
                    $"Reactivando {configuracion.Singular}...",
                    "Restaurando el registro en el servidor");

                IsBusy = true;
                ActualizarComandos();
                NotificarEstado();

                ApiResult<bool> resultado =
                    await apiService.ReactivarAsync(
                        configuracion.Codigo,
                        item.Id);

                if (!resultado.Success)
                {
                    await MostrarToastAsync(
                        string.IsNullOrWhiteSpace(resultado.Message)
                            ? "No fue posible reactivar el registro."
                            : resultado.Message);
                    return;
                }

                mensajeExito = string.IsNullOrWhiteSpace(resultado.Message)
                    ? mensajeExito
                    : resultado.Message;

                if (UsaPaginacionServidor)
                {
                    Registros.Remove(item);
                    TotalRegistros = Math.Max(0, TotalRegistros - 1);
                    RecalcularPaginasServidor();

                    if (EsUsuario)
                        UsuarioVisitaService.MarcarListadoParaRecargar();
                    else if (EsTerreno)
                        TerrenoVisitaService.MarcarListadoParaRecargar();

                    if (Registros.Count == 0 &&
                        TotalRegistros > 0 &&
                        paginaActual > 1)
                    {
                        paginaDestino = Math.Min(
                            paginaActual - 1,
                            Math.Max(1, totalPaginas));
                        recargarPagina = true;
                    }
                }
                else
                {
                    originales.RemoveAll(registro => registro.Id == item.Id);
                    Registros.Remove(item);
                    TotalRegistros = Registros.Count;
                }
            }
            finally
            {
                IsBusy = false;
                OcultarRelay();
                ActualizarComandos();
                NotificarEstado();
            }

            if (recargarPagina)
            {
                await CargarPaginaServidorAsync(
                    paginaDestino,
                    false,
                    $"Actualizando {NombrePlural}...",
                    "Ajustando la página después de la reactivación");
            }

            await MostrarToastAsync(mensajeExito);
        }

        private void RecalcularPaginasServidor()
        {
            int tamano = Math.Max(1, tamanoPaginaActual);

            totalPaginas = TotalRegistros == 0
                ? 1
                : (int)Math.Ceiling(TotalRegistros / (double)tamano);

            paginaActual = Math.Min(
                Math.Max(1, paginaActual),
                Math.Max(1, totalPaginas));

            NotificarEstado();
        }

        private static bool Contiene(string? valor, string filtro) =>
            (valor ?? string.Empty)
                .Contains(filtro, StringComparison.OrdinalIgnoreCase);

        private async Task CerrarAsync()
        {
            if (IsBusy || Shell.Current?.Navigation == null)
                return;

            try
            {
                MostrarRelay(
                    "Regresando...",
                    UsaPaginacionServidor
                        ? $"Cerrando {NombrePlural} y volviendo al listado"
                        : "Cerrando los registros eliminados");

                IsBusy = true;
                ActualizarComandos();
                await Task.Yield();
                await Shell.Current.Navigation.PopModalAsync();
            }
            finally
            {
                IsBusy = false;
                OcultarRelay();
                ActualizarComandos();
                NotificarEstado();
            }
        }

        private CancellationTokenSource PrepararCarga()
        {
            var source = new CancellationTokenSource();
            CancellationTokenSource? anterior =
                Interlocked.Exchange(ref cargaCts, source);

            if (anterior != null)
            {
                try
                {
                    anterior.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            return source;
        }

        private bool EsCargaActual(CancellationTokenSource source) =>
            ReferenceEquals(Volatile.Read(ref cargaCts), source);

        private void LiberarCarga(CancellationTokenSource source)
        {
            Interlocked.CompareExchange(ref cargaCts, null, source);
            source.Dispose();
        }

        private void MostrarRelay(string titulo, string detalle)
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
            LimpiarCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            ReactivarCommand.ChangeCanExecute();
            PaginaAnteriorCommand.ChangeCanExecute();
            PaginaSiguienteCommand.ChangeCanExecute();
            CerrarCommand.ChangeCanExecute();
        }

        private void NotificarEstado()
        {
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(Resumen));
            OnPropertyChanged(nameof(TieneMensaje));
            OnPropertyChanged(nameof(PaginaActual));
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(PuedeIrAnterior));
            OnPropertyChanged(nameof(PuedeIrSiguiente));
            OnPropertyChanged(nameof(MostrarPaginacion));
            OnPropertyChanged(nameof(PaginaTexto));
            OnPropertyChanged(nameof(RangoPaginaTexto));
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Platform == DevicePlatform.WinUI ? 40 : 20;

        private sealed record PaginaEliminados(
            IReadOnlyCollection<CatalogoEliminadoItem> Items,
            int PaginaActual,
            int TamanoPagina,
            int TotalRegistros,
            int TotalPaginas);
    }
}
