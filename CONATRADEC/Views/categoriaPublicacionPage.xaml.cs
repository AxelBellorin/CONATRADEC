using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class categoriaPublicacionPage : ContentPage
    {
        private const string ClaveVisita =
            InterfazCodigos.CategoriasPublicacion;

        private const double AnchoMinimoTarjeta = 330;
        private const double EspaciadoTarjetas = 14;

        private readonly CategoriaPublicacionViewModel
            viewModel = new();

        private int cantidadColumnasActual;
        private bool navegacionShellSuscrita;
        private bool salidaExternaPendiente;

        public categoriaPublicacionPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;

            CategoriasCollectionView.SizeChanged +=
                OnListadoSizeChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            salidaExternaPendiente = false;
            SuscribirNavegacionShell();

            viewModel.ActualizarPermisos();
            AjustarDiseno(Width);

            ContenidoPrincipal.IsVisible =
                viewModel.CanView;

            ContenidoSinPermiso.IsVisible =
                !viewModel.CanView;

            if (!viewModel.CanView)
            {
                InterfazVisitaCacheService.FinalizarVisita(
                    ClaveVisita);

                viewModel.FinalizarVisita();
                return;
            }

            bool nuevaVisita =
                InterfazVisitaCacheService.AsegurarVisita(
                    ClaveVisita);

            if (nuevaVisita)
            {
                await viewModel.IniciarNuevaVisitaAsync();
                return;
            }

            /*
             * Crear, Editar y Eliminados pertenecen a la misma visita.
             * Al regresar se conserva la búsqueda aplicada y el listado solo
             * se renueva si una operación confirmada cambió el catálogo.
             */
            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            if (salidaExternaPendiente)
            {
                InterfazVisitaCacheService.FinalizarVisita(
                    ClaveVisita);

                viewModel.FinalizarVisita();
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

        private void OnListadoSizeChanged(
            object? sender,
            EventArgs e)
        {
            AjustarDiseno(Width);
        }

        private void AjustarDiseno(double width)
        {
            if (width <= 0)
                return;

            AjustarMargenContenido(width);
            AjustarCantidadColumnas(width);
        }

        private void AjustarMargenContenido(double width)
        {
            if (CategoriasCollectionView == null)
                return;

            CategoriasCollectionView.Margin =
                width < 600
                    ? new Thickness(12, 12, 12, 20)
                    : width < 900
                        ? new Thickness(18, 16, 18, 24)
                        : new Thickness(24, 20, 24, 28);
        }

        private void AjustarCantidadColumnas(double width)
        {
            if (width <= 0 ||
                CategoriasGridLayout == null)
            {
                return;
            }

            double anchoUtil =
                ObtenerAnchoUtilListado(width);

            int nuevasColumnas =
                CalcularColumnas(anchoUtil);

            if (cantidadColumnasActual == nuevasColumnas &&
                CategoriasGridLayout.Span == nuevasColumnas)
            {
                return;
            }

            cantidadColumnasActual = nuevasColumnas;
            CategoriasGridLayout.Span = nuevasColumnas;
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
            if (CategoriasCollectionView?.Width > 0)
                return CategoriasCollectionView.Width;

            double margenHorizontal =
                width < 600
                    ? 24
                    : width < 900
                        ? 36
                        : 48;

            return Math.Max(
                0,
                width - margenHorizontal);
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

            salidaExternaPendiente =
                !EsRutaInternaCategoriaPublicacion(
                    rutaDestino);
        }

        private static bool EsRutaInternaCategoriaPublicacion(
            string ruta) =>
            ruta.Contains(
                nameof(categoriaPublicacionPage),
                StringComparison.OrdinalIgnoreCase) ||
            ruta.Contains(
                nameof(categoriaPublicacionFormPage),
                StringComparison.OrdinalIgnoreCase) ||
            ruta.Contains(
                nameof(CatalogoEliminadosPage),
                StringComparison.OrdinalIgnoreCase);
    }
}
