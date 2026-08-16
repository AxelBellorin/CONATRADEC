using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class DepartamentoViewModel : GlobalService
    {
        private readonly DepartamentoApiService departamentoApiService;
        private CancellationTokenSource? cargaCts;
        private int eliminacionEnCurso;

        private PaisRequest paisRequest = new();
        private string titlePage = string.Empty;
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
        private int paisCargadoId;

        public DepartamentoViewModel()
            : this(new DepartamentoApiService())
        {
        }

        public DepartamentoViewModel(
            DepartamentoApiService departamentoApiService)
        {
            this.departamentoApiService = departamentoApiService
                ?? throw new ArgumentNullException(nameof(departamentoApiService));

            tamanoPaginaActual = ObtenerTamanoPagina();

            ReturnCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RegresarAPaisesAsync,
                    "regresar a países"),
                () => !IsBusy && !Navegando);

            AddCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    OnAddAsync,
                    "abrir el formulario de departamento"),
                () =>
                    CanAdd &&
                    PaisValido &&
                    !IsBusy &&
                    !Navegando);

            EditCommand = new Command<DepartamentoResponse>(
                async departamento => await EjecutarSeguroAsync(
                    () => OnEditAsync(departamento),
                    "editar el departamento"),
                departamento =>
                    departamento != null &&
                    CanEdit &&
                    !IsBusy &&
                    !Navegando);

            DeleteCommand = new Command<DepartamentoResponse>(
                async departamento => await EjecutarSeguroAsync(
                    () => OnDeleteAsync(departamento),
                    "eliminar el departamento"),
                departamento =>
                    departamento != null &&
                    CanDelete &&
                    !IsBusy &&
                    !Navegando);

            ViewCommand = new Command<DepartamentoResponse>(
                async departamento => await EjecutarSeguroAsync(
                    () => OnViewAsync(departamento),
                    "consultar los municipios"),
                departamento =>
                    departamento != null &&
                    CanView &&
                    !IsBusy &&
                    !Navegando);

            BuscarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    AplicarBusquedaAsync,
                    "buscar departamentos"),
                () =>
                    CanView &&
                    PaisValido &&
                    !IsBusy &&
                    !Navegando);

            LimpiarFiltrosCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    LimpiarFiltrosAsync,
                    "limpiar la búsqueda"),
                () =>
                    CanView &&
                    PaisValido &&
                    !IsBusy &&
                    !Navegando);

            RefrescarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RefrescarAsync,
                    "actualizar los departamentos"),
                () =>
                    CanView &&
                    PaisValido &&
                    !IsBusy &&
                    !Navegando);

            PaginaAnteriorCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    IrPaginaAnteriorAsync,
                    "cargar la página anterior"),
                () =>
                    CanView &&
                    PaisValido &&
                    PuedeIrAnterior &&
                    !IsBusy &&
                    !Navegando);

            PaginaSiguienteCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    IrPaginaSiguienteAsync,
                    "cargar la página siguiente"),
                () =>
                    CanView &&
                    PaisValido &&
                    PuedeIrSiguiente &&
                    !IsBusy &&
                    !Navegando);
        }

        public ObservableCollection<DepartamentoResponse> List { get; } = new();

        public Command ReturnCommand { get; }
        public Command AddCommand { get; }
        public Command<DepartamentoResponse> EditCommand { get; }
        public Command<DepartamentoResponse> DeleteCommand { get; }
        public Command<DepartamentoResponse> ViewCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }

        public PaisRequest PaisRequest
        {
            get => paisRequest;
            set
            {
                PaisRequest nuevo = value ?? new PaisRequest();
                int anterior = paisRequest.PaisId;
                paisRequest = nuevo;

                OnPropertyChanged();
                OnPropertyChanged(nameof(NombrePais));
                OnPropertyChanged(nameof(CodigoPais));
                OnPropertyChanged(nameof(MostrarCodigoPais));
                OnPropertyChanged(nameof(PaisValido));
                OnPropertyChanged(nameof(TitlePage));

                if (anterior != paisRequest.PaisId)
                {
                    CancelarCarga();
                    ReiniciarEstadoListado();
                }

                ActualizarComandos();
            }
        }

        public string TitlePage
        {
            get => string.IsNullOrWhiteSpace(titlePage)
                ? $"Departamentos de {NombrePais}"
                : titlePage;
            set
            {
                titlePage = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string NombrePais =>
            string.IsNullOrWhiteSpace(PaisRequest.NombrePais)
                ? "País seleccionado"
                : PaisRequest.NombrePais;

        public string CodigoPais =>
            PaisRequest.CodigoISOPais ?? string.Empty;

        public bool MostrarCodigoPais =>
            !string.IsNullOrWhiteSpace(CodigoPais);

        public bool PaisValido => PaisRequest.PaisId > 0;

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
            CanView && PaisValido && pantallaCargada && List.Count > 0;

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
                ? "1 departamento encontrado"
                : $"{TotalRegistros:N0} departamentos encontrados";

        public bool MostrarVacio =>
            CanView &&
            PaisValido &&
            pantallaCargada &&
            !IsBusy &&
            List.Count == 0 &&
            !TieneMensaje;

        public bool MostrarAccesoDenegado => !CanView;
        public bool TienePaginaCargada => pantallaCargada;

        public void ActualizarPermisos()
        {
            LoadPagePermissions("departamentoPage");

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
            if (!CanView || !PaisValido || Navegando)
                return;

            ReiniciarEstadoListado();
            await CargarPaginaAsync(
                1,
                "Cargando departamentos...",
                "Consultando información actual del servidor");
        }

        public Task InicializarAsync()
        {
            if (!CanView || !PaisValido || Navegando)
                return Task.CompletedTask;

            if (pantallaCargada && paisCargadoId == PaisRequest.PaisId)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                1,
                "Cargando departamentos...",
                "Consultando información actual del servidor");
        }

        public Task RecargarPaginaActualAsync() =>
            CargarPaginaAsync(
                Math.Max(1, paginaActual),
                "Actualizando departamentos...",
                "Aplicando los cambios realizados dentro del módulo");

        public bool AplicarCambiosPendientes()
        {
            if (!PaisValido)
                return false;

            bool requiereGet = false;
            int paisId = PaisRequest.PaisId;

            if (UbicacionVisitaService.ConsumirDepartamentoActualizado(
                    paisId,
                    out DepartamentoActualizadoPendiente mutacion))
            {
                int indice = BuscarDepartamento(mutacion.DepartamentoId);

                if (indice >= 0)
                {
                    DepartamentoResponse actual = List[indice];
                    bool cambioOrden = !string.Equals(
                        actual.NombreDepartamento,
                        mutacion.NombreDepartamento,
                        StringComparison.OrdinalIgnoreCase);

                    if (!string.IsNullOrWhiteSpace(textoBusquedaAplicado) ||
                        (cambioOrden && totalPaginas > 1))
                    {
                        requiereGet = true;
                    }
                    else
                    {
                        List[indice] = new DepartamentoResponse
                        {
                            DepartamentoId = actual.DepartamentoId,
                            NombreDepartamento = mutacion.NombreDepartamento,
                            PaisId = actual.PaisId,
                            NombrePais = NombrePais,
                            Activo = actual.Activo,
                            CantidadMunicipios = actual.CantidadMunicipios
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
                DepartamentoResponse actual = List[i];
                if (actual.DepartamentoId is not > 0)
                    continue;

                if (!UbicacionVisitaService
                    .ConsumirDeltaMunicipiosDepartamento(
                        actual.DepartamentoId.Value,
                        out int delta))
                {
                    continue;
                }

                List[i] = new DepartamentoResponse
                {
                    DepartamentoId = actual.DepartamentoId,
                    NombreDepartamento = actual.NombreDepartamento,
                    PaisId = actual.PaisId,
                    NombrePais = NombrePais,
                    Activo = actual.Activo,
                    CantidadMunicipios = Math.Max(
                        0,
                        actual.CantidadMunicipios + delta)
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
                "Buscando departamentos...",
                "Consultando los registros que coinciden con la búsqueda");
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;

            await CargarPaginaAsync(
                1,
                "Actualizando departamentos...",
                "Quitando filtros y consultando la primera página");
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;

            try
            {
                await CargarPaginaAsync(
                    Math.Max(1, paginaActual),
                    "Actualizando departamentos...",
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
                    "Consultando la página anterior de departamentos")
                : Task.CompletedTask;

        private Task IrPaginaSiguienteAsync() =>
            PuedeIrSiguiente
                ? CargarPaginaAsync(
                    paginaActual + 1,
                    "Cargando página siguiente...",
                    "Consultando la siguiente página de departamentos")
                : Task.CompletedTask;

        private async Task CargarPaginaAsync(
            int paginaSolicitada,
            string tituloOperacion,
            string detalleOperacion)
        {
            if (!CanView || !PaisValido || Navegando)
                return;

            int paisId = PaisRequest.PaisId;
            paginaSolicitada = Math.Max(1, paginaSolicitada);
            CancellationTokenSource source = PrepararNuevaCarga();

            try
            {
                MostrarRelay(tituloOperacion, detalleOperacion);
                IsBusy = true;
                Mensaje = string.Empty;
                ActualizarComandos();
                NotificarEstado();

                ApiResult<DepartamentoPaginaResponse> resultado =
                    await departamentoApiService.BuscarDepartamentosAsync(
                        paisId,
                        textoBusquedaAplicado,
                        paginaSolicitada,
                        ObtenerTamanoPagina(),
                        source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source) ||
                    PaisRequest.PaisId != paisId)
                {
                    return;
                }

                if (!resultado.Success || resultado.Data == null)
                {
                    if (!EsCancelacion(resultado.Message))
                        Mensaje = resultado.Message;

                    return;
                }

                DepartamentoPaginaResponse pagina = resultado.Data;

                if (pagina.TotalRegistros > 0 &&
                    pagina.PaginaActual > Math.Max(1, pagina.TotalPaginas))
                {
                    ApiResult<DepartamentoPaginaResponse> correccion =
                        await departamentoApiService.BuscarDepartamentosAsync(
                            paisId,
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
                paisCargadoId = paisId;
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
                    Mensaje = "No fue posible cargar los departamentos.";
                    await MostrarErrorInesperadoAsync(
                        "cargar los departamentos",
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
                NotificarEstado();
            }
        }

        private void AplicarPagina(DepartamentoPaginaResponse pagina)
        {
            List.Clear();

            foreach (DepartamentoResponse item in pagina.Items)
            {
                if (item.DepartamentoId is not > 0)
                    continue;

                item.PaisId = PaisRequest.PaisId;
                item.NombrePais = NombrePais;
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

        private Task RegresarAPaisesAsync() =>
            NavegarAsync(AppRoutes.Paises);

        private Task OnAddAsync()
        {
            if (!CanAdd || !PaisValido)
                return Task.CompletedTask;

            return NavegarAsync(
                "//DepartamentoFormPage",
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["Pais"] = PaisRequest,
                    ["Departamento"] = new DepartamentoRequest
                    {
                        PaisId = PaisRequest.PaisId
                    }
                });
        }

        private Task OnEditAsync(DepartamentoResponse? departamento)
        {
            if (!CanEdit || departamento == null)
                return Task.CompletedTask;

            return NavegarAsync(
                "//DepartamentoFormPage",
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Edit,
                    ["Pais"] = PaisRequest,
                    ["Departamento"] = new DepartamentoRequest(departamento)
                });
        }

        private Task OnViewAsync(DepartamentoResponse? departamento)
        {
            if (!CanView || departamento == null)
                return Task.CompletedTask;

            return NavegarAsync(
                "//MunicipioPage",
                new Dictionary<string, object>
                {
                    ["Pais"] = PaisRequest,
                    ["Departamento"] = new DepartamentoRequest(departamento),
                    ["TitlePage"] =
                        $"Municipios de {departamento.NombreDepartamento} - {NombrePais}"
                });
        }

        private async Task OnDeleteAsync(DepartamentoResponse? departamento)
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
                await OnDeleteCoreAsync(departamento);
            }
            finally
            {
                Volatile.Write(ref eliminacionEnCurso, 0);
            }
        }

        private async Task OnDeleteCoreAsync(DepartamentoResponse? departamento)
        {
            if (!CanDelete ||
                departamento?.DepartamentoId is not > 0 ||
                IsBusy)
            {
                return;
            }

            bool confirmar = await Application.Current!.MainPage!.DisplayAlert(
                "Eliminar departamento",
                $"¿Desea eliminar el departamento '{departamento.NombreDepartamento}'?",
                "Eliminar",
                "Cancelar");

            if (!confirmar)
                return;

            try
            {
                MostrarRelay(
                    "Eliminando departamento...",
                    "Actualizando el estado del departamento en el servidor");
                IsBusy = true;
                ActualizarComandos();

                var request = new DepartamentoRequest(departamento)
                {
                    PaisId = PaisRequest.PaisId
                };

                ApiResult<bool> resultado =
                    await departamentoApiService.DeleteDepartamentoResultAsync(
                        request);

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                bool teniaPaginaPosterior = paginaActual < totalPaginas;
                List.Remove(departamento);
                TotalRegistros = Math.Max(0, TotalRegistros - 1);
                RecalcularPaginasLocales();

                UbicacionVisitaService.RegistrarDeltaDepartamentosPais(
                    PaisRequest.PaisId,
                    -1);

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
                        "Actualizando departamentos...",
                        "Completando correctamente la página después de eliminar");
                }

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Departamento eliminado correctamente."
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

        private int BuscarDepartamento(int departamentoId)
        {
            for (int i = 0; i < List.Count; i++)
            {
                if (List[i].DepartamentoId == departamentoId)
                    return i;
            }

            return -1;
        }

        private void OrdenarPaginaActual()
        {
            List<DepartamentoResponse> ordenados = List
                .OrderBy(
                    item => item.NombreDepartamento,
                    StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(item => item.DepartamentoId)
                .ToList();

            List.Clear();
            foreach (DepartamentoResponse item in ordenados)
                List.Add(item);
        }

        private void ReiniciarEstadoListado()
        {
            CancelarCarga();
            List.Clear();
            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;
            Mensaje = string.Empty;
            paginaActual = 1;
            totalPaginas = 1;
            TotalRegistros = 0;
            tamanoPaginaActual = ObtenerTamanoPagina();
            paisCargadoId = 0;
            pantallaCargada = false;
            NotificarEstado();
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

        private void OcultarRelay() => MostrandoRelay = false;

        private void ActualizarComandos()
        {
            ReturnCommand.ChangeCanExecute();
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
