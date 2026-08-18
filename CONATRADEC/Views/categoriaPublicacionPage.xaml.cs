using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class categoriaPublicacionPage : ContentPage
    {
        private const string ClaveVisita =
            InterfazCodigos.CategoriasPublicacion;

        private readonly CategoriaPublicacionViewModel
            viewModel = new();

        private bool navegacionShellSuscrita;
        private bool salidaExternaPendiente;

        public categoriaPublicacionPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            salidaExternaPendiente = false;
            SuscribirNavegacionShell();

            viewModel.ActualizarPermisos();
            ContenidoPrincipal.IsVisible = viewModel.CanView;
            ContenidoSinPermiso.IsVisible = !viewModel.CanView;

            if (!viewModel.CanView)
            {
                InterfazVisitaCacheService.FinalizarVisita(
                    ClaveVisita);
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
             * Nuevo y Editar forman parte de la misma visita. Al regresar del
             * formulario se conservan búsqueda, filtro y resultados; solamente
             * se vuelve a consultar cuando una operación confirmada modificó el
             * catálogo mientras el formulario estuvo abierto.
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
            base.OnDisappearing();
        }

        private void SuscribirNavegacionShell()
        {
            if (navegacionShellSuscrita ||
                Shell.Current == null)
            {
                return;
            }

            Shell.Current.Navigating += Shell_Navigating;
            navegacionShellSuscrita = true;
        }

        private void DesuscribirNavegacionShell()
        {
            if (!navegacionShellSuscrita ||
                Shell.Current == null)
            {
                return;
            }

            Shell.Current.Navigating -= Shell_Navigating;
            navegacionShellSuscrita = false;
        }

        private void Shell_Navigating(
            object? sender,
            ShellNavigatingEventArgs e)
        {
            string rutaDestino =
                e.Target?.Location?.OriginalString ??
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
                StringComparison.OrdinalIgnoreCase);
    }
}
