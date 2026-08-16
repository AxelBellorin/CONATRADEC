using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class TipoAnalisisSueloViewModel : GlobalService
    {
        private readonly TipoAnalisisSueloApiService apiService;
        private CancellationTokenSource? cargaCts;

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

        public TipoAnalisisSueloViewModel()
            : this(new TipoAnalisisSueloApiService())
        {
        }

        public TipoAnalisisSueloViewModel(
            TipoAnalisisSueloApiService apiService)
        {
            this.apiService =
                apiService
                ?? throw new ArgumentNullException(
                    nameof(apiService));

            tamanoPaginaActual =
                ObtenerTamanoPagina();

            RegresarConfiguracionCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        SalirAConfiguracionAsync,
                        "regresar a configuración"),
                    () =>
                        !IsBusy &&
                        !Navegando);

            AddCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        OnAddAsync,
                        "abrir el formulario de tipo de análisis"),
                    () =>
                        CanAdd &&
                        !IsBusy &&
                        !Navegando);

            EditCommand =
                new Command<TipoAnalisisSueloResponse>(
                    async item =>
                        await EjecutarSeguroAsync(
                            () => OnEditAsync(item),
                            "editar el tipo de análisis"),
                    item =>
                        item != null &&
                        CanEdit &&
                        !IsBusy &&
                        !Navegando);

            ViewCommand =
                new Command<TipoAnalisisSueloResponse>(
                    async item =>
                        await EjecutarSeguroAsync(
                            () => OnViewAsync(item),
                            "consultar el tipo de análisis"),
                    item =>
                        item != null &&
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            DeleteCommand =
                new Command<TipoAnalisisSueloResponse>(
                    async item =>
                        await EjecutarSeguroAsync(
                            () => OnDeleteAsync(item),
                            "eliminar el tipo de análisis"),
                    item =>
                        item != null &&
                        CanDelete &&
                        !IsBusy &&
                        !Navegando);

            BuscarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        AplicarBusquedaAsync,
                        "buscar tipos de análisis"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            LimpiarFiltrosCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        LimpiarFiltrosAsync,
                        "limpiar la búsqueda"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            RefrescarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        RefrescarAsync,
                        "actualizar los tipos de análisis"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            PaginaAnteriorCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        IrPaginaAnteriorAsync,
                        "cargar la página anterior"),
                    () =>
                        CanView &&
                        PuedeIrAnterior &&
                        !IsBusy &&
                        !Navegando);

            PaginaSiguienteCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        IrPaginaSiguienteAsync,
                        "cargar la página siguiente"),
                    () =>
                        CanView &&
                        PuedeIrSiguiente &&
                        !IsBusy &&
                        !Navegando);
        }

        public ObservableCollection<TipoAnalisisSueloResponse>
            List { get; } =
                new();

        public Command RegresarConfiguracionCommand { get; }
        public Command AddCommand { get; }
        public Command<TipoAnalisisSueloResponse> EditCommand { get; }
        public Command<TipoAnalisisSueloResponse> ViewCommand { get; }
        public Command<TipoAnalisisSueloResponse> DeleteCommand { get; }
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
                string nuevoValor =
                    value ??
                    string.Empty;

                if (textoBusqueda == nuevoValor)
                    return;

                textoBusqueda =
                    nuevoValor;

                OnPropertyChanged();
            }
        }

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                string nuevoValor =
                    value ??
                    string.Empty;

                if (mensaje == nuevoValor)
                    return;

                mensaje =
                    nuevoValor;

                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(
                Mensaje);

        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                if (isRefreshing == value)
                    return;

                isRefreshing =
                    value;

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

                navegando =
                    value;

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

                totalRegistros =
                    value;

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

                int inicio =
                    ((Math.Max(1, paginaActual) - 1) *
                     Math.Max(1, tamanoPaginaActual)) + 1;

                int fin =
                    Math.Min(
                        inicio + List.Count - 1,
                        TotalRegistros);

                return
                    $"Mostrando {inicio}-{fin} de {TotalRegistros}";
            }
        }

        public string ResumenResultados =>
            TotalRegistros == 1
                ? "1 tipo de análisis encontrado"
                : $"{TotalRegistros} tipos de análisis encontrados";

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
            LoadPagePermissions(
                "tipoAnalisisSueloPage");

            OnPropertyChanged(
                nameof(MostrarAccesoDenegado));

            ActualizarComandos();
            NotificarEstadoLista();
        }

        /// <summary>
        /// Inicia una visita real al módulo: descarta búsqueda, página y datos
        /// anteriores y consulta la primera página directamente al servidor.
        /// </summary>
        public async Task IniciarNuevaVisitaAsync()
        {
            CancelarCarga();
            RestablecerEstadoLocal();

            await CargarPaginaAsync(
                1,
                cargaInicial: true);
        }

        /// <summary>
        /// Al regresar desde un subflujo interno conserva la página y filtros.
        /// Solo consulta nuevamente cuando hubo un cambio confirmado.
        /// </summary>
        public async Task InicializarAsync()
        {
            if (!CanView ||
                Navegando)
            {
                return;
            }

            if (!pantallaCargada)
            {
                await CargarPaginaAsync(
                    1,
                    cargaInicial: true);

                return;
            }

            if (versionAplicada !=
                TipoAnalisisSueloListadoEstadoService.VersionActual)
            {
                await RecargarPaginaActualAsync();
            }
        }

        public Task RecargarPaginaActualAsync() =>
            CargarPaginaAsync(
                Math.Max(1, paginaActual));

        /// <summary>
        /// Finaliza una salida real del módulo y libera tanto la visita como el
        /// estado visible para que el próximo ingreso comience desde cero.
        /// </summary>
        public void FinalizarVisita()
        {
            TipoAnalisisSueloListadoEstadoService
                .FinalizarVisita();

            CancelarCarga();
            RestablecerEstadoLocal();
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
            ActualizarComandos();
            NotificarEstadoLista();
        }

        private void RestablecerEstadoLocal()
        {
            textoBusquedaAplicado =
                string.Empty;

            TextoBusqueda =
                string.Empty;

            Mensaje =
                string.Empty;

            paginaActual = 1;
            totalPaginas = 1;
            TotalRegistros = 0;
            tamanoPaginaActual =
                ObtenerTamanoPagina();

            pantallaCargada = false;
            versionAplicada = -1;

            List.Clear();
            NotificarEstadoLista();
        }

        private async Task AplicarBusquedaAsync()
        {
            textoBusquedaAplicado =
                (TextoBusqueda ?? string.Empty)
                    .Trim();

            await CargarPaginaAsync(1);
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda =
                string.Empty;

            textoBusquedaAplicado =
                string.Empty;

            await CargarPaginaAsync(1);
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing =
                true;

            try
            {
                await CargarPaginaAsync(
                    Math.Max(1, paginaActual));
            }
            finally
            {
                IsRefreshing =
                    false;
            }
        }

        private Task IrPaginaAnteriorAsync()
        {
            if (!PuedeIrAnterior)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                paginaActual - 1);
        }

        private Task IrPaginaSiguienteAsync()
        {
            if (!PuedeIrSiguiente)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                paginaActual + 1);
        }

        private async Task CargarPaginaAsync(
            int paginaSolicitada,
            bool cargaInicial = false)
        {
            if (!CanView ||
                Navegando)
            {
                return;
            }

            paginaSolicitada =
                Math.Max(
                    1,
                    paginaSolicitada);

            CancellationTokenSource source =
                PrepararNuevaCarga();

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;
                ActualizarComandos();

                ApiResult<TipoAnalisisSueloPaginaResponse> resultado =
                    await apiService
                        .BuscarAsync(
                            textoBusquedaAplicado,
                            paginaSolicitada,
                            ObtenerTamanoPagina(),
                            source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source))
                {
                    return;
                }

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    if (!EsMensajeCancelacion(
                            resultado.Message))
                    {
                        Mensaje =
                            resultado.Message;
                    }

                    return;
                }

                TipoAnalisisSueloPaginaResponse pagina =
                    resultado.Data;

                int paginasServidor =
                    Math.Max(
                        1,
                        pagina.TotalPaginas);

                /*
                 * Si otro cliente eliminó registros y la página dejó de existir,
                 * se corrige únicamente hacia la última página válida.
                 */
                if (paginaSolicitada > paginasServidor)
                {
                    ApiResult<TipoAnalisisSueloPaginaResponse> correccion =
                        await apiService
                            .BuscarAsync(
                                textoBusquedaAplicado,
                                paginasServidor,
                                ObtenerTamanoPagina(),
                                source.Token);

                    if (source.IsCancellationRequested ||
                        !EsCargaActual(source))
                    {
                        return;
                    }

                    if (!correccion.Success ||
                        correccion.Data == null)
                    {
                        if (!EsMensajeCancelacion(
                                correccion.Message))
                        {
                            Mensaje =
                                correccion.Message;
                        }

                        return;
                    }

                    pagina =
                        correccion.Data;
                }

                AplicarPagina(pagina);

                pantallaCargada =
                    true;

                versionAplicada =
                    TipoAnalisisSueloListadoEstadoService
                        .VersionActual;
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
                        "No fue posible cargar los tipos de análisis de suelo.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los tipos de análisis de suelo",
                        ex);
                }
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    IsBusy = false;

                    if (cargaInicial)
                    {
                        IsRefreshing = false;
                    }
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        /// <summary>
        /// La colección contiene únicamente la página recibida. Nunca acumula
        /// páginas anteriores, manteniendo acotado el uso de memoria.
        /// </summary>
        private void AplicarPagina(
            TipoAnalisisSueloPaginaResponse pagina)
        {
            List.Clear();

            foreach (TipoAnalisisSueloResponse item
                     in pagina.Items)
            {
                if (item.TipoAnalisisSueloId > 0)
                    List.Add(item);
            }

            paginaActual =
                Math.Max(
                    1,
                    pagina.PaginaActual);

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

            Mensaje =
                string.Empty;

            NotificarEstadoLista();
        }

        private Task OnAddAsync() =>
            NavegarAsync(
                AppRoutes.TipoAnalisisSueloFormulario,
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.Create
                    },
                    {
                        "Item",
                        new TipoAnalisisSueloRequest()
                    }
                });

        private Task OnEditAsync(
            TipoAnalisisSueloResponse? item) =>
            AbrirDetalleAsync(
                item,
                FormMode.FormModeSelect.Edit);

        private Task OnViewAsync(
            TipoAnalisisSueloResponse? item) =>
            AbrirDetalleAsync(
                item,
                FormMode.FormModeSelect.View);

        private async Task AbrirDetalleAsync(
            TipoAnalisisSueloResponse? item,
            FormMode.FormModeSelect mode)
        {
            if (item == null ||
                item.TipoAnalisisSueloId <= 0 ||
                IsBusy)
            {
                return;
            }

            CancellationTokenSource source =
                PrepararNuevaCarga();

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;
                ActualizarComandos();

                ApiResult<TipoAnalisisSueloResponse> resultado =
                    await apiService
                        .GetByIdAsync(
                            item.TipoAnalisisSueloId,
                            source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source))
                {
                    return;
                }

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    if (!EsMensajeCancelacion(
                            resultado.Message))
                    {
                        Mensaje =
                            resultado.Message;

                        await MostrarToastAsync(
                            resultado.Message);
                    }

                    return;
                }

                TipoAnalisisSueloRequest request =
                    new(resultado.Data);

                await NavegarAsync(
                    AppRoutes.TipoAnalisisSueloFormulario,
                    new Dictionary<string, object>
                    {
                        {
                            "Mode",
                            mode
                        },
                        {
                            "Item",
                            request
                        }
                    });
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al abandonar la pantalla.
            }
            catch (ObjectDisposedException)
            {
                // La solicitud terminó mientras se navegaba.
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    IsBusy = false;
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        private async Task OnDeleteAsync(
            TipoAnalisisSueloResponse? item)
        {
            if (item == null ||
                IsBusy)
            {
                return;
            }

            if (!item.PuedeEliminar ||
                item.EsTipoSistema)
            {
                await Application.Current!
                    .MainPage!
                    .DisplayAlert(
                        "Tipo protegido",
                        "Este tipo pertenece a un módulo interno del sistema y no puede eliminarse.",
                        "Aceptar");

                return;
            }

            bool confirmar =
                await Application.Current!
                    .MainPage!
                    .DisplayAlert(
                        "Eliminar tipo de análisis",
                        $"¿Desea eliminar el tipo de análisis '{item.NombreMostrar}'?",
                        "Eliminar",
                        "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;
                ActualizarComandos();

                ApiResult<bool> resultado =
                    await apiService
                        .DeleteAsync(
                            item.TipoAnalisisSueloId);

                if (!resultado.Success)
                {
                    if (resultado.StatusCode == 409)
                    {
                        await Application.Current!
                            .MainPage!
                            .DisplayAlert(
                                "No se puede eliminar",
                                resultado.Message,
                                "Aceptar");
                    }
                    else
                    {
                        await MostrarToastAsync(
                            resultado.Message);
                    }

                    return;
                }

                int totalEstimado =
                    Math.Max(
                        0,
                        TotalRegistros - 1);

                int tamano =
                    Math.Max(
                        1,
                        tamanoPaginaActual);

                int paginasEstimadas =
                    totalEstimado == 0
                        ? 1
                        : (int)Math.Ceiling(
                            totalEstimado /
                            (double)tamano);

                int paginaDestino =
                    Math.Min(
                        Math.Max(1, paginaActual),
                        Math.Max(1, paginasEstimadas));

                TipoAnalisisSueloListadoEstadoService
                    .MarcarCambio();

                /*
                 * La eliminación siempre se confirma contra el servidor y luego
                 * se vuelve a consultar la página válida. No se asume que quitar
                 * la tarjeta local represente el estado final de la base de datos.
                 */
                await CargarPaginaAsync(
                    paginaDestino);

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(
                        resultado.Message)
                            ? "Tipo de análisis eliminado correctamente."
                            : resultado.Message);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        private async Task SalirAConfiguracionAsync()
        {
            FinalizarVisita();

            await NavegarAsync(
                AppRoutes.Configuracion);
        }

        private async Task NavegarAsync(
            string ruta,
            IDictionary<string, object>? parametros = null)
        {
            if (Navegando)
                return;

            Navegando =
                true;

            try
            {
                CancelarCarga();

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
                Navegando =
                    false;
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
            OnPropertyChanged(
                nameof(MostrarVacio));

            OnPropertyChanged(
                nameof(MostrarPaginacion));

            OnPropertyChanged(
                nameof(PuedeIrAnterior));

            OnPropertyChanged(
                nameof(PuedeIrSiguiente));

            OnPropertyChanged(
                nameof(PaginaActual));

            OnPropertyChanged(
                nameof(TotalPaginas));

            OnPropertyChanged(
                nameof(PaginaTexto));

            OnPropertyChanged(
                nameof(RangoPaginaTexto));

            OnPropertyChanged(
                nameof(ResumenResultados));
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Platform ==
            DevicePlatform.WinUI
                ? 40
                : 20;

        private CancellationTokenSource
            PrepararNuevaCarga()
        {
            var source =
                new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref cargaCts,
                    source);

            CancelarSeguro(
                anterior);

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
