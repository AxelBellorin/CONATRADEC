using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class departamentoPage : ContentPage, IQueryAttributable
    {
        private const double AnchoMinimoTarjeta = 380;
        private const double EspaciadoTarjetas = 12;

        private readonly DepartamentoViewModel viewModel = new();
        private readonly SemaphoreSlim inicializacionLock = new(1, 1);
        private readonly SemaphoreSlim procesamientoLock = new(1, 1);

        private PaisRequest paisPendiente = new();
        private string tituloPendiente = string.Empty;
        private long versionParametros;
        private long versionInicializada;
        private bool parametrosValidos;
        private bool paginaVisible;
        private bool navegacionShellSuscrita;
        private bool salidaExternaPendiente;
        private int columnasActuales;
        private int paginaAntesCambio = -1;
        private int versionVisitaAplicada;

        public departamentoPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;

            viewModel.List.CollectionChanged +=
                (_, _) => AjustarColumnas(Width);
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            bool tienePais =
                query.TryGetValue("Pais", out object? paisValue) &&
                paisValue is PaisRequest pais &&
                pais.PaisId > 0;

            parametrosValidos = tienePais;

            if (tienePais)
                paisPendiente = (PaisRequest)paisValue!;

            tituloPendiente =
                query.TryGetValue("TitlePage", out object? tituloValue) &&
                tituloValue is string titulo
                    ? titulo
                    : string.Empty;

            Interlocked.Increment(ref versionParametros);

            if (paginaVisible)
            {
                Dispatcher.Dispatch(
                    () => _ = ProcesarAparicionAsync());
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            paginaVisible = true;
            salidaExternaPendiente = false;
            SuscribirNavegacionShell();
            UbicacionVisitaService.AsegurarVisita();

            viewModel.ActualizarPermisos();
            AjustarDiseno(Width);

            await ProcesarAparicionAsync();
        }

        protected override void OnDisappearing()
        {
            paginaVisible = false;

            if (salidaExternaPendiente)
            {
                UbicacionVisitaService.FinalizarVisita();
                salidaExternaPendiente = false;
            }

            DesuscribirNavegacionShell();
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarDiseno(width);
        }

        protected override bool OnBackButtonPressed()
        {
            if (viewModel.ReturnCommand.CanExecute(null))
                viewModel.ReturnCommand.Execute(null);

            return true;
        }

        private async Task ProcesarAparicionAsync()
        {
            await procesamientoLock.WaitAsync();

            try
            {
                if (!paginaVisible)
                    return;

                if (!await AplicarParametrosPendientesAsync())
                    return;

            if (!viewModel.CanView || !viewModel.PaisValido)
                return;

            int versionVisita = UbicacionVisitaService.VersionActual;
            if (versionVisitaAplicada != versionVisita)
            {
                versionVisitaAplicada = versionVisita;
                await viewModel.IniciarNuevaVisitaAsync();
                return;
            }

            if (UbicacionVisitaService.ConsumirRecargaDepartamentos(
                    viewModel.PaisRequest.PaisId))
            {
                await viewModel.RecargarPaginaActualAsync();
                return;
            }

            bool requiereGet = viewModel.AplicarCambiosPendientes();
            if (requiereGet)
            {
                await viewModel.RecargarPaginaActualAsync();
                return;
            }

                if (!viewModel.TienePaginaCargada)
                    await viewModel.InicializarAsync();
            }
            finally
            {
                procesamientoLock.Release();
            }
        }

        private async Task<bool> AplicarParametrosPendientesAsync()
        {
            await inicializacionLock.WaitAsync();

            try
            {
                long actual = Volatile.Read(ref versionParametros);

                // Shell puede aplicar los parámetros después de OnAppearing.
                // Sin una versión recibida todavía, no se asume un error.
                if (actual <= 0)
                    return false;

                if (versionInicializada == actual)
                    return parametrosValidos;

                versionInicializada = actual;

                if (!parametrosValidos)
                {
                    await MostrarErrorNavegacionAsync();
                    return false;
                }

                viewModel.PaisRequest = paisPendiente;
                viewModel.TitlePage = tituloPendiente;
                AjustarDiseno(Width);
                return true;
            }
            finally
            {
                inicializacionLock.Release();
            }
        }

        private async Task MostrarErrorNavegacionAsync()
        {
            await DisplayAlert(
                "No fue posible abrir departamentos",
                "No se recibió correctamente el país requerido para esta pantalla.",
                "Aceptar");

            await Shell.Current.GoToAsync(AppRoutes.Paises);
        }

        private void AjustarDiseno(double width)
        {
            if (width <= 0)
                return;

            if (ContenidoDepartamentos != null)
            {
                ContenidoDepartamentos.Padding =
                    width < 600
                        ? new Thickness(12, 12, 12, 20)
                        : width < 900
                            ? new Thickness(18, 16, 18, 24)
                            : new Thickness(24, 20, 24, 28);
            }

            AjustarColumnas(width);

            if (PaginacionDepartamentos != null)
            {
                PaginacionDepartamentos.WidthRequest =
                    Math.Min(560, ObtenerAnchoUtil(width));
            }
        }

        private void AjustarColumnas(double width)
        {
            if (DepartamentosGridLayout == null)
                return;

            int columnas =
                viewModel.List.Count == 0
                    ? 1
                    : CalcularColumnas(ObtenerAnchoUtil(width));

            if (columnasActuales == columnas &&
                DepartamentosGridLayout.Span == columnas)
            {
                return;
            }

            columnasActuales = columnas;
            DepartamentosGridLayout.Span = columnas;
        }

        private static int CalcularColumnas(double anchoUtil)
        {
            if (anchoUtil >=
                (AnchoMinimoTarjeta * 3) + (EspaciadoTarjetas * 2))
            {
                return 3;
            }

            return anchoUtil >=
                   (AnchoMinimoTarjeta * 2) + EspaciadoTarjetas
                ? 2
                : 1;
        }

        private static double ObtenerAnchoUtil(double width)
        {
            double padding = width < 600 ? 24 : width < 900 ? 36 : 48;
            return Math.Max(0, width - padding);
        }

        private void PaginacionDepartamentos_Pressed(object? sender, EventArgs e)
        {
            paginaAntesCambio = viewModel.PaginaActual;
        }

        private async void PaginacionDepartamentos_Clicked(object? sender, EventArgs e)
        {
            int paginaOrigen =
                paginaAntesCambio > 0
                    ? paginaAntesCambio
                    : viewModel.PaginaActual;

            bool operacionDetectada = false;

            for (int intento = 0; intento < 240; intento++)
            {
                if (viewModel.IsBusy || viewModel.PaginaActual != paginaOrigen)
                    operacionDetectada = true;

                if (operacionDetectada && !viewModel.IsBusy)
                {
                    if (viewModel.PaginaActual != paginaOrigen &&
                        viewModel.List.Count > 0)
                    {
                        await DesplazarListadoAlInicioAsync();
                    }

                    paginaAntesCambio = -1;
                    return;
                }

                await Task.Delay(50);
            }

            paginaAntesCambio = -1;
        }

        private async Task DesplazarListadoAlInicioAsync()
        {
            if (DepartamentosCollectionView == null || viewModel.List.Count == 0)
                return;

            await Task.Delay(60);
            DepartamentosCollectionView.ScrollTo(
                0,
                position: ScrollToPosition.Start,
                animate: false);
        }

        private void SuscribirNavegacionShell()
        {
            if (navegacionShellSuscrita || Shell.Current == null)
                return;

            Shell.Current.Navigating += Shell_Navigating;
            navegacionShellSuscrita = true;
        }

        private void DesuscribirNavegacionShell()
        {
            if (!navegacionShellSuscrita || Shell.Current == null)
                return;

            Shell.Current.Navigating -= Shell_Navigating;
            navegacionShellSuscrita = false;
        }

        private void Shell_Navigating(object? sender, ShellNavigatingEventArgs e)
        {
            string ruta = e.Target?.Location?.OriginalString ?? string.Empty;
            if (string.IsNullOrWhiteSpace(ruta))
                return;

            salidaExternaPendiente =
                !paisPage.EsRutaInternaUbicaciones(ruta);
        }
    }
}
