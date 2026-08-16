using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class tipoCultivoPage : ContentPage
    {
        /*
         * El número de columnas se calcula a partir del ancho útil real.
         * Esto evita que WinUI estrecho conserve dos columnas solamente por
         * seguir siendo un dispositivo Desktop.
         */
        private const double AnchoMinimoTarjeta = 390;
        private const double EspaciadoTarjetas = 12;

        private readonly TipoCultivoViewModel
            viewModel = new();

        private int cantidadColumnasActual;
        private bool navegacionShellSuscrita;
        private bool salidaExternaPendiente;
        private int paginaAntesCambio = -1;

        public tipoCultivoPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext =
                viewModel;

            viewModel.List.CollectionChanged +=
                (_, _) => AjustarCantidadColumnas(Width);

            TiposCultivoCollectionView.SizeChanged +=
                (_, _) => AjustarDiseno(Width);
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
                TipoCultivoListadoEstadoService
                    .AsegurarVisita();

            if (nuevaVisita)
            {
                await viewModel
                    .IniciarNuevaVisitaAsync();
                return;
            }

            /*
             * Crear, editar, consultar y el modal de eliminados pertenecen a
             * la misma visita. InicializarAsync solo consulta nuevamente cuando
             * el listado todavía no fue cargado o cuando ocurrió un cambio real.
             */
            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            /*
             * Una navegación hacia el formulario sigue dentro del módulo.
             * Al abandonar Tipos de cultivo hacia cualquier otro módulo se
             * finaliza la visita para que el próximo ingreso consulte datos
             * frescos y arranque nuevamente desde la primera página.
             */
            if (salidaExternaPendiente)
            {
                TipoCultivoListadoEstadoService
                    .FinalizarVisita();

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
            base.OnSizeAllocated(
                width,
                height);

            AjustarDiseno(width);
        }

        private void AjustarDiseno(
            double width)
        {
            if (width <= 0)
                return;

            AjustarPaddingContenido(width);
            AjustarCantidadColumnas(width);
            AjustarPaginacion(width);
        }

        private void AjustarPaddingContenido(
            double width)
        {
            if (ContenidoTiposCultivo == null)
                return;

            /*
             * OnIdiom Desktop no cambia cuando una ventana de Windows se hace
             * estrecha. El padding se adapta por ancho real para conservar
             * espacio útil en teléfono, tablet y WinUI reducido.
             */
            ContenidoTiposCultivo.Padding =
                width < 600
                    ? new Thickness(12, 12, 12, 20)
                    : width < 900
                        ? new Thickness(18, 16, 18, 24)
                        : new Thickness(24, 20, 24, 28);
        }

        private void AjustarCantidadColumnas(
            double width)
        {
            if (width <= 0 ||
                TiposCultivoGridLayout == null)
            {
                return;
            }

            double anchoUtil =
                ObtenerAnchoUtilListado(width);

            int nuevasColumnas =
                viewModel.List.Count == 0
                    ? 1
                    : CalcularColumnas(anchoUtil);

            if (cantidadColumnasActual ==
                    nuevasColumnas &&
                TiposCultivoGridLayout.Span ==
                    nuevasColumnas)
            {
                return;
            }

            cantidadColumnasActual =
                nuevasColumnas;

            TiposCultivoGridLayout.Span =
                nuevasColumnas;
        }

        private static int CalcularColumnas(
            double anchoUtil)
        {
            if (anchoUtil <= 0)
                return 1;

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

        private double ObtenerAnchoUtilListado(
            double width)
        {
            if (TiposCultivoCollectionView?.Width > 0)
                return TiposCultivoCollectionView.Width;

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

        private void AjustarPaginacion(
            double width)
        {
            if (PaginacionTiposCultivo == null)
                return;

            double anchoDisponible =
                ObtenerAnchoUtilListado(width);

            PaginacionTiposCultivo.WidthRequest =
                Math.Min(
                    560,
                    Math.Max(
                        0,
                        anchoDisponible));
        }

        /// <summary>
        /// Captura la página actual antes de ejecutar Anterior/Siguiente.
        /// Pressed ocurre antes del Command y evita perder la referencia cuando
        /// la API responde muy rápido.
        /// </summary>
        private void PaginacionTiposCultivo_Pressed(
            object? sender,
            EventArgs e)
        {
            paginaAntesCambio =
                viewModel.PaginaActual;
        }

        /// <summary>
        /// Después de cargar la nueva página posiciona su primer registro al
        /// inicio visible. El ViewModel continúa siendo el único responsable
        /// de consultar y reemplazar la página.
        /// </summary>
        private async void PaginacionTiposCultivo_Clicked(
            object? sender,
            EventArgs e)
        {
            int paginaOrigen =
                paginaAntesCambio > 0
                    ? paginaAntesCambio
                    : viewModel.PaginaActual;

            bool operacionDetectada = false;

            for (int intento = 0;
                 intento < 240;
                 intento++)
            {
                if (viewModel.IsBusy ||
                    viewModel.PaginaActual !=
                        paginaOrigen)
                {
                    operacionDetectada = true;
                }

                if (operacionDetectada &&
                    !viewModel.IsBusy)
                {
                    if (viewModel.PaginaActual !=
                            paginaOrigen &&
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

        private async Task
            DesplazarListadoAlInicioAsync()
        {
            if (TiposCultivoCollectionView == null ||
                viewModel.List.Count == 0)
            {
                return;
            }

            // Permite que CollectionView materialice la página recién recibida.
            await Task.Delay(60);

            TiposCultivoCollectionView.ScrollTo(
                0,
                position:
                    ScrollToPosition.Start,
                animate:
                    false);
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

            if (string.IsNullOrWhiteSpace(
                    rutaDestino))
            {
                return;
            }

            /*
             * El formulario pertenece a la misma visita. El modal de registros
             * eliminados no usa navegación Shell, por lo que tampoco finaliza
             * la visita.
             */
            salidaExternaPendiente =
                !EsRutaInternaTiposCultivo(
                    rutaDestino);
        }

        private static bool
            EsRutaInternaTiposCultivo(
                string ruta)
        {
            return
                ruta.Contains(
                    "TipoCultivoPage",
                    StringComparison.OrdinalIgnoreCase) ||
                ruta.Contains(
                    "TipoCultivoFormPage",
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
