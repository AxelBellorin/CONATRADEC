using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class MunicipioViewModel : GlobalService
    {
        private readonly MunicipioApiService municipioApiService;
        private CancellationTokenSource? cargaCts;
        private int eliminacionEnCurso;

        private DepartamentoRequest departamentoRequest = new();
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
        private int departamentoCargadoId;

        public MunicipioViewModel()
            : this(new MunicipioApiService())
        {
        }

        public MunicipioViewModel(MunicipioApiService municipioApiService)
        {
            this.municipioApiService = municipioApiService
                ?? throw new ArgumentNullException(nameof(municipioApiService));

            tamanoPaginaActual = ObtenerTamanoPagina();

            ReturnCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RegresarADepartamentosAsync,
                    "regresar a departamentos"),
                () => !IsBusy && !Navegando);

            AddCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    OnAddAsync,
                    "abrir el formulario de municipio"),
                () =>
                    CanAdd &&
                    UbicacionValida &&
                    !IsBusy &&
                    !Navegando);

            EditCommand = new Command<MunicipioResponse>(
                async municipio => await EjecutarSeguroAsync(
                    () => OnEditAsync(municipio),
                    "editar el municipio"),
                municipio =>
                    municipio != null &&
                    CanEdit &&
                    !IsBusy &&
                    !Navegando);

            DeleteCommand = new Command<MunicipioResponse>(
                async municipio => await EjecutarSeguroAsync(
                    () => OnDeleteAsync(municipio),
                    "eliminar el municipio"),
                municipio =>
                    municipio != null &&
                    CanDelete &&
                    !IsBusy &&
                    !Navegando);

            ViewCommand = new Command<MunicipioResponse>(
                async municipio => await EjecutarSeguroAsync(
                    () => OnViewAsync(municipio),
                    "consultar el municipio"),
                municipio =>
                    municipio != null &&
                    CanView &&
                    !IsBusy &&
                    !Navegando);

            BuscarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    AplicarBusquedaAsync,
                    "buscar municipios"),
                () =>
                    CanView &&
                    UbicacionValida &&
                    !IsBusy &&
                    !Navegando);

            LimpiarFiltrosCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    LimpiarFiltrosAsync,
                    "limpiar la búsqueda"),
                () =>
                    CanView &&
                    UbicacionValida &&
                    !IsBusy &&
                    !Navegando);

            RefrescarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RefrescarAsync,
                    "actualizar los municipios"),
                () =>
                    CanView &&
                    UbicacionValida &&
                    !IsBusy &&
                    !Navegando);

            PaginaAnteriorCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    IrPaginaAnteriorAsync,
                    "cargar la página anterior"),
                () =>
                    CanView &&
                    UbicacionValida &&
                    PuedeIrAnterior &&
                    !IsBusy &&
                    !Navegando);

            PaginaSiguienteCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    IrPaginaSiguienteAsync,
                    "cargar la página siguiente"),
                () =>
                    CanView &&
                    UbicacionValida &&
                    PuedeIrSiguiente &&
                    !IsBusy &&
                    !Navegando);
        }

        public ObservableCollection<MunicipioResponse> List { get; } = new();

        public Command ReturnCommand { get; }
        public Command AddCommand { get; }
        public Command<MunicipioResponse> EditCommand { get; }
        public Command<MunicipioResponse> DeleteCommand { get; }
        public Command<MunicipioResponse> ViewCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }

        public DepartamentoRequest DepartamentoRequest
        {
            get => departamentoRequest;
            set
            {
                DepartamentoRequest nuevo = value ?? new DepartamentoRequest();
                int anterior = departamentoRequest.DepartamentoId ?? 0;
                departamentoRequest = nuevo;

                OnPropertyChanged();
                OnPropertyChanged(nameof(NombreDepartamento));
                OnPropertyChanged(nameof(DepartamentoValido));
                OnPropertyChanged(nameof(UbicacionValida));
                OnPropertyChanged(nameof(TitlePage));

                if (anterior != (departamentoRequest.DepartamentoId ?? 0))
                {
                    CancelarCarga();
                    ReiniciarEstadoListado();
                }

                ActualizarComandos();
            }
        }

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
                OnPropertyChanged(nameof(UbicacionValida));
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
                ? $"Municipios de {NombreDepartamento}"
                : titlePage;
            set
            {
                titlePage = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string NombreDepartamento =>
            string.IsNullOrWhiteSpace(DepartamentoRequest.NombreDepartamento)
                ? "Departamento seleccionado"
                : DepartamentoRequest.NombreDepartamento;

        public string NombrePais =>
            string.IsNullOrWhiteSpace(PaisRequest.NombrePais)
                ? "País seleccionado"
                : PaisRequest.NombrePais;

        public string CodigoPais =>
            PaisRequest.CodigoISOPais ?? string.Empty;

        public bool MostrarCodigoPais =>
            !string.IsNullOrWhiteSpace(CodigoPais);

        public bool DepartamentoValido =>
            DepartamentoRequest.DepartamentoId is > 0;

        public bool PaisValido => PaisRequest.PaisId > 0;
        public bool UbicacionValida => DepartamentoValido && PaisValido;

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
            CanView && UbicacionValida && pantallaCargada && List.Count > 0;

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
                ? "1 municipio encontrado"
                : $"{TotalRegistros:N0} municipios encontrados";

        public bool MostrarVacio =>
            CanView &&
            UbicacionValida &&
            pantallaCargada &&
            !IsBusy &&
            List.Count == 0 &&
            !TieneMensaje;

        public bool MostrarAccesoDenegado => !CanView;
        public bool TienePaginaCargada => pantallaCargada;

        public void ActualizarPermisos()
        {
            LoadPagePermissions("municipioPage");

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
            if (!CanView || !UbicacionValida || Navegando)
                return;

            ReiniciarEstadoListado();
            await CargarPaginaAsync(
                1,
                "Cargando municipios...",
                "Consultando información actual del servidor");
        }

        public Task InicializarAsync()
        {
            if (!CanView || !UbicacionValida || Navegando)
                return Task.CompletedTask;

            int departamentoId = DepartamentoRequest.DepartamentoId!.Value;

            if (pantallaCargada && departamentoCargadoId == departamentoId)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                1,
                "Cargando municipios...",
                "Consultando información actual del servidor");
        }

        public Task RecargarPaginaActualAsync() =>
            CargarPaginaAsync(
                Math.Max(1, paginaActual),
                "Actualizando municipios...",
                "Aplicando los cambios realizados dentro del módulo");

        public bool AplicarCambiosPendientes()
        {
            if (!DepartamentoValido)
                return false;

            int departamentoId = DepartamentoRequest.DepartamentoId!.Value;

            if (!UbicacionVisitaService.ConsumirMunicipioActualizado(
                    departamentoId,
                    out MunicipioActualizadoPendiente mutacion))
            {
                return false;
            }

            int indice = BuscarMunicipio(mutacion.MunicipioId);
            if (indice < 0)
                return true;

            MunicipioResponse actual = List[indice];
            bool cambioOrden = !string.Equals(
                actual.NombreMunicipio,
                mutacion.NombreMunicipio,
                StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(textoBusquedaAplicado) ||
                (cambioOrden && totalPaginas > 1))
            {
                return true;
            }

            List[indice] = new MunicipioResponse
            {
                MunicipioId = actual.MunicipioId,
                NombreMunicipio = mutacion.NombreMunicipio,
                DepartamentoId = actual.DepartamentoId,
                NombreDepartamento = NombreDepartamento,
                PaisId = PaisRequest.PaisId,
                NombrePais = NombrePais,
                Activo = actual.Activo,
                CantidadTerrenos = actual.CantidadTerrenos,
                CantidadUsuarios = actual.CantidadUsuarios
            };

            if (cambioOrden)
                OrdenarPaginaActual();

            return false;
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
                "Buscando municipios...",
                "Consultando los registros que coinciden con la búsqueda");
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;

            await CargarPaginaAsync(
                1,
                "Actualizando municipios...",
                "Quitando filtros y consultando la primera página");
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;

            try
            {
                await CargarPaginaAsync(
                    Math.Max(1, paginaActual),
                    "Actualizando municipios...",
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
                    "Consultando la página anterior de municipios")
                : Task.CompletedTask;

        private Task IrPaginaSiguienteAsync() =>
            PuedeIrSiguiente
                ? CargarPaginaAsync(
                    paginaActual + 1,
                    "Cargando página siguiente...",
                    "Consultando la siguiente página de municipios")
                : Task.CompletedTask;

        private async Task CargarPaginaAsync(
            int paginaSolicitada,
            string tituloOperacion,
            string detalleOperacion)
        {
            if (!CanView || !UbicacionValida || Navegando)
                return;

            int departamentoId = DepartamentoRequest.DepartamentoId!.Value;
            paginaSolicitada = Math.Max(1, paginaSolicitada);
            CancellationTokenSource source = PrepararNuevaCarga();

            try
            {
                MostrarRelay(tituloOperacion, detalleOperacion);
                IsBusy = true;
                Mensaje = string.Empty;
                ActualizarComandos();
                NotificarEstado();

                ApiResult<MunicipioPaginaResponse> resultado =
                    await municipioApiService.BuscarMunicipiosAsync(
                        departamentoId,
                        textoBusquedaAplicado,
                        paginaSolicitada,
                        ObtenerTamanoPagina(),
                        source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source) ||
                    DepartamentoRequest.DepartamentoId != departamentoId)
                {
                    return;
                }

                if (!resultado.Success || resultado.Data == null)
                {
                    if (!EsCancelacion(resultado.Message))
                        Mensaje = resultado.Message;

                    return;
                }

                MunicipioPaginaResponse pagina = resultado.Data;

                if (pagina.TotalRegistros > 0 &&
                    pagina.PaginaActual > Math.Max(1, pagina.TotalPaginas))
                {
                    ApiResult<MunicipioPaginaResponse> correccion =
                        await municipioApiService.BuscarMunicipiosAsync(
                            departamentoId,
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
                departamentoCargadoId = departamentoId;
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
                    Mensaje = "No fue posible cargar los municipios.";
                    await MostrarErrorInesperadoAsync(
                        "cargar los municipios",
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

        private void AplicarPagina(MunicipioPaginaResponse pagina)
        {
            List.Clear();

            foreach (MunicipioResponse item in pagina.Items)
            {
                if (item.MunicipioId is not > 0)
                    continue;

                item.DepartamentoId = DepartamentoRequest.DepartamentoId;
                item.NombreDepartamento = NombreDepartamento;
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

        private Task RegresarADepartamentosAsync()
        {
            var parametros = new Dictionary<string, object>
            {
                ["Pais"] = PaisRequest,
                ["TitlePage"] = $"Departamentos de {NombrePais}"
            };

            return NavegarAsync("//DepartamentoPage", parametros);
        }

        private Task OnAddAsync()
        {
            if (!CanAdd || !UbicacionValida)
                return Task.CompletedTask;

            return NavegarAsync(
                "//MunicipioFormPage",
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["Pais"] = PaisRequest,
                    ["Departamento"] = DepartamentoRequest,
                    ["Municipio"] = new MunicipioRequest
                    {
                        DepartamentoId = DepartamentoRequest.DepartamentoId
                    }
                });
        }

        private Task OnEditAsync(MunicipioResponse? municipio)
        {
            if (!CanEdit || municipio == null)
                return Task.CompletedTask;

            return NavegarAsync(
                "//MunicipioFormPage",
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Edit,
                    ["Pais"] = PaisRequest,
                    ["Departamento"] = DepartamentoRequest,
                    ["Municipio"] = new MunicipioRequest(municipio)
                });
        }

        private Task OnViewAsync(MunicipioResponse? municipio)
        {
            if (!CanView || municipio == null)
                return Task.CompletedTask;

            return NavegarAsync(
                "//MunicipioFormPage",
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.View,
                    ["Pais"] = PaisRequest,
                    ["Departamento"] = DepartamentoRequest,
                    ["Municipio"] = new MunicipioRequest(municipio)
                });
        }

        private async Task OnDeleteAsync(MunicipioResponse? municipio)
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
                await OnDeleteCoreAsync(municipio);
            }
            finally
            {
                Volatile.Write(ref eliminacionEnCurso, 0);
            }
        }

        private async Task OnDeleteCoreAsync(MunicipioResponse? municipio)
        {
            if (!CanDelete || municipio?.MunicipioId is not > 0 || IsBusy)
                return;

            bool confirmar = await Application.Current!.MainPage!.DisplayAlert(
                "Eliminar municipio",
                $"¿Desea eliminar el municipio '{municipio.NombreMunicipio}'?",
                "Eliminar",
                "Cancelar");

            if (!confirmar)
                return;

            try
            {
                MostrarRelay(
                    "Eliminando municipio...",
                    "Actualizando el estado del municipio en el servidor");
                IsBusy = true;
                ActualizarComandos();

                var request = new MunicipioRequest(municipio)
                {
                    DepartamentoId = DepartamentoRequest.DepartamentoId
                };

                ApiResult<bool> resultado =
                    await municipioApiService.DeleteMunicipioResultAsync(request);

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                bool teniaPaginaPosterior = paginaActual < totalPaginas;
                List.Remove(municipio);
                TotalRegistros = Math.Max(0, TotalRegistros - 1);
                RecalcularPaginasLocales();

                UbicacionVisitaService.RegistrarDeltaMunicipiosDepartamento(
                    DepartamentoRequest.DepartamentoId!.Value,
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
                        "Actualizando municipios...",
                        "Completando correctamente la página después de eliminar");
                }

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Municipio eliminado correctamente."
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

        private int BuscarMunicipio(int municipioId)
        {
            for (int i = 0; i < List.Count; i++)
            {
                if (List[i].MunicipioId == municipioId)
                    return i;
            }

            return -1;
        }

        private void OrdenarPaginaActual()
        {
            List<MunicipioResponse> ordenados = List
                .OrderBy(
                    item => item.NombreMunicipio,
                    StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(item => item.MunicipioId)
                .ToList();

            List.Clear();
            foreach (MunicipioResponse item in ordenados)
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
            departamentoCargadoId = 0;
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
