using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class TerrenoViewModel : GlobalService
    {
        private readonly TerrenoApiService terrenoApiService;
        private readonly TerrenoBusquedaApiService busquedaApiService;
        private readonly PaisApiService paisApiService;
        private readonly DepartamentoApiService departamentoApiService;
        private readonly MunicipioApiService municipioApiService;

        private CancellationTokenSource? cargaCts;
        private CancellationTokenSource? ubicacionCts;

        private string textoBusqueda = string.Empty;
        private string codigoFiltro = string.Empty;
        private string propietarioFiltro = string.Empty;
        private string identificacionFiltro = string.Empty;
        private string direccionFiltro = string.Empty;
        private string extensionMinimaTexto = string.Empty;
        private string extensionMaximaTexto = string.Empty;
        private string ordenarSeleccionado = "Código (A-Z)";
        private string mensaje = string.Empty;

        private PaisResponse? paisSeleccionado;
        private DepartamentoResponse? departamentoSeleccionado;
        private MunicipioResponse? municipioSeleccionado;

        private bool mostrarFiltrosAvanzados;
        private bool filtrarPorFecha;
        private bool isRefreshing;
        private bool cargandoMas;
        private bool navegando;
        private bool ubicacionCargando;
        private bool pantallaCargada;
        private bool catalogosCargados;
        private bool actualizandoUbicacionInterna;

        private DateTime fechaDesde = DateTime.Today.AddYears(-1);
        private DateTime fechaHasta = DateTime.Today;

        private int paginaActual;
        private int totalPaginas = 1;
        private int totalRegistros;

        public TerrenoViewModel()
            : this(
                new TerrenoApiService(),
                new TerrenoBusquedaApiService(),
                new PaisApiService(),
                new DepartamentoApiService(),
                new MunicipioApiService())
        {
        }

        internal TerrenoViewModel(
            TerrenoApiService terrenoApiService,
            TerrenoBusquedaApiService busquedaApiService,
            PaisApiService paisApiService,
            DepartamentoApiService departamentoApiService,
            MunicipioApiService municipioApiService)
        {
            this.terrenoApiService = terrenoApiService;
            this.busquedaApiService = busquedaApiService;
            this.paisApiService = paisApiService;
            this.departamentoApiService = departamentoApiService;
            this.municipioApiService = municipioApiService;

            List = new ObservableCollection<TerrenoResponse>();
            Paises = new ObservableCollection<PaisResponse>();
            Departamentos = new ObservableCollection<DepartamentoResponse>();
            Municipios = new ObservableCollection<MunicipioResponse>();

            Ordenamientos = new ObservableCollection<string>
            {
                "Código (A-Z)",
                "Código (Z-A)",
                "Propietario (A-Z)",
                "Propietario (Z-A)",
                "Fecha más reciente",
                "Fecha más antigua",
                "Mayor extensión",
                "Menor extensión"
            };

            AddCommand = new Command(
                async () => await EjecutarSeguroAsync(OnAddAsync, "abrir el formulario de terreno"),
                () => CanAdd && !IsBusy && !Navegando);

            EditCommand = new Command<TerrenoResponse>(
                async item => await EjecutarSeguroAsync(() => OnEditAsync(item), "editar el terreno"),
                item => item != null && CanEdit && !IsBusy && !Navegando);

            DeleteCommand = new Command<TerrenoResponse>(
                async item => await EjecutarSeguroAsync(() => OnDeleteAsync(item), "eliminar el terreno"),
                item => item != null && CanDelete && !IsBusy && !Navegando);

            ViewCommand = new Command<TerrenoResponse>(
                async item => await EjecutarSeguroAsync(() => OnViewAsync(item), "abrir el terreno"),
                item => item != null && CanView && !IsBusy && !Navegando);

            BuscarCommand = new Command(
                async () => await EjecutarSeguroAsync(() => CargarAsync(true), "buscar terrenos"),
                () => CanView && !IsBusy && !Navegando);

            LimpiarFiltrosCommand = new Command(
                async () => await EjecutarSeguroAsync(LimpiarFiltrosAsync, "limpiar los filtros"),
                () => CanView && !IsBusy && !Navegando);

            AlternarFiltrosCommand = new Command(
                () => MostrarFiltrosAvanzados = !MostrarFiltrosAvanzados,
                () => CanView && !Navegando);

            RefrescarCommand = new Command(
                async () => await EjecutarSeguroAsync(RefrescarAsync, "actualizar los terrenos"),
                () => CanView && !IsBusy && !Navegando);

            CargarMasCommand = new Command(
                async () => await EjecutarSeguroAsync(() => CargarAsync(false), "cargar más terrenos"),
                () => CanView && !IsBusy && !CargandoMas && !Navegando && PuedeCargarMas);
        }

        public ObservableCollection<TerrenoResponse> List { get; }
        public ObservableCollection<PaisResponse> Paises { get; }
        public ObservableCollection<DepartamentoResponse> Departamentos { get; }
        public ObservableCollection<MunicipioResponse> Municipios { get; }
        public ObservableCollection<string> Ordenamientos { get; }

        public Command AddCommand { get; }
        public Command<TerrenoResponse> EditCommand { get; }
        public Command<TerrenoResponse> DeleteCommand { get; }
        public Command<TerrenoResponse> ViewCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command AlternarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command CargarMasCommand { get; }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set => AsignarTexto(ref textoBusqueda, value);
        }

        public string CodigoFiltro
        {
            get => codigoFiltro;
            set => AsignarTexto(ref codigoFiltro, value);
        }

        public string PropietarioFiltro
        {
            get => propietarioFiltro;
            set => AsignarTexto(ref propietarioFiltro, value);
        }

        public string IdentificacionFiltro
        {
            get => identificacionFiltro;
            set => AsignarTexto(ref identificacionFiltro, value);
        }

        public string DireccionFiltro
        {
            get => direccionFiltro;
            set => AsignarTexto(ref direccionFiltro, value);
        }

        public string ExtensionMinimaTexto
        {
            get => extensionMinimaTexto;
            set => AsignarTexto(ref extensionMinimaTexto, value);
        }

        public string ExtensionMaximaTexto
        {
            get => extensionMaximaTexto;
            set => AsignarTexto(ref extensionMaximaTexto, value);
        }

        public string OrdenarSeleccionado
        {
            get => ordenarSeleccionado;
            set
            {
                string nuevoValor = string.IsNullOrWhiteSpace(value)
                    ? "Código (A-Z)"
                    : value;

                if (ordenarSeleccionado == nuevoValor)
                    return;

                ordenarSeleccionado = nuevoValor;
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

        public PaisResponse? PaisSeleccionado
        {
            get => paisSeleccionado;
            set
            {
                if (ReferenceEquals(paisSeleccionado, value))
                    return;

                paisSeleccionado = value;
                OnPropertyChanged();

                if (!actualizandoUbicacionInterna)
                    _ = CargarDepartamentosPorPaisAsync(value);
            }
        }

        public DepartamentoResponse? DepartamentoSeleccionado
        {
            get => departamentoSeleccionado;
            set
            {
                if (ReferenceEquals(departamentoSeleccionado, value))
                    return;

                departamentoSeleccionado = value;
                OnPropertyChanged();

                if (!actualizandoUbicacionInterna)
                    _ = CargarMunicipiosPorDepartamentoAsync(value);
            }
        }

        public MunicipioResponse? MunicipioSeleccionado
        {
            get => municipioSeleccionado;
            set
            {
                if (ReferenceEquals(municipioSeleccionado, value))
                    return;

                municipioSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public bool MostrarFiltrosAvanzados
        {
            get => mostrarFiltrosAvanzados;
            set
            {
                if (mostrarFiltrosAvanzados == value)
                    return;

                mostrarFiltrosAvanzados = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextoBotonFiltros));
            }
        }

        public string TextoBotonFiltros =>
            MostrarFiltrosAvanzados ? "Ocultar filtros" : "Filtros";

        public bool FiltrarPorFecha
        {
            get => filtrarPorFecha;
            set
            {
                if (filtrarPorFecha == value)
                    return;

                filtrarPorFecha = value;
                OnPropertyChanged();
            }
        }

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
                NotificarEstadoLista();
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

        public bool UbicacionCargando
        {
            get => ubicacionCargando;
            private set
            {
                if (ubicacionCargando == value)
                    return;

                ubicacionCargando = value;
                OnPropertyChanged();
            }
        }

        public bool TieneMensaje => !string.IsNullOrWhiteSpace(Mensaje);
        public bool TieneTerrenos => List.Count > 0;
        public bool MostrarVacio => pantallaCargada && !TieneTerrenos && !IsBusy;
        public bool PuedeCargarMas => paginaActual < totalPaginas;
        public bool MostrarFinLista => pantallaCargada && TieneTerrenos && !PuedeCargarMas && !CargandoMas;

        public string TotalTexto => totalRegistros == 1
            ? "1 terreno"
            : $"{totalRegistros:N0} terrenos";

        public void ActualizarPermisos()
        {
            LoadPagePermissions("terrenoPage");
            ActualizarComandos();
        }

        public async Task InicializarAsync()
        {
            if (!CanView || Navegando)
                return;

            await CargarCatalogosAsync();
            await CargarAsync(true);
        }

        public void CancelarCarga()
        {
            CancellationTokenSource? carga = Interlocked.Exchange(ref cargaCts, null);
            CancelarSeguro(carga);

            CancellationTokenSource? ubicacion = Interlocked.Exchange(ref ubicacionCts, null);
            CancelarSeguro(ubicacion);

            IsBusy = false;
            IsRefreshing = false;
            CargandoMas = false;
            UbicacionCargando = false;
        }

        private async Task CargarCatalogosAsync()
        {
            if (catalogosCargados)
                return;

            ApiResult<ObservableCollection<PaisResponse>> result =
                await paisApiService.GetPaisResultAsync();

            if (!result.Success || result.Data == null)
            {
                Mensaje = result.Message;
                return;
            }

            Paises.Clear();

            foreach (PaisResponse pais in result.Data.OrderBy(x => x.NombrePais))
                Paises.Add(pais);

            catalogosCargados = true;
        }

        private async Task CargarDepartamentosPorPaisAsync(PaisResponse? pais)
        {
            CancellationTokenSource source = PrepararUbicacion();

            try
            {
                UbicacionCargando = true;
                LimpiarDepartamentoYMunicipio();

                if (pais?.PaisId is not > 0)
                    return;

                ApiResult<ObservableCollection<DepartamentoResponse>> result =
                    await departamentoApiService.GetDepartamentosResultAsync(
                        pais.PaisId,
                        source.Token);

                if (source.IsCancellationRequested || !EsUbicacionActual(source))
                    return;

                if (!result.Success || result.Data == null)
                {
                    Mensaje = result.Message;
                    return;
                }

                foreach (DepartamentoResponse item in result.Data
                             .OrderBy(x => x.NombreDepartamento))
                {
                    Departamentos.Add(item);
                }
            }
            catch (OperationCanceledException)
            {
                // La selección cambió antes de terminar la consulta anterior.
            }
            finally
            {
                if (EsUbicacionActual(source))
                    UbicacionCargando = false;

                LiberarUbicacion(source);
            }
        }

        private async Task CargarMunicipiosPorDepartamentoAsync(
            DepartamentoResponse? departamento)
        {
            CancellationTokenSource source = PrepararUbicacion();

            try
            {
                UbicacionCargando = true;
                LimpiarMunicipio();

                if (departamento?.DepartamentoId is not > 0)
                    return;

                ApiResult<ObservableCollection<MunicipioResponse>> result =
                    await municipioApiService.GetMunicipiosResultAsync(
                        departamento.DepartamentoId,
                        source.Token);

                if (source.IsCancellationRequested || !EsUbicacionActual(source))
                    return;

                if (!result.Success || result.Data == null)
                {
                    Mensaje = result.Message;
                    return;
                }

                foreach (MunicipioResponse item in result.Data
                             .OrderBy(x => x.NombreMunicipio))
                {
                    Municipios.Add(item);
                }
            }
            catch (OperationCanceledException)
            {
                // La selección cambió antes de terminar la consulta anterior.
            }
            finally
            {
                if (EsUbicacionActual(source))
                    UbicacionCargando = false;

                LiberarUbicacion(source);
            }
        }

        private async Task CargarAsync(bool reiniciar)
        {
            if (!CanView || Navegando)
                return;

            if (reiniciar && IsBusy)
                return;

            if (!reiniciar && (CargandoMas || !PuedeCargarMas))
                return;

            if (!ValidarFiltros())
                return;

            CancellationTokenSource source = PrepararCarga();

            try
            {
                if (reiniciar)
                {
                    IsBusy = true;
                    Mensaje = string.Empty;
                }
                else
                {
                    CargandoMas = true;
                }

                int pagina = reiniciar ? 1 : paginaActual + 1;
                (string ordenarPor, bool descendente) = ResolverOrdenamiento();

                ApiResult<TerrenoBusquedaPaginadaResponse> result =
                    await busquedaApiService.BuscarAsync(
                        TextoBusqueda,
                        CodigoFiltro,
                        PropietarioFiltro,
                        IdentificacionFiltro,
                        DireccionFiltro,
                        PaisSeleccionado?.PaisId,
                        DepartamentoSeleccionado?.DepartamentoId,
                        MunicipioSeleccionado?.MunicipioId,
                        FiltrarPorFecha ? DateOnly.FromDateTime(FechaDesde) : null,
                        FiltrarPorFecha ? DateOnly.FromDateTime(FechaHasta) : null,
                        ParseDecimal(ExtensionMinimaTexto),
                        ParseDecimal(ExtensionMaximaTexto),
                        ordenarPor,
                        descendente,
                        pagina,
                        ObtenerTamanoPagina(),
                        source.Token);

                if (source.IsCancellationRequested || !EsCargaActual(source))
                    return;

                if (!result.Success || result.Data == null)
                {
                    if (!EsMensajeCancelacion(result.Message))
                        Mensaje = result.Message;

                    return;
                }

                AplicarPagina(result.Data, reiniciar);
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al reemplazar la búsqueda o navegar.
            }
            catch (ObjectDisposedException)
            {
                // La navegación puede cerrar el stream en Android.
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    if (reiniciar)
                    {
                        IsBusy = false;
                        IsRefreshing = false;
                    }
                    else
                    {
                        CargandoMas = false;
                    }
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        private void AplicarPagina(
            TerrenoBusquedaPaginadaResponse pagina,
            bool reiniciar)
        {
            if (reiniciar)
                List.Clear();

            foreach (TerrenoResponse item in pagina.Data)
            {
                if (item.TerrenoId.HasValue &&
                    List.Any(x => x.TerrenoId == item.TerrenoId))
                {
                    continue;
                }

                List.Add(item);
            }

            paginaActual = Math.Max(1, pagina.Page);
            totalPaginas = Math.Max(1, pagina.TotalPages);
            totalRegistros = Math.Max(0, pagina.Total);
            pantallaCargada = true;

            NotificarEstadoLista();
        }

        private bool ValidarFiltros()
        {
            Mensaje = string.Empty;

            decimal? extensionMinima = ParseDecimal(ExtensionMinimaTexto);
            decimal? extensionMaxima = ParseDecimal(ExtensionMaximaTexto);

            if (!string.IsNullOrWhiteSpace(ExtensionMinimaTexto) && !extensionMinima.HasValue)
            {
                Mensaje = "La extensión mínima no tiene un formato válido.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(ExtensionMaximaTexto) && !extensionMaxima.HasValue)
            {
                Mensaje = "La extensión máxima no tiene un formato válido.";
                return false;
            }

            if (extensionMinima is < 0 || extensionMaxima is < 0)
            {
                Mensaje = "Las extensiones utilizadas como filtro no pueden ser negativas.";
                return false;
            }

            if (extensionMinima.HasValue &&
                extensionMaxima.HasValue &&
                extensionMinima.Value > extensionMaxima.Value)
            {
                Mensaje = "La extensión mínima no puede ser mayor que la extensión máxima.";
                return false;
            }

            if (FiltrarPorFecha && FechaDesde.Date > FechaHasta.Date)
            {
                Mensaje = "La fecha inicial no puede ser mayor que la fecha final.";
                return false;
            }

            return true;
        }

        private async Task LimpiarFiltrosAsync()
        {
            textoBusqueda = string.Empty;
            codigoFiltro = string.Empty;
            propietarioFiltro = string.Empty;
            identificacionFiltro = string.Empty;
            direccionFiltro = string.Empty;
            extensionMinimaTexto = string.Empty;
            extensionMaximaTexto = string.Empty;
            ordenarSeleccionado = "Código (A-Z)";
            filtrarPorFecha = false;
            fechaDesde = DateTime.Today.AddYears(-1);
            fechaHasta = DateTime.Today;

            actualizandoUbicacionInterna = true;

            try
            {
                paisSeleccionado = null;
                departamentoSeleccionado = null;
                municipioSeleccionado = null;
                Departamentos.Clear();
                Municipios.Clear();
            }
            finally
            {
                actualizandoUbicacionInterna = false;
            }

            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(CodigoFiltro));
            OnPropertyChanged(nameof(PropietarioFiltro));
            OnPropertyChanged(nameof(IdentificacionFiltro));
            OnPropertyChanged(nameof(DireccionFiltro));
            OnPropertyChanged(nameof(ExtensionMinimaTexto));
            OnPropertyChanged(nameof(ExtensionMaximaTexto));
            OnPropertyChanged(nameof(OrdenarSeleccionado));
            OnPropertyChanged(nameof(FiltrarPorFecha));
            OnPropertyChanged(nameof(FechaDesde));
            OnPropertyChanged(nameof(FechaHasta));
            OnPropertyChanged(nameof(PaisSeleccionado));
            OnPropertyChanged(nameof(DepartamentoSeleccionado));
            OnPropertyChanged(nameof(MunicipioSeleccionado));

            await CargarAsync(true);
        }

        private async Task RefrescarAsync()
        {
            try
            {
                IsRefreshing = true;
                await CargarAsync(true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task OnAddAsync()
        {
            var parameters = new Dictionary<string, object>
            {
                ["Mode"] = FormMode.FormModeSelect.Create,
                ["Terreno"] = new TerrenoRequest(new TerrenoResponse())
            };

            await NavegarAsync(AppRoutes.TerrenoFormulario, parameters);
        }

        private Task OnEditAsync(TerrenoResponse? item)
        {
            if (item == null)
                return Task.CompletedTask;

            return NavegarAsync(
                AppRoutes.TerrenoFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Edit,
                    ["Terreno"] = new TerrenoRequest(item)
                });
        }

        private Task OnViewAsync(TerrenoResponse? item)
        {
            if (item == null)
                return Task.CompletedTask;

            return NavegarAsync(
                AppRoutes.TerrenoFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.View,
                    ["Terreno"] = new TerrenoRequest(item)
                });
        }

        private async Task OnDeleteAsync(TerrenoResponse? item)
        {
            if (item == null)
                return;

            bool confirmar = await ConfirmarAsync(
                "Eliminar terreno",
                $"¿Desea eliminar el terreno {item.CodigoTerreno}? El registro se desactivará y se conservará su historial.",
                "Eliminar",
                "Cancelar");

            if (!confirmar)
                return;

            IsBusy = true;

            try
            {
                ApiResult<bool> resultado =
                    await terrenoApiService.DeleteTerrenoResultAsync(
                        new TerrenoRequest(item));

                if (!resultado.Success)
                {
                    await MostrarErrorAsync(resultado.Message);
                    return;
                }

                List.Remove(item);
                totalRegistros = Math.Max(0, totalRegistros - 1);
                NotificarEstadoLista();

                await MostrarExitoAsync("Terreno eliminado correctamente.");
            }
            finally
            {
                IsBusy = false;
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
                await Task.Yield();
                await GoToAsyncParameters(ruta, parametros);
            }
            finally
            {
                Navegando = false;
            }
        }

        private (string ordenarPor, bool descendente) ResolverOrdenamiento() =>
            OrdenarSeleccionado switch
            {
                "Código (Z-A)" => ("codigo", true),
                "Propietario (A-Z)" => ("propietario", false),
                "Propietario (Z-A)" => ("propietario", true),
                "Fecha más reciente" => ("fecha", true),
                "Fecha más antigua" => ("fecha", false),
                "Mayor extensión" => ("extension", true),
                "Menor extensión" => ("extension", false),
                _ => ("codigo", false)
            };

        private static decimal? ParseDecimal(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            string normalizado = valor.Trim().Replace(',', '.');

            return decimal.TryParse(
                normalizado,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal resultado)
                ? resultado
                : null;
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Current.Platform == DevicePlatform.WinUI ? 40 : 20;

        private void LimpiarDepartamentoYMunicipio()
        {
            actualizandoUbicacionInterna = true;

            try
            {
                departamentoSeleccionado = null;
                municipioSeleccionado = null;
                Departamentos.Clear();
                Municipios.Clear();
                OnPropertyChanged(nameof(DepartamentoSeleccionado));
                OnPropertyChanged(nameof(MunicipioSeleccionado));
            }
            finally
            {
                actualizandoUbicacionInterna = false;
            }
        }

        private void LimpiarMunicipio()
        {
            actualizandoUbicacionInterna = true;

            try
            {
                municipioSeleccionado = null;
                Municipios.Clear();
                OnPropertyChanged(nameof(MunicipioSeleccionado));
            }
            finally
            {
                actualizandoUbicacionInterna = false;
            }
        }

        private void AsignarTexto(
            ref string campo,
            string? valor,
            [CallerMemberName] string? propertyName = null)
        {
            string nuevoValor = valor ?? string.Empty;

            if (campo == nuevoValor)
                return;

            campo = nuevoValor;
            OnPropertyChanged(propertyName);
        }

        private CancellationTokenSource PrepararCarga()
        {
            var source = new CancellationTokenSource();
            CancellationTokenSource? anterior = Interlocked.Exchange(ref cargaCts, source);
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

        private CancellationTokenSource PrepararUbicacion()
        {
            var source = new CancellationTokenSource();
            CancellationTokenSource? anterior = Interlocked.Exchange(ref ubicacionCts, source);
            CancelarSeguro(anterior);
            return source;
        }

        private bool EsUbicacionActual(CancellationTokenSource source) =>
            ReferenceEquals(Volatile.Read(ref ubicacionCts), source);

        private void LiberarUbicacion(CancellationTokenSource source)
        {
            Interlocked.CompareExchange(ref ubicacionCts, null, source);
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

        private static bool EsMensajeCancelacion(string? mensaje) =>
            string.Equals(
                mensaje,
                "La operación fue cancelada.",
                StringComparison.OrdinalIgnoreCase);

        private async Task EjecutarSeguroAsync(Func<Task> accion, string operacion)
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
                await MostrarErrorInesperadoAsync(operacion, ex);
            }
        }

        private void NotificarEstadoLista()
        {
            OnPropertyChanged(nameof(TieneTerrenos));
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(PuedeCargarMas));
            OnPropertyChanged(nameof(MostrarFinLista));
            OnPropertyChanged(nameof(TotalTexto));
        }

        private void ActualizarComandos()
        {
            AddCommand.ChangeCanExecute();
            EditCommand.ChangeCanExecute();
            DeleteCommand.ChangeCanExecute();
            ViewCommand.ChangeCanExecute();
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            AlternarFiltrosCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            CargarMasCommand.ChangeCanExecute();
        }
    }
}
