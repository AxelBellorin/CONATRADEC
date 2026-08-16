using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Listado administrativo paginado de fuentes eliminadas.
    /// Solo mantiene en memoria la página visible y comunica la reactivación
    /// al listado activo de la misma visita.
    /// </summary>
    public sealed class FuenteNutrienteEliminadasViewModel : GlobalService
    {
        private readonly FuenteNutrienteConsultaApiService consultaApiService;
        private readonly FuenteNutrienteApiService apiService;

        private CancellationTokenSource? cargaCts;
        private CancellationTokenSource? accionCts;

        private string textoBusqueda = string.Empty;
        private string textoBusquedaAplicado = string.Empty;
        private string mensaje = string.Empty;
        private bool isRefreshing;
        private bool pantallaCargada;
        private int paginaActual = 1;
        private int totalPaginas = 1;
        private int totalRegistros;
        private int tamanoPaginaActual;
        private int reactivacionEnCurso;

        public FuenteNutrienteEliminadasViewModel()
            : this(
                new FuenteNutrienteConsultaApiService(),
                new FuenteNutrienteApiService())
        {
        }

        public FuenteNutrienteEliminadasViewModel(
            FuenteNutrienteConsultaApiService consultaApiService,
            FuenteNutrienteApiService apiService)
        {
            this.consultaApiService =
                consultaApiService ??
                throw new ArgumentNullException(nameof(consultaApiService));
            this.apiService =
                apiService ??
                throw new ArgumentNullException(nameof(apiService));

            tamanoPaginaActual = ObtenerTamanoPagina();

            BuscarCommand = new Command(
                async () => await AplicarBusquedaAsync(),
                () => CanView && !IsBusy);

            LimpiarCommand = new Command(
                async () => await LimpiarFiltroAsync(),
                () => CanView && !IsBusy);

            RefrescarCommand = new Command(
                async () => await RefrescarAsync(),
                () => CanView && !IsBusy);

            PaginaAnteriorCommand = new Command(
                async () => await IrPaginaAsync(paginaActual - 1, true),
                () => CanView && PuedeIrAnterior && !IsBusy);

            PaginaSiguienteCommand = new Command(
                async () => await IrPaginaAsync(paginaActual + 1, true),
                () => CanView && PuedeIrSiguiente && !IsBusy);

            ReactivarCommand = new Command<FuenteNutrienteResponse>(
                async fuente => await ReactivarAsync(fuente),
                fuente =>
                    fuente?.FuenteNutrientesId is > 0 &&
                    CanEdit &&
                    !IsBusy &&
                    Volatile.Read(ref reactivacionEnCurso) == 0);

            CerrarCommand = new Command(
                async () => await CerrarAsync(),
                () => !IsBusy);
        }

        public event EventHandler? SolicitarDesplazamientoInicio;

        public ObservableCollection<FuenteNutrienteResponse> Fuentes { get; } =
            new();

        public Command BuscarCommand { get; }
        public Command LimpiarCommand { get; }
        public Command RefrescarCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }
        public Command<FuenteNutrienteResponse> ReactivarCommand { get; }
        public Command CerrarCommand { get; }

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
                OnPropertyChanged(nameof(MostrarVacio));
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

        public int TotalRegistros
        {
            get => totalRegistros;
            private set
            {
                if (totalRegistros == value)
                    return;

                totalRegistros = value;
                OnPropertyChanged();
                NotificarPaginacion();
            }
        }

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(Mensaje);

        public bool MostrarVacio =>
            CanView &&
            pantallaCargada &&
            !IsBusy &&
            Fuentes.Count == 0 &&
            !TieneMensaje;

        public bool MostrarAccesoDenegado =>
            !CanView;

        public string Resumen =>
            TotalRegistros == 1
                ? "1 fuente eliminada"
                : $"{TotalRegistros} fuentes eliminadas";

        public bool PuedeIrAnterior =>
            pantallaCargada && paginaActual > 1;

        public bool PuedeIrSiguiente =>
            pantallaCargada && paginaActual < totalPaginas;

        public bool MostrarPaginacion =>
            CanView && pantallaCargada && Fuentes.Count > 0;

        public string PaginaTexto =>
            $"Página {Math.Max(1, paginaActual)} de {Math.Max(1, totalPaginas)}";

        public string RangoPaginaTexto
        {
            get
            {
                if (TotalRegistros <= 0 || Fuentes.Count == 0)
                    return "Sin registros en esta página";

                int inicio =
                    ((Math.Max(1, paginaActual) - 1) *
                     Math.Max(1, tamanoPaginaActual)) + 1;

                int fin =
                    Math.Min(
                        inicio + Fuentes.Count - 1,
                        TotalRegistros);

                return $"Mostrando {inicio}-{fin} de {TotalRegistros}";
            }
        }

        public async Task IniciarAsync()
        {
            ActualizarPermisos();

            textoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;
            paginaActual = 1;
            totalPaginas = 1;
            TotalRegistros = 0;
            pantallaCargada = false;
            Mensaje = string.Empty;

            OnPropertyChanged(nameof(TextoBusqueda));

            if (CanView)
                await CargarPaginaAsync(1, false);
        }

        public void CancelarOperaciones()
        {
            CancellationTokenSource? carga =
                Interlocked.Exchange(ref cargaCts, null);
            CancellationTokenSource? accion =
                Interlocked.Exchange(ref accionCts, null);

            CancelarSeguro(carga);
            CancelarSeguro(accion);
            IsBusy = false;
            IsRefreshing = false;
            ActualizarComandos();
        }

        private void ActualizarPermisos()
        {
            LoadPagePermissions("fuenteNutrientePage");
            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            ActualizarComandos();
            NotificarEstado();
        }

        private async Task AplicarBusquedaAsync()
        {
            textoBusquedaAplicado =
                (TextoBusqueda ?? string.Empty).Trim();

            await CargarPaginaAsync(1, false);
        }

        private async Task LimpiarFiltroAsync()
        {
            bool yaLimpio =
                string.IsNullOrWhiteSpace(TextoBusqueda) &&
                string.IsNullOrWhiteSpace(textoBusquedaAplicado) &&
                paginaActual == 1;

            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;

            if (yaLimpio && pantallaCargada)
                return;

            await CargarPaginaAsync(1, false);
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;

            try
            {
                await CargarPaginaAsync(
                    Math.Max(1, paginaActual),
                    false);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task IrPaginaAsync(
            int pagina,
            bool desplazar) =>
            CargarPaginaAsync(
                Math.Clamp(pagina, 1, Math.Max(1, totalPaginas)),
                desplazar);

        private async Task CargarPaginaAsync(
            int pagina,
            bool desplazar)
        {
            if (!CanView)
                return;

            CancellationTokenSource source = PrepararCarga();

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;
                ActualizarComandos();

                ApiResult<FuenteNutrientePaginaResponse> resultado =
                    await consultaApiService.BuscarInactivasAsync(
                        textoBusquedaAplicado,
                        pagina,
                        ObtenerTamanoPagina(),
                        source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source))
                {
                    return;
                }

                if (!resultado.Success || resultado.Data == null)
                {
                    if (!EsMensajeCancelacion(resultado.Message))
                    {
                        Mensaje = string.IsNullOrWhiteSpace(resultado.Message)
                            ? "No fue posible cargar las fuentes eliminadas."
                            : resultado.Message;
                    }

                    return;
                }

                AplicarPagina(resultado.Data);
                pantallaCargada = true;

                if (desplazar && Fuentes.Count > 0)
                {
                    SolicitarDesplazamientoInicio?.Invoke(
                        this,
                        EventArgs.Empty);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al reemplazar la consulta o cerrar la ventana.
            }
            catch (ObjectDisposedException)
            {
                // La pantalla terminó mientras concluía la solicitud.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaActual(source))
                {
                    Mensaje =
                        "Ocurrió un error inesperado al cargar las fuentes eliminadas.";

                    await MostrarToastAsync(
                        "Error: " + ex.Message);
                }
            }
            finally
            {
                if (EsCargaActual(source))
                    IsBusy = false;

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstado();
            }
        }

        private void AplicarPagina(
            FuenteNutrientePaginaResponse pagina)
        {
            Fuentes.Clear();

            foreach (FuenteNutrienteResponse fuente in pagina.Items)
            {
                if (fuente.FuenteNutrientesId is > 0 &&
                    fuente.Activo != true)
                {
                    Fuentes.Add(fuente);
                }
            }

            paginaActual = Math.Max(1, pagina.PaginaActual);
            totalPaginas = Math.Max(1, pagina.TotalPaginas);
            tamanoPaginaActual =
                pagina.TamanoPagina > 0
                    ? pagina.TamanoPagina
                    : ObtenerTamanoPagina();
            TotalRegistros = Math.Max(0, pagina.TotalRegistros);
            Mensaje = string.Empty;
            NotificarEstado();
        }

        private async Task ReactivarAsync(
            FuenteNutrienteResponse? fuente)
        {
            if (fuente?.FuenteNutrientesId is not > 0 ||
                !CanEdit ||
                IsBusy ||
                Interlocked.CompareExchange(
                    ref reactivacionEnCurso,
                    1,
                    0) != 0)
            {
                return;
            }

            bool recargar = false;
            int paginaRecarga = paginaActual;

            try
            {
                bool confirmar =
                    await Application.Current!
                        .MainPage!
                        .DisplayAlert(
                            "Reactivar fuente",
                            $"¿Desea reactivar '{fuente.NombreNutriente}' con sus datos y clasificación anteriores?",
                            "Reactivar",
                            "Cancelar");

                if (!confirmar)
                    return;

                CancellationTokenSource source = PrepararAccion();

                try
                {
                    IsBusy = true;
                    ActualizarComandos();

                    ApiResult<FuenteNutrienteResponse> resultado =
                        await apiService
                            .ReactivarFuenteNutrienteAdminResultAsync(
                                fuente.FuenteNutrientesId.Value,
                                source.Token);

                    if (source.IsCancellationRequested ||
                        !EsAccionActual(source))
                    {
                        return;
                    }

                    if (!resultado.Success)
                    {
                        if (!EsMensajeCancelacion(resultado.Message))
                        {
                            await MostrarToastAsync(
                                string.IsNullOrWhiteSpace(resultado.Message)
                                    ? "No fue posible reactivar la fuente."
                                    : resultado.Message);
                        }

                        return;
                    }

                    Fuentes.Remove(fuente);
                    TotalRegistros = Math.Max(0, TotalRegistros - 1);

                    totalPaginas =
                        TotalRegistros == 0
                            ? 1
                            : (int)Math.Ceiling(
                                TotalRegistros /
                                (double)Math.Max(1, tamanoPaginaActual));

                    if (paginaActual > totalPaginas)
                        paginaActual = totalPaginas;

                    paginaRecarga = Math.Max(1, paginaActual);
                    recargar =
                        TotalRegistros > 0 &&
                        (Fuentes.Count == 0 ||
                         paginaRecarga < totalPaginas);

                    FuenteNutrienteListadoEstadoService
                        .MarcarParaRecargar();

                    await MostrarToastAsync(
                        string.IsNullOrWhiteSpace(resultado.Message)
                            ? "Fuente reactivada correctamente."
                            : resultado.Message);

                    NotificarEstado();
                }
                finally
                {
                    IsBusy = false;
                    LiberarAccion(source);
                    ActualizarComandos();
                    NotificarEstado();
                }
            }
            finally
            {
                Interlocked.Exchange(
                    ref reactivacionEnCurso,
                    0);
                ActualizarComandos();
            }

            if (recargar)
                await CargarPaginaAsync(paginaRecarga, false);
        }

        private static async Task CerrarAsync()
        {
            if (Shell.Current?.Navigation?.ModalStack.Count > 0)
            {
                await Shell.Current.Navigation.PopModalAsync();
            }
        }

        private void NotificarEstado()
        {
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(Resumen));
            OnPropertyChanged(nameof(TieneMensaje));
            NotificarPaginacion();
        }

        private void NotificarPaginacion()
        {
            OnPropertyChanged(nameof(PuedeIrAnterior));
            OnPropertyChanged(nameof(PuedeIrSiguiente));
            OnPropertyChanged(nameof(MostrarPaginacion));
            OnPropertyChanged(nameof(PaginaTexto));
            OnPropertyChanged(nameof(RangoPaginaTexto));
        }

        private void ActualizarComandos()
        {
            BuscarCommand.ChangeCanExecute();
            LimpiarCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            PaginaAnteriorCommand.ChangeCanExecute();
            PaginaSiguienteCommand.ChangeCanExecute();
            ReactivarCommand.ChangeCanExecute();
            CerrarCommand.ChangeCanExecute();
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Platform == DevicePlatform.WinUI
                ? 40
                : 20;

        private CancellationTokenSource PrepararCarga()
        {
            var source = new CancellationTokenSource();
            CancellationTokenSource? anterior =
                Interlocked.Exchange(ref cargaCts, source);
            CancelarSeguro(anterior);
            return source;
        }

        private CancellationTokenSource PrepararAccion()
        {
            var source = new CancellationTokenSource();
            CancellationTokenSource? anterior =
                Interlocked.Exchange(ref accionCts, source);
            CancelarSeguro(anterior);
            return source;
        }

        private bool EsCargaActual(CancellationTokenSource source) =>
            ReferenceEquals(Volatile.Read(ref cargaCts), source);

        private bool EsAccionActual(CancellationTokenSource source) =>
            ReferenceEquals(Volatile.Read(ref accionCts), source);

        private void LiberarCarga(CancellationTokenSource source)
        {
            Interlocked.CompareExchange(ref cargaCts, null, source);
            source.Dispose();
        }

        private void LiberarAccion(CancellationTokenSource source)
        {
            Interlocked.CompareExchange(ref accionCts, null, source);
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
                // La operación ya había terminado.
            }
        }

        private static bool EsMensajeCancelacion(string? valor) =>
            !string.IsNullOrWhiteSpace(valor) &&
            valor.Contains(
                "cancel",
                StringComparison.OrdinalIgnoreCase);
    }
}
