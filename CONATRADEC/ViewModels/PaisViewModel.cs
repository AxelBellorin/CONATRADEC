using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class PaisViewModel : GlobalService
    {
        private readonly PaisApiService paisApiService;
        private CancellationTokenSource? cargaCts;
        private int eliminacionEnCurso;

        private string textoBusqueda = string.Empty;
        private string textoBusquedaAplicado = string.Empty;
        private string mensaje = string.Empty;
        private bool isRefreshing;
        private bool navegando;
        private bool pantallaCargada;
        private bool mostrandoRelay;
        private string tituloRelay = "Procesando...";
        private string detalleRelay = "Espere un momento.";
        private int paginaActual = 1;
        private int totalPaginas = 1;
        private int totalRegistros;
        private int tamanoPaginaActual;

        public PaisViewModel()
            : this(new PaisApiService())
        {
        }

        public PaisViewModel(PaisApiService paisApiService)
        {
            this.paisApiService = paisApiService
                ?? throw new ArgumentNullException(nameof(paisApiService));

            tamanoPaginaActual = ObtenerTamanoPagina();

            RegresarConfiguracionCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RegresarConfiguracionAsync,
                    "regresar a configuración"),
                () => !IsBusy && !Navegando);

            AddCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    OnAddAsync,
                    "abrir el formulario de país"),
                () => CanAdd && !IsBusy && !Navegando);

            EditCommand = new Command<PaisResponse>(
                async pais => await EjecutarSeguroAsync(
                    () => OnEditAsync(pais),
                    "editar el país"),
                pais =>
                    pais != null &&
                    CanEdit &&
                    !IsBusy &&
                    !Navegando);

            DeleteCommand = new Command<PaisResponse>(
                async pais => await EjecutarSeguroAsync(
                    () => OnDeleteAsync(pais),
                    "eliminar el país"),
                pais =>
                    pais != null &&
                    CanDelete &&
                    !IsBusy &&
                    !Navegando);

            ViewCommand = new Command<PaisResponse>(
                async pais => await EjecutarSeguroAsync(
                    () => OnViewAsync(pais),
                    "consultar los departamentos"),
                pais =>
                    pais != null &&
                    CanView &&
                    !IsBusy &&
                    !Navegando);

            BuscarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    AplicarBusquedaAsync,
                    "buscar países"),
                () => CanView && !IsBusy && !Navegando);

            LimpiarFiltrosCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    LimpiarFiltrosAsync,
                    "limpiar la búsqueda"),
                () => CanView && !IsBusy && !Navegando);

            RefrescarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RefrescarAsync,
                    "actualizar los países"),
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

        public ObservableCollection<PaisResponse> List { get; } = new();

        public Command RegresarConfiguracionCommand { get; }
        public Command AddCommand { get; }
        public Command<PaisResponse> EditCommand { get; }
        public Command<PaisResponse> DeleteCommand { get; }
        public Command<PaisResponse> ViewCommand { get; }
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
                string nuevo = value ?? string.Empty;
                if (textoBusqueda == nuevo)
                    return;

                textoBusqueda = nuevo;
                OnPropertyChanged();
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
                string nuevo = value ?? string.Empty;
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
                string nuevo = value ?? string.Empty;
                if (detalleRelay == nuevo)
                    return;

                detalleRelay = nuevo;
                OnPropertyChanged();
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

        public int PaginaActual => paginaActual;
        public int TotalPaginas => totalPaginas;

        public bool PuedeIrAnterior =>
            pantallaCargada && paginaActual > 1;

        public bool PuedeIrSiguiente =>
            pantallaCargada && paginaActual < totalPaginas;

        public bool MostrarPaginacion =>
            CanView && pantallaCargada && List.Count > 0;

        public string PaginaTexto =>
            $"Página {Math.Max(1, paginaActual)} de {Math.Max(1, totalPaginas)}";

        public string RangoPaginaTexto
        {
            get
            {
                if (TotalRegistros <= 0 || List.Count == 0)
                    return "Sin registros en esta página";

                int inicio =
                    ((Math.Max(1, paginaActual) - 1) *
                     Math.Max(1, tamanoPaginaActual)) + 1;

                int fin = Math.Min(
                    inicio + List.Count - 1,
                    TotalRegistros);

                return $"Mostrando {inicio}-{fin} de {TotalRegistros}";
            }
        }

        public string ResumenResultados =>
            TotalRegistros == 1
                ? "1 país encontrado"
                : $"{TotalRegistros:N0} países encontrados";

        public bool MostrarVacio =>
            CanView &&
            pantallaCargada &&
            !IsBusy &&
            List.Count == 0 &&
            !TieneMensaje;

        public bool MostrarAccesoDenegado => !CanView;
        public bool TienePaginaCargada => pantallaCargada;

        public void ActualizarPermisos()
        {
            LoadPagePermissions("paisPage");

            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanAdd));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanDelete));
            OnPropertyChanged(nameof(MostrarAccesoDenegado));

            ActualizarComandos();
            NotificarEstado();
        }

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
            List.Clear();
            NotificarEstado();

            await CargarPaginaAsync(
                1,
                "Cargando países...",
                "Consultando información actual del servidor");
        }

        public Task InicializarAsync() =>
            pantallaCargada
                ? Task.CompletedTask
                : CargarPaginaAsync(
                    1,
                    "Cargando países...",
                    "Consultando información actual del servidor");

        public Task RecargarPaginaActualAsync() =>
            CargarPaginaAsync(
                Math.Max(1, paginaActual),
                "Actualizando países...",
                "Aplicando los cambios realizados dentro del módulo");

        /// <summary>
        /// Aplica cambios que pueden resolverse con el DTO ya disponible.
        /// Devuelve true únicamente cuando la composición global requiere GET.
        /// </summary>
        public bool AplicarCambiosPendientes()
        {
            bool requiereGet = false;

            if (UbicacionVisitaService.ConsumirPaisActualizado(
                    out PaisActualizadoPendiente mutacion))
            {
                int indice = BuscarPais(mutacion.PaisId);

                if (indice >= 0)
                {
                    PaisResponse actual = List[indice];

                    bool cambioOrden = !string.Equals(
                        actual.NombrePais,
                        mutacion.NombrePais,
                        StringComparison.OrdinalIgnoreCase);

                    if (!string.IsNullOrWhiteSpace(textoBusquedaAplicado) ||
                        (cambioOrden && totalPaginas > 1))
                    {
                        requiereGet = true;
                    }
                    else
                    {
                        List[indice] = new PaisResponse
                        {
                            PaisId = actual.PaisId,
                            NombrePais = mutacion.NombrePais,
                            CodigoISOPais = mutacion.CodigoISOPais,
                            Activo = actual.Activo,
                            CantidadDepartamentos =
                                actual.CantidadDepartamentos
                        };

                        if (cambioOrden)
                            OrdenarPaginaActual();
                    }
                }
                else
                {
                    requiereGet = true;
                }
            }

            for (int i = 0; i < List.Count; i++)
            {
                PaisResponse actual = List[i];

                if (!UbicacionVisitaService
                    .ConsumirDeltaDepartamentosPais(
                        actual.PaisId,
                        out int delta))
                {
                    continue;
                }

                List[i] = new PaisResponse
                {
                    PaisId = actual.PaisId,
                    NombrePais = actual.NombrePais,
                    CodigoISOPais = actual.CodigoISOPais,
                    Activo = actual.Activo,
                    CantidadDepartamentos = Math.Max(
                        0,
                        actual.CantidadDepartamentos + delta)
                };
            }

            return requiereGet;
        }

        public void CancelarCarga()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(ref cargaCts, null);

            CancelarSeguro(source);

            IsBusy = false;
            IsRefreshing = false;
            OcultarRelay();
            ActualizarComandos();
            NotificarEstado();
        }

        private async Task AplicarBusquedaAsync()
        {
            textoBusquedaAplicado =
                (TextoBusqueda ?? string.Empty).Trim();

            await CargarPaginaAsync(
                1,
                "Buscando países...",
                "Consultando los registros que coinciden con la búsqueda");
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;

            await CargarPaginaAsync(
                1,
                "Actualizando países...",
                "Quitando filtros y consultando la primera página");
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;

            try
            {
                await CargarPaginaAsync(
                    Math.Max(1, paginaActual),
                    "Actualizando países...",
                    "Consultando nuevamente la página actual");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task IrPaginaAnteriorAsync() =>
            PuedeIrAnterior
                ? CargarPaginaAsync(
                    paginaActual - 1,
                    "Cargando página anterior...",
                    "Consultando la página anterior de países")
                : Task.CompletedTask;

        private Task IrPaginaSiguienteAsync() =>
            PuedeIrSiguiente
                ? CargarPaginaAsync(
                    paginaActual + 1,
                    "Cargando página siguiente...",
                    "Consultando la siguiente página de países")
                : Task.CompletedTask;

        private async Task CargarPaginaAsync(
            int paginaSolicitada,
            string tituloOperacion,
            string detalleOperacion)
        {
            if (!CanView || Navegando)
                return;

            paginaSolicitada = Math.Max(1, paginaSolicitada);
            CancellationTokenSource source = PrepararNuevaCarga();

            try
            {
                MostrarRelay(tituloOperacion, detalleOperacion);
                IsBusy = true;
                Mensaje = string.Empty;
                ActualizarComandos();
                NotificarEstado();

                ApiResult<PaisPaginaResponse> resultado =
                    await paisApiService.BuscarPaisesAsync(
                        textoBusquedaAplicado,
                        paginaSolicitada,
                        ObtenerTamanoPagina(),
                        source.Token);

                if (source.IsCancellationRequested || !EsCargaActual(source))
                    return;

                if (!resultado.Success || resultado.Data == null)
                {
                    if (!EsCancelacion(resultado.Message))
                        Mensaje = resultado.Message;

                    return;
                }

                PaisPaginaResponse pagina = resultado.Data;

                if (pagina.TotalRegistros > 0 &&
                    pagina.PaginaActual > Math.Max(1, pagina.TotalPaginas))
                {
                    ApiResult<PaisPaginaResponse> correccion =
                        await paisApiService.BuscarPaisesAsync(
                            textoBusquedaAplicado,
                            Math.Max(1, pagina.TotalPaginas),
                            ObtenerTamanoPagina(),
                            source.Token);

                    if (!correccion.Success || correccion.Data == null)
                    {
                        Mensaje = correccion.Message;
                        return;
                    }

                    pagina = correccion.Data;
                }

                AplicarPagina(pagina);
                pantallaCargada = true;
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested && EsCargaActual(source))
                {
                    Mensaje = "No fue posible cargar los países.";
                    await MostrarErrorInesperadoAsync("cargar los países", ex);
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
                NotificarEstado();
            }
        }

        private void AplicarPagina(PaisPaginaResponse pagina)
        {
            List.Clear();

            foreach (PaisResponse item in pagina.Items)
            {
                if (item.PaisId > 0)
                    List.Add(item);
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

        private Task RegresarConfiguracionAsync() =>
            NavegarAsync(AppRoutes.Configuracion);

        private Task OnAddAsync() =>
            NavegarAsync(
                "//PaisFormPage",
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["Pais"] = new PaisRequest()
                });

        private Task OnEditAsync(PaisResponse? pais)
        {
            if (!CanEdit || pais == null)
                return Task.CompletedTask;

            return NavegarAsync(
                "//PaisFormPage",
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Edit,
                    ["Pais"] = new PaisRequest(pais)
                });
        }

        private Task OnViewAsync(PaisResponse? pais)
        {
            if (!CanView || pais == null)
                return Task.CompletedTask;

            return NavegarAsync(
                "//DepartamentoPage",
                new Dictionary<string, object>
                {
                    ["Pais"] = new PaisRequest(pais),
                    ["TitlePage"] = $"Departamentos de {pais.NombrePais}"
                });
        }

        private async Task OnDeleteAsync(PaisResponse? pais)
        {
            if (Interlocked.CompareExchange(
                    ref eliminacionEnCurso,
                    1,
                    0) != 0)
            {
                return;
            }

            try
            {
                await OnDeleteCoreAsync(pais);
            }
            finally
            {
                Volatile.Write(ref eliminacionEnCurso, 0);
            }
        }

        private async Task OnDeleteCoreAsync(PaisResponse? pais)
        {
            if (!CanDelete || pais == null || IsBusy)
                return;

            bool confirmar = await Application.Current!.MainPage!.DisplayAlert(
                "Eliminar país",
                $"¿Desea eliminar el país '{pais.NombrePais}'?",
                "Eliminar",
                "Cancelar");

            if (!confirmar)
                return;

            try
            {
                MostrarRelay(
                    "Eliminando país...",
                    "Actualizando el estado del país en el servidor");
                IsBusy = true;
                ActualizarComandos();

                ApiResult<bool> resultado =
                    await paisApiService.DeletePaisResultAsync(
                        new PaisRequest(pais));

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                bool teniaPaginaPosterior = paginaActual < totalPaginas;
                List.Remove(pais);
                TotalRegistros = Math.Max(0, TotalRegistros - 1);
                RecalcularPaginasLocales();

                int destino = Math.Min(
                    Math.Max(1, paginaActual),
                    Math.Max(1, totalPaginas));

                bool requiereGet = teniaPaginaPosterior;

                if (List.Count == 0 && TotalRegistros > 0)
                {
                    // RecalcularPaginasLocales ya ajustó paginaActual a la
                    // última página válida. Se consulta esa página, no una
                    // adicional hacia atrás.
                    destino = Math.Max(1, paginaActual);
                    requiereGet = true;
                }

                if (requiereGet)
                {
                    await CargarPaginaAsync(
                        destino,
                        "Actualizando países...",
                        "Completando correctamente la página después de eliminar");
                }

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "País eliminado correctamente."
                        : resultado.Message);
            }
            finally
            {
                IsBusy = false;
                OcultarRelay();
                ActualizarComandos();
                NotificarEstado();
            }
        }

        private async Task NavegarAsync(
            string ruta,
            IDictionary<string, object>? parametros = null)
        {
            if (Navegando)
                return;

            Navegando = true;
            ActualizarComandos();

            try
            {
                CancelarCarga();

                if (parametros == null)
                    await GoToAsyncParameters(ruta);
                else
                    await GoToAsyncParameters(ruta, parametros);
            }
            finally
            {
                Navegando = false;
                ActualizarComandos();
            }
        }

        private int BuscarPais(int paisId)
        {
            for (int i = 0; i < List.Count; i++)
            {
                if (List[i].PaisId == paisId)
                    return i;
            }

            return -1;
        }

        private void OrdenarPaginaActual()
        {
            List<PaisResponse> ordenados = List
                .OrderBy(item => item.NombrePais, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(item => item.PaisId)
                .ToList();

            List.Clear();
            foreach (PaisResponse item in ordenados)
                List.Add(item);
        }

        private void RecalcularPaginasLocales()
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
                await MostrarErrorInesperadoAsync(descripcion, ex);
            }
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

        private void NotificarEstado()
        {
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(MostrarPaginacion));
            OnPropertyChanged(nameof(PuedeIrAnterior));
            OnPropertyChanged(nameof(PuedeIrSiguiente));
            OnPropertyChanged(nameof(PaginaActual));
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(PaginaTexto));
            OnPropertyChanged(nameof(RangoPaginaTexto));
            OnPropertyChanged(nameof(ResumenResultados));
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Platform == DevicePlatform.WinUI ? 40 : 20;

        private CancellationTokenSource PrepararNuevaCarga()
        {
            var source = new CancellationTokenSource();
            CancellationTokenSource? anterior =
                Interlocked.Exchange(ref cargaCts, source);

            CancelarSeguro(anterior);
            return source;
        }

        private bool EsCargaActual(CancellationTokenSource source) =>
            ReferenceEquals(Volatile.Read(ref cargaCts), source);

        private void LiberarCarga(CancellationTokenSource source)
        {
            Interlocked.CompareExchange(ref cargaCts, null, source);
            source.Dispose();
        }

        private static void CancelarSeguro(CancellationTokenSource? source)
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
        }

        private static bool EsCancelacion(string? valor) =>
            !string.IsNullOrWhiteSpace(valor) &&
            valor.Contains("cancel", StringComparison.OrdinalIgnoreCase);
    }
}
