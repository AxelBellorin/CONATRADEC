using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class ExtraccionNutrienteViewModel : GlobalService
    {
        private readonly ExtraccionNutrienteConsultaApiService consultaApiService;
        private readonly ExtraccionNutrienteApiService apiService;

        private CancellationTokenSource? cargaCts;
        private CancellationTokenSource? accionCts;

        private string textoBusqueda = string.Empty;
        private string textoBusquedaAplicado = string.Empty;
        private string mensaje = string.Empty;
        private bool isRefreshing;
        private bool navegando;
        private bool pantallaCargada;
        private int paginaActual = 1;
        private int totalPaginas = 1;
        private int totalRegistros;
        private int tamanoPaginaActual;
        private int versionAplicada = -1;
        private int eliminacionEnCurso;

        public ExtraccionNutrienteViewModel()
            : this(
                new ExtraccionNutrienteConsultaApiService(),
                new ExtraccionNutrienteApiService())
        {
        }

        public ExtraccionNutrienteViewModel(
            ExtraccionNutrienteConsultaApiService consultaApiService,
            ExtraccionNutrienteApiService apiService)
        {
            this.consultaApiService = consultaApiService
                ?? throw new ArgumentNullException(
                    nameof(consultaApiService));

            this.apiService = apiService
                ?? throw new ArgumentNullException(
                    nameof(apiService));

            tamanoPaginaActual =
                ObtenerTamanoPagina();

            RegresarConfiguracionCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    () => NavegarAsync(AppRoutes.Configuracion),
                    "regresar a configuración"),
                () => !IsBusy && !Navegando);

            AddCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    AddAsync,
                    "abrir el formulario de extracción"),
                () => CanAdd && !IsBusy && !Navegando);

            EditCommand = new Command<ExtraccionNutrienteResponse>(
                async item => await EjecutarSeguroAsync(
                    () => OpenAsync(
                        item,
                        FormMode.FormModeSelect.Edit),
                    "editar el parámetro de extracción"),
                item =>
                    item != null &&
                    CanEdit &&
                    !IsBusy &&
                    !Navegando);

            ViewCommand = new Command<ExtraccionNutrienteResponse>(
                async item => await EjecutarSeguroAsync(
                    () => OpenAsync(
                        item,
                        FormMode.FormModeSelect.View),
                    "consultar el parámetro de extracción"),
                item =>
                    item != null &&
                    CanView &&
                    !IsBusy &&
                    !Navegando);

            DeleteCommand = new Command<ExtraccionNutrienteResponse>(
                async item => await EjecutarSeguroAsync(
                    () => DeleteAsync(item),
                    "eliminar el parámetro de extracción"),
                item =>
                    item != null &&
                    CanDelete &&
                    !IsBusy &&
                    !Navegando &&
                    Volatile.Read(ref eliminacionEnCurso) == 0);

            BuscarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    AplicarBusquedaAsync,
                    "buscar parámetros de extracción"),
                () => CanView && !IsBusy && !Navegando);

            LimpiarFiltrosCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    LimpiarFiltrosAsync,
                    "limpiar la búsqueda"),
                () => CanView && !IsBusy && !Navegando);

            RefrescarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RefrescarAsync,
                    "actualizar los parámetros"),
                () => CanView && !IsBusy && !Navegando);

            PaginaAnteriorCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    IrPaginaAnteriorAsync,
                    "cargar la página anterior"),
                () =>
                    CanView &&
                    PuedeIrAnterior &&
                    !IsBusy &&
                    !Navegando);

            PaginaSiguienteCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    IrPaginaSiguienteAsync,
                    "cargar la página siguiente"),
                () =>
                    CanView &&
                    PuedeIrSiguiente &&
                    !IsBusy &&
                    !Navegando);
        }

        public event EventHandler? SolicitarDesplazamientoInicio;

        public ObservableCollection<ExtraccionNutrienteResponse> List { get; } =
            new();

        public Command RegresarConfiguracionCommand { get; }
        public Command AddCommand { get; }
        public Command<ExtraccionNutrienteResponse> EditCommand { get; }
        public Command<ExtraccionNutrienteResponse> ViewCommand { get; }
        public Command<ExtraccionNutrienteResponse> DeleteCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }

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

        public string ResumenResultados =>
            TotalRegistros == 1
                ? "1 parámetro encontrado"
                : $"{TotalRegistros} parámetros encontrados";

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
            CanView &&
            pantallaCargada &&
            List.Count > 0;

        public string PaginaTexto =>
            $"Página {Math.Max(1, paginaActual)} de {Math.Max(1, totalPaginas)}";

        public string RangoPaginaTexto
        {
            get
            {
                if (TotalRegistros <= 0 ||
                    List.Count == 0)
                {
                    return "Sin registros en esta página";
                }

                int tamano =
                    Math.Max(1, tamanoPaginaActual);

                int inicio =
                    ((Math.Max(1, paginaActual) - 1) * tamano) + 1;

                int fin =
                    Math.Min(
                        inicio + List.Count - 1,
                        TotalRegistros);

                return $"Mostrando {inicio}-{fin} de {TotalRegistros}";
            }
        }

        public bool MostrarVacio =>
            CanView &&
            pantallaCargada &&
            !IsBusy &&
            List.Count == 0 &&
            !TieneMensaje;

        public bool MostrarAccesoDenegado =>
            !CanView;

        public void ActualizarPermisos()
        {
            LoadPagePermissions("extraccionNutrientePage");

            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            NotificarEstadoLista();
            ActualizarComandos();
        }

        /// <summary>
        /// Una visita nueva siempre comienza en página 1, sin búsqueda y con
        /// información fresca obtenida desde el servidor.
        /// </summary>
        public async Task IniciarNuevaVisitaAsync()
        {
            if (!CanView || Navegando)
                return;

            CancelarCarga();

            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;
            Mensaje = string.Empty;
            paginaActual = 1;
            totalPaginas = 1;
            TotalRegistros = 0;
            tamanoPaginaActual = ObtenerTamanoPagina();
            pantallaCargada = false;
            versionAplicada = -1;

            List.Clear();
            NotificarEstadoLista();

            await CargarPaginaAsync(
                1,
                cargaInicial: true);
        }

        /// <summary>
        /// Ver, Crear, Editar y Eliminados son subflujos de la misma visita.
        /// Al regresar solo se consulta nuevamente si hubo una mutación real.
        /// </summary>
        public async Task InicializarAsync()
        {
            if (!CanView || Navegando)
                return;

            if (!pantallaCargada)
            {
                await CargarPaginaAsync(
                    1,
                    cargaInicial: true);
                return;
            }

            int versionActual =
                ExtraccionNutrienteListadoEstadoService.VersionActual;

            if (versionAplicada != versionActual)
            {
                await CargarPaginaAsync(
                    Math.Max(1, paginaActual));
            }
        }

        public void FinalizarVisita()
        {
            CancelarCarga();

            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;
            Mensaje = string.Empty;
            paginaActual = 1;
            totalPaginas = 1;
            TotalRegistros = 0;
            tamanoPaginaActual = ObtenerTamanoPagina();
            pantallaCargada = false;
            versionAplicada = -1;

            List.Clear();
            NotificarEstadoLista();
        }

        public void CancelarCarga()
        {
            CancellationTokenSource? carga =
                Interlocked.Exchange(
                    ref cargaCts,
                    null);

            CancellationTokenSource? accion =
                Interlocked.Exchange(
                    ref accionCts,
                    null);

            CancelarSeguro(carga);
            CancelarSeguro(accion);

            IsBusy = false;
            IsRefreshing = false;
            ActualizarComandos();
        }

        private async Task AplicarBusquedaAsync()
        {
            textoBusquedaAplicado =
                (TextoBusqueda ?? string.Empty)
                    .Trim();

            await CargarPaginaAsync(
                1,
                desplazarAlInicio: true);
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;

            await CargarPaginaAsync(
                1,
                desplazarAlInicio: true);
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;

            try
            {
                await CargarPaginaAsync(
                    Math.Max(1, paginaActual));
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
                desplazarAlInicio: true);
        }

        private Task IrPaginaSiguienteAsync()
        {
            if (!PuedeIrSiguiente)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                paginaActual + 1,
                desplazarAlInicio: true);
        }

        /// <summary>
        /// Mantiene una sola página en memoria. Si el total cambió mientras la
        /// visita estaba abierta, corrige la página solicitada hacia la última
        /// página válida antes de reemplazar la colección.
        /// </summary>
        private async Task CargarPaginaAsync(
            int paginaSolicitada,
            bool cargaInicial = false,
            bool desplazarAlInicio = false)
        {
            if (!CanView ||
                Navegando ||
                IsBusy)
            {
                return;
            }

            paginaSolicitada =
                Math.Max(1, paginaSolicitada);

            CancellationTokenSource source =
                PrepararNuevaCarga();

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;
                ActualizarComandos();

                int tamanoPagina =
                    ObtenerTamanoPagina();

                ApiResult<ExtraccionNutrientePaginaResponse> resultado =
                    await consultaApiService.BuscarAsync(
                        textoBusquedaAplicado,
                        paginaSolicitada,
                        tamanoPagina,
                        source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source))
                {
                    return;
                }

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    if (!EsMensajeCancelacion(resultado.Message))
                        Mensaje = resultado.Message;

                    return;
                }

                ExtraccionNutrientePaginaResponse pagina =
                    resultado.Data;

                int paginasServidor =
                    Math.Max(1, pagina.TotalPaginas);

                if (paginaSolicitada > paginasServidor &&
                    pagina.TotalRegistros > 0)
                {
                    resultado =
                        await consultaApiService.BuscarAsync(
                            textoBusquedaAplicado,
                            paginasServidor,
                            tamanoPagina,
                            source.Token);

                    if (source.IsCancellationRequested ||
                        !EsCargaActual(source))
                    {
                        return;
                    }

                    if (!resultado.Success ||
                        resultado.Data == null)
                    {
                        if (!EsMensajeCancelacion(resultado.Message))
                            Mensaje = resultado.Message;

                        return;
                    }

                    pagina = resultado.Data;
                }

                AplicarPagina(pagina);
                pantallaCargada = true;
                versionAplicada =
                    ExtraccionNutrienteListadoEstadoService.VersionActual;

                if (!cargaInicial && desplazarAlInicio)
                {
                    SolicitarDesplazamientoInicio?.Invoke(
                        this,
                        EventArgs.Empty);
                }
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
                    Mensaje =
                        "No fue posible cargar los parámetros de extracción.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los parámetros de extracción",
                        ex);
                }
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    IsBusy = false;
                    IsRefreshing = false;
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        private void AplicarPagina(
            ExtraccionNutrientePaginaResponse pagina)
        {
            List.Clear();

            foreach (ExtraccionNutrienteResponse item
                     in pagina.Items)
            {
                if (item.ParametroExtraccionNutrienteCafeId > 0)
                    List.Add(item);
            }

            paginaActual =
                Math.Max(1, pagina.PaginaActual);

            totalPaginas =
                Math.Max(1, pagina.TotalPaginas);

            tamanoPaginaActual =
                pagina.TamanoPagina > 0
                    ? pagina.TamanoPagina
                    : ObtenerTamanoPagina();

            TotalRegistros =
                Math.Max(0, pagina.TotalRegistros);

            Mensaje = string.Empty;
            NotificarEstadoLista();
        }

        private Task AddAsync() =>
            NavegarAsync(
                AppRoutes.ExtraccionNutrienteFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["Item"] = new ExtraccionNutrienteRequest()
                });

        private async Task OpenAsync(
            ExtraccionNutrienteResponse? item,
            FormMode.FormModeSelect mode)
        {
            if (item?.ParametroExtraccionNutrienteCafeId is not > 0 ||
                IsBusy ||
                Navegando)
            {
                return;
            }

            CancellationTokenSource source =
                PrepararNuevaAccion();

            bool accionLiberada = false;

            try
            {
                IsBusy = true;
                ActualizarComandos();

                ApiResult<ExtraccionNutrienteResponse> resultado =
                    await consultaApiService.GetByIdAsync(
                        item.ParametroExtraccionNutrienteCafeId,
                        source.Token);

                if (source.IsCancellationRequested ||
                    !EsAccionActual(source))
                {
                    return;
                }

                if (!resultado.Success ||
                    resultado.Data?.ParametroExtraccionNutrienteCafeId is not > 0)
                {
                    if (!EsMensajeCancelacion(resultado.Message))
                    {
                        await MostrarToastAsync(
                            string.IsNullOrWhiteSpace(resultado.Message)
                                ? "No fue posible cargar el parámetro de extracción."
                                : resultado.Message);
                    }

                    return;
                }

                ExtraccionNutrienteRequest actual =
                    new(resultado.Data);

                IsBusy = false;
                LiberarAccion(source);
                accionLiberada = true;

                await NavegarAsync(
                    AppRoutes.ExtraccionNutrienteFormulario,
                    new Dictionary<string, object>
                    {
                        ["Mode"] = mode,
                        ["Item"] = actual
                    });
            }
            finally
            {
                if (!accionLiberada)
                {
                    IsBusy = false;
                    LiberarAccion(source);
                }

                ActualizarComandos();
            }
        }

        private async Task DeleteAsync(
            ExtraccionNutrienteResponse? item)
        {
            if (item?.ParametroExtraccionNutrienteCafeId is not > 0 ||
                IsBusy ||
                Interlocked.CompareExchange(
                    ref eliminacionEnCurso,
                    1,
                    0) != 0)
            {
                return;
            }

            int paginaARecargar = paginaActual;
            bool eliminado = false;

            try
            {
                bool confirmar =
                    await Application.Current!
                        .MainPage!
                        .DisplayAlert(
                            "Eliminar parámetro de extracción",
                            $"¿Desea eliminar la extracción configurada para '{item.ElementoTexto}'?",
                            "Eliminar",
                            "Cancelar");

                if (!confirmar)
                    return;

                CancellationTokenSource source =
                    PrepararNuevaAccion();

                try
                {
                    IsBusy = true;
                    ActualizarComandos();

                    ApiResult<bool> resultado =
                        await apiService.DeleteAsync(
                            item.ParametroExtraccionNutrienteCafeId,
                            source.Token);

                    if (source.IsCancellationRequested ||
                        !EsAccionActual(source))
                    {
                        return;
                    }

                    if (!resultado.Success)
                    {
                        if (!EsMensajeCancelacion(resultado.Message))
                            await MostrarToastAsync(resultado.Message);

                        return;
                    }

                    /*
                     * El servidor vuelve a ser la fuente de verdad. No se
                     * elimina el objeto localmente antes de reconstruir la página.
                     */
                    ExtraccionNutrienteListadoEstadoService
                        .MarcarParaRecargar();

                    paginaARecargar =
                        Math.Max(1, paginaActual);

                    eliminado = true;

                    await MostrarToastAsync(
                        string.IsNullOrWhiteSpace(resultado.Message)
                            ? "Parámetro eliminado correctamente."
                            : resultado.Message);
                }
                finally
                {
                    IsBusy = false;
                    LiberarAccion(source);
                    ActualizarComandos();
                    NotificarEstadoLista();
                }
            }
            finally
            {
                Interlocked.Exchange(
                    ref eliminacionEnCurso,
                    0);

                ActualizarComandos();
            }

            if (eliminado &&
                CanView &&
                !Navegando)
            {
                await CargarPaginaAsync(
                    paginaARecargar,
                    desplazarAlInicio: true);
            }
        }

        private async Task NavegarAsync(
            string ruta,
            IDictionary<string, object>? parametros = null)
        {
            if (Navegando)
                return;

            Navegando = true;

            try
            {
                CancelarCarga();

                if (parametros == null)
                {
                    await GoToAsyncParameters(ruta);
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
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    descripcion,
                    ex);
            }
        }

        private void ActualizarComandos()
        {
            RegresarConfiguracionCommand.ChangeCanExecute();
            AddCommand.ChangeCanExecute();
            EditCommand.ChangeCanExecute();
            ViewCommand.ChangeCanExecute();
            DeleteCommand.ChangeCanExecute();
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            PaginaAnteriorCommand.ChangeCanExecute();
            PaginaSiguienteCommand.ChangeCanExecute();
        }

        private void NotificarEstadoLista()
        {
            OnPropertyChanged(nameof(PaginaActual));
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(PuedeIrAnterior));
            OnPropertyChanged(nameof(PuedeIrSiguiente));
            OnPropertyChanged(nameof(MostrarPaginacion));
            OnPropertyChanged(nameof(PaginaTexto));
            OnPropertyChanged(nameof(RangoPaginaTexto));
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(ResumenResultados));
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Platform == DevicePlatform.WinUI
                ? 40
                : 20;

        private CancellationTokenSource PrepararNuevaCarga()
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

        private CancellationTokenSource PrepararNuevaAccion()
        {
            var source =
                new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref accionCts,
                    source);

            CancelarSeguro(anterior);

            return source;
        }

        private bool EsCargaActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(ref cargaCts),
                source);

        private bool EsAccionActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(ref accionCts),
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

        private void LiberarAccion(
            CancellationTokenSource source)
        {
            Interlocked.CompareExchange(
                ref accionCts,
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
                // La solicitud ya había terminado.
            }
        }

        private static bool EsMensajeCancelacion(
            string? valor) =>
            !string.IsNullOrWhiteSpace(valor) &&
            valor.Contains(
                "cancel",
                StringComparison.OrdinalIgnoreCase);
    }
}
