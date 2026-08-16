using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class TipoCultivoViewModel : GlobalService
    {
        private readonly TipoCultivoApiService apiService;
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

        public TipoCultivoViewModel()
            : this(new TipoCultivoApiService())
        {
        }

        public TipoCultivoViewModel(
            TipoCultivoApiService apiService)
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
                        () => NavegarAsync(
                            AppRoutes.Configuracion),
                        "regresar a configuración"),
                    () =>
                        !IsBusy &&
                        !Navegando);

            AddCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        OnAddAsync,
                        "abrir el formulario de tipo de cultivo"),
                    () =>
                        CanAdd &&
                        !IsBusy &&
                        !Navegando);

            EditCommand =
                new Command<TipoCultivoResponse>(
                    async item =>
                        await EjecutarSeguroAsync(
                            () => OnEditAsync(item),
                            "editar el tipo de cultivo"),
                    item =>
                        item != null &&
                        CanEdit &&
                        !IsBusy &&
                        !Navegando);

            ViewCommand =
                new Command<TipoCultivoResponse>(
                    async item =>
                        await EjecutarSeguroAsync(
                            () => OnViewAsync(item),
                            "consultar el tipo de cultivo"),
                    item =>
                        item != null &&
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            DeleteCommand =
                new Command<TipoCultivoResponse>(
                    async item =>
                        await EjecutarSeguroAsync(
                            () => OnDeleteAsync(item),
                            "eliminar el tipo de cultivo"),
                    item =>
                        item != null &&
                        CanDelete &&
                        !IsBusy &&
                        !Navegando);

            BuscarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        AplicarBusquedaAsync,
                        "buscar tipos de cultivo"),
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
                        "actualizar los tipos de cultivo"),
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

        public ObservableCollection<TipoCultivoResponse>
            List { get; } =
                new();

        public Command RegresarConfiguracionCommand { get; }
        public Command AddCommand { get; }
        public Command<TipoCultivoResponse> EditCommand { get; }
        public Command<TipoCultivoResponse> ViewCommand { get; }
        public Command<TipoCultivoResponse> DeleteCommand { get; }
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
                OnPropertyChanged(
                    nameof(TieneMensaje));
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
                OnPropertyChanged(
                    nameof(ResumenResultados));
                OnPropertyChanged(
                    nameof(RangoPaginaTexto));
                OnPropertyChanged(
                    nameof(MostrarPaginacion));
            }
        }

        public string ResumenResultados =>
            TotalRegistros == 1
                ? "1 tipo de cultivo encontrado"
                : $"{TotalRegistros} tipos de cultivo encontrados";

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
                    return
                        "Sin registros en esta página";
                }

                int tamano =
                    Math.Max(
                        1,
                        tamanoPaginaActual);

                int inicio =
                    ((Math.Max(
                        1,
                        paginaActual) - 1) *
                     tamano) + 1;

                int fin =
                    Math.Min(
                        inicio +
                        List.Count - 1,
                        TotalRegistros);

                return
                    $"Mostrando {inicio}-{fin} de {TotalRegistros}";
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
            LoadPagePermissions(
                "tipoCultivoPage");

            OnPropertyChanged(
                nameof(MostrarAccesoDenegado));

            NotificarEstadoLista();
            ActualizarComandos();
        }

        /// <summary>
        /// Se ejecuta al entrar a Tipos de cultivo desde otra interfaz.
        /// Descarta filtros, página y datos de la visita anterior y consulta
        /// únicamente la primera página al servidor.
        /// </summary>
        public async Task IniciarNuevaVisitaAsync()
        {
            if (!CanView ||
                Navegando)
            {
                return;
            }

            CancelarCarga();

            TextoBusqueda =
                string.Empty;

            textoBusquedaAplicado =
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

            await CargarPaginaAsync(
                1,
                cargaInicial: true);
        }

        /// <summary>
        /// Durante la misma visita no repite GET si nada cambió. Crear, editar
        /// o reactivar incrementan la versión y obligan a renovar la página.
        /// </summary>
        public async Task InicializarAsync()
        {
            if (!CanView ||
                Navegando)
            {
                return;
            }

            int versionActual =
                TipoCultivoListadoEstadoService
                    .VersionActual;

            if (!pantallaCargada)
            {
                await CargarPaginaAsync(
                    1,
                    cargaInicial: true);
                return;
            }

            if (versionAplicada !=
                versionActual)
            {
                await CargarPaginaAsync(
                    Math.Max(
                        1,
                        paginaActual));
            }
        }

        public Task RecargarPaginaActualAsync()
        {
            if (!CanView ||
                Navegando)
            {
                return Task.CompletedTask;
            }

            return CargarPaginaAsync(
                Math.Max(
                    1,
                    paginaActual));
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
        }

        private async Task AplicarBusquedaAsync()
        {
            textoBusquedaAplicado =
                (TextoBusqueda ??
                 string.Empty)
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
                    Math.Max(
                        1,
                        paginaActual));
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

        /// <summary>
        /// Consulta una sola página y reemplaza la colección actual. Nunca
        /// acumula páginas anteriores, manteniendo acotado el uso de memoria.
        /// </summary>
        private async Task CargarPaginaAsync(
            int paginaSolicitada,
            bool cargaInicial = false)
        {
            if (!CanView ||
                Navegando ||
                IsBusy)
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

                ApiResult<TipoCultivoPaginaResponse>
                    resultado =
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

                TipoCultivoPaginaResponse pagina =
                    resultado.Data;

                int paginasServidor =
                    Math.Max(
                        1,
                        pagina.TotalPaginas);

                /*
                 * Si una eliminación realizada por otro cliente redujo el total
                 * de páginas, se corrige una sola vez hacia la última válida.
                 */
                if (paginaSolicitada >
                        paginasServidor &&
                    pagina.TotalRegistros > 0)
                {
                    resultado =
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

                    pagina =
                        resultado.Data;
                }

                AplicarPagina(pagina);

                pantallaCargada =
                    true;

                versionAplicada =
                    TipoCultivoListadoEstadoService
                        .VersionActual;
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al navegar o reemplazar la búsqueda.
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
                        "No fue posible cargar los tipos de cultivo.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los tipos de cultivo",
                        ex);
                }
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    IsBusy = false;

                    if (cargaInicial)
                        IsRefreshing = false;
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        private void AplicarPagina(
            TipoCultivoPaginaResponse pagina)
        {
            List.Clear();

            foreach (TipoCultivoResponse item
                     in pagina.Items)
            {
                if (item.TipoCultivoId > 0)
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
                AppRoutes.TipoCultivoFormulario,
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.Create
                    },
                    {
                        "Item",
                        new TipoCultivoRequest()
                    }
                });

        private async Task OnEditAsync(
            TipoCultivoResponse? item)
        {
            TipoCultivoResponse? actual =
                await ObtenerDetalleActualAsync(
                    item);

            if (actual == null)
                return;

            await NavegarAsync(
                AppRoutes.TipoCultivoFormulario,
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.Edit
                    },
                    {
                        "Item",
                        new TipoCultivoRequest(actual)
                    }
                });
        }

        private async Task OnViewAsync(
            TipoCultivoResponse? item)
        {
            TipoCultivoResponse? actual =
                await ObtenerDetalleActualAsync(
                    item);

            if (actual == null)
                return;

            await NavegarAsync(
                AppRoutes.TipoCultivoFormulario,
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.View
                    },
                    {
                        "Item",
                        new TipoCultivoRequest(actual)
                    }
                });
        }

        /// <summary>
        /// Ver y Editar consultan el registro actual del servidor antes de abrir
        /// el formulario. Así no se navega con una copia potencialmente antigua
        /// que haya quedado en la página del listado.
        /// </summary>
        private async Task<TipoCultivoResponse?>
            ObtenerDetalleActualAsync(
                TipoCultivoResponse? item)
        {
            if (item == null ||
                item.TipoCultivoId <= 0)
            {
                return null;
            }

            CancellationTokenSource source =
                PrepararNuevaCarga();

            try
            {
                IsBusy = true;
                ActualizarComandos();

                ApiResult<TipoCultivoResponse>
                    resultado =
                        await apiService
                            .GetByIdAsync(
                                item.TipoCultivoId,
                                source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source))
                {
                    return null;
                }

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    if (!EsMensajeCancelacion(
                            resultado.Message))
                    {
                        await MostrarToastAsync(
                            string.IsNullOrWhiteSpace(
                                resultado.Message)
                                ? "No fue posible obtener el tipo de cultivo."
                                : resultado.Message);
                    }

                    return null;
                }

                return resultado.Data;
            }
            finally
            {
                if (EsCargaActual(source))
                    IsBusy = false;

                LiberarCarga(source);
                ActualizarComandos();
            }
        }

        private async Task OnDeleteAsync(
            TipoCultivoResponse? item)
        {
            if (item == null ||
                item.TipoCultivoId <= 0 ||
                IsBusy)
            {
                return;
            }

            bool confirmar =
                await Application.Current!
                    .MainPage!
                    .DisplayAlert(
                        "Eliminar tipo de cultivo",
                        $"¿Desea eliminar el tipo de cultivo '{item.NombreMostrar}'?",
                        "Eliminar",
                        "Cancelar");

            if (!confirmar)
                return;

            bool eliminado = false;
            int paginaDestino =
                Math.Max(
                    1,
                    paginaActual);

            try
            {
                IsBusy = true;
                ActualizarComandos();

                ApiResult<bool> resultado =
                    await apiService
                        .DeleteAsync(
                            item.TipoCultivoId);

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

                TipoCultivoListadoEstadoService
                    .MarcarCambio();

                eliminado = true;

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(
                        resultado.Message)
                            ? "Tipo de cultivo eliminado correctamente."
                            : resultado.Message);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
                NotificarEstadoLista();
            }

            /*
             * Se renueva la página desde el servidor. Quitar solo el elemento
             * local dejaría un hueco y podría omitir el registro que se desplazó
             * desde la siguiente página después de la eliminación.
             */
            if (eliminado)
            {
                await CargarPaginaAsync(
                    paginaDestino);
            }
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
            DeviceInfo.Current.Platform ==
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
