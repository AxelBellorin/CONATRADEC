using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class matrizPermisosPage : ContentPage
    {
        private readonly MatrizPermisosViewModel viewModel = new();

        private bool modoCompactoActual;
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

            AjustarDistribucion(
                Width,
                Height);

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
            base.OnSizeAllocated(
                width,
                height);

            AjustarDistribucion(
                width,
                height);
        }

        /// <summary>
        /// Prioriza el listado de permisos cuando la ventana tiene poca
        /// altura. El encabezado y los filtros quedan disponibles mediante
        /// su propio desplazamiento vertical.
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

            bool modoCompacto =
                height < 760;

            bool anchoCompacto =
                width < 780;

            /*
             * En una ventana baja, el panel superior no consume más del
             * 34 % de la altura. En una ventana amplia puede crecer un poco
             * más, pero siempre deja la mayor parte para los permisos.
             */
            double porcentajeSuperior =
                modoCompacto
                    ? 0.34
                    : 0.42;

            double minimoSuperior =
                modoCompacto
                    ? 170
                    : 245;

            double maximoSuperior =
                modoCompacto
                    ? 245
                    : 380;

            PanelSuperiorScroll.MaximumHeightRequest =
                Math.Clamp(
                    height * porcentajeSuperior,
                    minimoSuperior,
                    maximoSuperior);

            PermisosList.MinimumHeightRequest =
                modoCompacto
                    ? 220
                    : 280;

            if (modoCompactoActual != modoCompacto)
            {
                modoCompactoActual =
                    modoCompacto;

                TituloMatrizLabel.FontSize =
                    modoCompacto
                        ? 23
                        : 30;

                SubtituloMatrizLabel.IsVisible =
                    !modoCompacto;

                EncabezadoGrid.ColumnSpacing =
                    modoCompacto
                        ? 8
                        : 16;

                BotonConfiguracion.Padding =
                    modoCompacto
                        ? new Thickness(11, 8)
                        : new Thickness(16, 10);

                BotonRefrescar.Padding =
                    modoCompacto
                        ? new Thickness(11, 8)
                        : new Thickness(16, 10);

                AccionesInferioresGrid.Padding =
                    modoCompacto
                        ? new Thickness(0, 1)
                        : new Thickness(0, 4);
            }

            if (anchoCompactoActual != anchoCompacto)
            {
                anchoCompactoActual =
                    anchoCompacto;

                BotonConfiguracion.Text =
                    anchoCompacto
                        ? "← Volver"
                        : "← Configuración";

                BotonRefrescar.Text =
                    anchoCompacto
                        ? "↻"
                        : "Refrescar";

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
            }
        }
    }
}
