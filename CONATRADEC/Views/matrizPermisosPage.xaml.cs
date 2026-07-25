using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class matrizPermisosPage : ContentPage
    {
        private readonly MatrizPermisosViewModel viewModel = new();

        private bool anchoCompactoActual;

        public matrizPermisosPage()
        {
            InitializeComponent();

            BindingContext = viewModel;
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisosPagina();

            if (!viewModel.CanView)
                return;

            AjustarDistribucion(Width, Height);

            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarOperaciones();

            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);

            AjustarDistribucion(width, height);
        }

        /// <summary>
        /// La matriz prioriza el listado de permisos.
        ///
        /// El encabezado, selector, buscador y acciones masivas comparten
        /// un panel superior desplazable que cede espacio en teléfonos y
        /// ventanas con poca altura.
        /// </summary>
        private void AjustarDistribucion(
            double width,
            double height)
        {
            if (width <= 0 ||
                height <= 0 ||
                PanelSuperiorScroll == null)
            {
                return;
            }

            bool telefono = width < 600;
            bool alturaCompacta = height < 760;

            double porcentajeSuperior =
                telefono
                    ? 0.44
                    : alturaCompacta
                        ? 0.34
                        : 0.42;

            double minimoSuperior =
                telefono
                    ? 230
                    : alturaCompacta
                        ? 175
                        : 245;

            double maximoSuperior =
                telefono
                    ? 360
                    : alturaCompacta
                        ? 250
                        : 390;

            PanelSuperiorScroll.MaximumHeightRequest =
                Math.Clamp(
                    height * porcentajeSuperior,
                    minimoSuperior,
                    maximoSuperior);

            PermisosList.MinimumHeightRequest =
                telefono
                    ? 220
                    : alturaCompacta
                        ? 230
                        : 290;

            bool anchoCompacto = width < 720;

            if (anchoCompactoActual == anchoCompacto)
                return;

            anchoCompactoActual = anchoCompacto;

            BotonGuardar.Text =
                anchoCompacto
                    ? "Guardar"
                    : "Guardar cambios";

            BotonRevertir.Padding =
                anchoCompacto
                    ? new Thickness(11, 8)
                    : new Thickness(16, 10);

            BotonGuardar.Padding =
                anchoCompacto
                    ? new Thickness(12, 8)
                    : new Thickness(18, 10);

            AccionesInferioresGrid.Padding =
                alturaCompacta
                    ? new Thickness(0, 1)
                    : new Thickness(0, 4);
        }
    }
}
