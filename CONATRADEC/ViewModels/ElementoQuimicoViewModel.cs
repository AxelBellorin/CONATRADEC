using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class ElementoQuimicoViewModel : GlobalService
    {
        private readonly ElementoQuimicoApiService elementoApiService;
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

        public ElementoQuimicoViewModel()
            : this(new ElementoQuimicoApiService())
        {
        }

        public ElementoQuimicoViewModel(
            ElementoQuimicoApiService elementoApiService)
        {
            this.elementoApiService = elementoApiService
                ?? throw new ArgumentNullException(
                    nameof(elementoApiService));

            tamanoPaginaActual =
                ObtenerTamanoPagina();

            RegresarConfiguracionCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    () => NavegarAsync(AppRoutes.Configuracion),
                    "regresar a configuración"),
                () => !IsBusy && !Navegando);

            AddCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    OnAddAsync,
                    "abrir el formulario de elemento químico"),
                () => CanAdd && !IsBusy && !Navegando);

            EditCommand = new Command<ElementoQuimicoResponse>(
                async elemento => await EjecutarSeguroAsync(
                    () => OnEditAsync(elemento),
                    "editar el elemento químico"),
                elemento =>
                    elemento != null &&
                    CanEdit &&
                    !IsBusy &&
                    !Navegando);

            DeleteCommand = new Command<ElementoQuimicoResponse>(
                async elemento => await EjecutarSeguroAsync(
                    () => OnDeleteAsync(elemento),
                    "eliminar el elemento químico"),
                elemento =>
                    elemento != null &&
                    CanDelete &&
                    !IsBusy &&
                    !Navegando &&
                    Volatile.Read(ref eliminacionEnCurso) == 0);

            ViewCommand = new Command<ElementoQuimicoResponse>(
                async elemento => await EjecutarSeguroAsync(
                    () => OnViewAsync(elemento),
                    "consultar el elemento químico"),
                elemento =>
                    elemento != null &&
                    CanView &&
                    !IsBusy &&
                    !Navegando);

            BuscarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    AplicarBusquedaAsync,
                    "buscar elementos químicos"),
                () => CanView && !IsBusy && !Navegando);

            LimpiarFiltrosCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    LimpiarFiltrosAsync,
                    "limpiar la búsqueda"),
                () => CanView && !IsBusy && !Navegando);

            RefrescarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RefrescarAsync,
                    "actualizar los elementos químicos"),
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

        public ObservableCollection<ElementoQuimicoResponse> List { get; } =
            new();

        public Command RegresarConfiguracionCommand { get; }
        public Command AddCommand { get; }
        public Command<ElementoQuimicoResponse> EditCommand { get; }
        public Command<ElementoQuimicoResponse> DeleteCommand { get; }
        public Command<ElementoQuimicoResponse> ViewCommand { get; }
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
                ? "1 elemento encontrado"
                : $"{TotalRegistros} elementos encontrados";

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

        public bool TienePaginaCargada =>
            pantallaCargada;

        public void ActualizarPermisos()
        {
            LoadPagePermissions("elementoQuimicoPage");

            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            NotificarEstadoLista();
            ActualizarComandos();
        }

        /// <summary>
        /// Una entrada nueva desde otra interfaz siempre inicia limpia:
        /// página 1, sin filtro aplicado y con datos frescos del servidor.
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
        /// Al volver desde el formulario mantiene la misma visita. Cuando hubo
        /// una mutación confirmada, la página visible se vuelve a consultar al
        /// servidor para aceptar el estado persistido y cualquier dato derivado.
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
                ElementoQuimicoListadoEstadoService.VersionActual;

            if (versionAplicada != versionActual)
            {
                /*
                 * Si la mutación provenía de una edición, se descarta la copia
                 * temporal porque la página será reconstruida desde servidor.
                 */
                ElementoQuimicoListadoEstadoService
                    .IntentarConsumirEdicion(
                        out _);

                await CargarPaginaAsync(
                    Math.Max(1, paginaActual));
            }
        }

        public Task RecargarPaginaActualAsync() =>
            CargarPaginaAsync(
                Math.Max(1, paginaActual));

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
        /// Consulta una sola página y reemplaza la colección actual. Nunca
        /// acumula páginas anteriores, manteniendo acotado el uso de memoria.
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

                ApiResult<ElementoQuimicoPaginaResponse> resultado =
                    await elementoApiService.BuscarElementosAsync(
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

                ElementoQuimicoPaginaResponse pagina =
                    resultado.Data;

                int paginasServidor =
                    Math.Max(1, pagina.TotalPaginas);

                /*
                 * Si otro cliente redujo el total de páginas mientras esta
                 * visita estaba abierta, se corrige una sola vez hacia la
                 * última página válida.
                 */
                if (paginaSolicitada > paginasServidor &&
                    pagina.TotalRegistros > 0)
                {
                    resultado =
                        await elementoApiService.BuscarElementosAsync(
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
                    ElementoQuimicoListadoEstadoService.VersionActual;

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
                        "No fue posible cargar los elementos químicos.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los elementos químicos",
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
            ElementoQuimicoPaginaResponse pagina)
        {
            List.Clear();

            foreach (ElementoQuimicoResponse elemento
                     in pagina.Items)
            {
                if (elemento.ElementoQuimicosId is > 0)
                    List.Add(elemento);
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

        private Task OnAddAsync() =>
            NavegarAsync(
                "//ElementoQuimicoFormPage",
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.Create
                    },
                    {
                        "ElementoQuimico",
                        new ElementoQuimicoRequest()
                    }
                });

        private Task OnEditAsync(
            ElementoQuimicoResponse? elemento) =>
            AbrirDetalleActualAsync(
                elemento,
                FormMode.FormModeSelect.Edit);

        private Task OnViewAsync(
            ElementoQuimicoResponse? elemento) =>
            AbrirDetalleActualAsync(
                elemento,
                FormMode.FormModeSelect.View);

        /// <summary>
        /// Ver y Editar siempre consultan el registro por ID antes de navegar.
        /// Así el formulario recibe el estado actual persistido y no una copia
        /// potencialmente antigua de la tarjeta de la página.
        /// </summary>
        private async Task AbrirDetalleActualAsync(
            ElementoQuimicoResponse? elemento,
            FormMode.FormModeSelect modo)
        {
            if (elemento?.ElementoQuimicosId is not > 0 ||
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

                ApiResult<ElementoQuimicoResponse> resultado =
                    await elementoApiService
                        .GetElementoQuimicoAdminByIdResultAsync(
                            elemento.ElementoQuimicosId.Value,
                            source.Token);

                if (source.IsCancellationRequested ||
                    !EsAccionActual(source))
                {
                    return;
                }

                if (!resultado.Success ||
                    resultado.Data?.ElementoQuimicosId is not > 0)
                {
                    if (!EsMensajeCancelacion(resultado.Message))
                    {
                        await MostrarToastAsync(
                            string.IsNullOrWhiteSpace(resultado.Message)
                                ? "No fue posible cargar el elemento químico."
                                : resultado.Message);
                    }

                    return;
                }

                ElementoQuimicoRequest actual =
                    new(resultado.Data);

                /*
                 * La consulta ya terminó. Se libera el estado Busy antes de
                 * navegar para no cancelar el detalle recién obtenido.
                 */
                IsBusy = false;
                LiberarAccion(source);
                accionLiberada = true;

                await NavegarAsync(
                    "//ElementoQuimicoFormPage",
                    new Dictionary<string, object>
                    {
                        {
                            "Mode",
                            modo
                        },
                        {
                            "ElementoQuimico",
                            actual
                        }
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

        private async Task OnDeleteAsync(
            ElementoQuimicoResponse? elemento)
        {
            if (elemento?.ElementoQuimicosId is not > 0 ||
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
                string identificacion =
                    $"{elemento.NombreElementoQuimico} " +
                    $"({elemento.SimboloElementoQuimico})";

                bool confirmar =
                    await Application.Current!
                        .MainPage!
                        .DisplayAlert(
                            "Eliminar elemento químico",
                            "¿Desea eliminar este elemento químico?\n\n" +
                            identificacion,
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
                        await elementoApiService
                            .DeleteElementoQuimicoResultAsync(
                                new ElementoQuimicoRequest(elemento),
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
                     * No se elimina el objeto localmente. El servidor vuelve a
                     * ser la fuente de verdad y CargarPaginaAsync determina si
                     * la página actual sigue existiendo.
                     */
                    ElementoQuimicoListadoEstadoService
                        .MarcarParaRecargar();

                    paginaARecargar =
                        Math.Max(1, paginaActual);

                    eliminado = true;

                    await MostrarToastAsync(
                        string.IsNullOrWhiteSpace(resultado.Message)
                            ? "Elemento químico eliminado correctamente."
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
            DeleteCommand.ChangeCanExecute();
            ViewCommand.ChangeCanExecute();
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
