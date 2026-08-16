using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class userPage : ContentPage
    {
        /*
         * Cada tarjeta necesita un ancho útil suficiente para conservar
         * correctamente avatar, datos, badges y acciones. Las columnas se
         * calculan por el ancho real disponible y no por el tipo de dispositivo.
         */
        private const double AnchoMinimoTarjeta = 430;
        private const double EspaciadoTarjetas = 12;

        private readonly UserViewModel viewModel = new();
        private int columnasActuales;
        private bool navegacionShellSuscrita;
        private bool salidaExternaPendiente;

        public userPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            salidaExternaPendiente = false;
            SuscribirNavegacionShell();

            viewModel.ActualizarPermisos();
            AjustarDiseno(Width);

            if (!viewModel.CanView)
                return;

            bool nuevaVisita =
                UsuarioVisitaService.AsegurarVisita();

            if (nuevaVisita)
            {
                await viewModel.IniciarNuevaVisitaAsync();
                return;
            }

            // Regresar desde Ver/Editar/Crear pertenece a la misma visita.
            // Normalmente se aplican únicamente los cambios confirmados por el
            // servidor sin ejecutar otro GET. La única excepción es una
            // reactivación desde Usuarios inactivos: al cambiar la composición
            // global del listado paginado se renueva solo la página visible.
            if (UsuarioVisitaService.ConsumirRecargaListado())
            {
                await viewModel.RecargarPaginaActualAsync();
                return;
            }

            viewModel.AplicarCambiosPendientes();

            if (!viewModel.TienePaginaCargada)
                await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            /*
             * Crear/Ver/Editar y Usuarios inactivos forman parte de la misma
             * visita y pueden reutilizar sus catálogos. Cuando la salida es a
             * cualquier otro módulo, se libera la visita para garantizar que
             * el próximo ingreso a Usuarios consulte datos frescos.
             */
            if (salidaExternaPendiente)
            {
                UsuarioVisitaService.FinalizarVisita();
                salidaExternaPendiente = false;
            }

            DesuscribirNavegacionShell();
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarDiseno(width);
        }

        private void AjustarDiseno(double width)
        {
            if (width <= 0)
                return;

            AjustarPaddingContenido(width);
            AjustarColumnas(width);
            AjustarPaginacion(width);
        }

        private void AjustarColumnas(double width)
        {
            if (UsuariosGridLayout == null)
                return;

            double anchoUtil =
                ObtenerAnchoUtilListado(width);

            int columnas =
                CalcularColumnas(anchoUtil);

            if (columnasActuales == columnas)
                return;

            columnasActuales = columnas;
            UsuariosGridLayout.Span = columnas;
        }

        private static int CalcularColumnas(
            double anchoUtil)
        {
            double requeridoTres =
                (AnchoMinimoTarjeta * 3) +
                (EspaciadoTarjetas * 2);

            if (anchoUtil >= requeridoTres)
                return 3;

            double requeridoDos =
                (AnchoMinimoTarjeta * 2) +
                EspaciadoTarjetas;

            return anchoUtil >= requeridoDos
                ? 2
                : 1;
        }

        private void AjustarPaginacion(double width)
        {
            if (PaginacionUsuarios == null)
                return;

            /*
             * El paginador usa el mismo ancho útil del listado. Esto evita que
             * una ventana WinUI estrecha conserve el ancho pensado para desktop.
             */
            double anchoDisponible =
                ObtenerAnchoUtilListado(width);

            PaginacionUsuarios.WidthRequest =
                Math.Min(
                    560,
                    Math.Max(0, anchoDisponible));
        }

        private void AjustarPaddingContenido(
            double width)
        {
            if (ContenidoUsuarios == null)
                return;

            /*
             * En WinUI una ventana estrecha sigue siendo Desktop para OnIdiom.
             * El padding debe responder al ancho real para no desperdiciar
             * espacio horizontal cuando la aplicación se redimensiona.
             */
            ContenidoUsuarios.Padding =
                width < 600
                    ? new Thickness(12, 12, 12, 20)
                    : width < 900
                        ? new Thickness(18, 16, 18, 24)
                        : new Thickness(24, 20, 24, 28);
        }

        private static double ObtenerAnchoUtilListado(
            double width)
        {
            double paddingHorizontal =
                width < 600
                    ? 24
                    : width < 900
                        ? 36
                        : 48;

            return Math.Max(
                0,
                width - paddingHorizontal);
        }


        /// <summary>
        /// Después de cambiar de página espera a que la consulta termine y
        /// posiciona el primer usuario de la nueva página al inicio visible.
        /// El evento Clicked se usa solo para la presentación; el Command del
        /// ViewModel continúa siendo el único responsable de la paginación.
        /// </summary>
        private async void PaginacionUsuarios_Clicked(
            object? sender,
            EventArgs e)
        {
            int paginaAnterior =
                viewModel.PaginaActual;

            bool operacionDetectada = false;

            for (int intento = 0; intento < 240; intento++)
            {
                if (viewModel.IsBusy ||
                    viewModel.PaginaActual != paginaAnterior)
                {
                    operacionDetectada = true;
                }

                if (operacionDetectada &&
                    !viewModel.IsBusy)
                {
                    if (viewModel.PaginaActual != paginaAnterior &&
                        viewModel.UsersList.Count > 0)
                    {
                        await DesplazarUsuariosAlInicioAsync();
                    }

                    return;
                }

                await Task.Delay(50);
            }
        }

        private async Task DesplazarUsuariosAlInicioAsync()
        {
            if (UsuariosCollectionView == null ||
                viewModel.UsersList.Count == 0)
            {
                return;
            }

            // Permite que CollectionView termine de materializar la nueva página.
            await Task.Delay(60);

            UsuariosCollectionView.ScrollTo(
                0,
                position: ScrollToPosition.Start,
                animate: false);
        }

        private void SuscribirNavegacionShell()
        {
            if (navegacionShellSuscrita ||
                Shell.Current == null)
            {
                return;
            }

            Shell.Current.Navigating +=
                Shell_Navigating;

            navegacionShellSuscrita = true;
        }

        private void DesuscribirNavegacionShell()
        {
            if (!navegacionShellSuscrita ||
                Shell.Current == null)
            {
                return;
            }

            Shell.Current.Navigating -=
                Shell_Navigating;

            navegacionShellSuscrita = false;
        }

        private void Shell_Navigating(
            object? sender,
            ShellNavigatingEventArgs e)
        {
            string rutaDestino =
                e.Target?
                    .Location?
                    .OriginalString ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(rutaDestino))
                return;

            /*
             * UserFormPage pertenece al flujo interno de Usuarios. El modal de
             * Usuarios inactivos no utiliza navegación Shell y por eso tampoco
             * marca una salida externa.
             */
            salidaExternaPendiente =
                !EsRutaInternaUsuarios(rutaDestino);
        }

        private static bool EsRutaInternaUsuarios(
            string ruta)
        {
            return
                ruta.Contains(
                    "UserPage",
                    StringComparison.OrdinalIgnoreCase) ||
                ruta.Contains(
                    "UserFormPage",
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
