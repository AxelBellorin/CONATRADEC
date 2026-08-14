using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class matrizPermisosPage : ContentPage
    {
        private readonly MatrizPermisosViewModel viewModel = new();

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
        /// La matriz utiliza un único desplazamiento vertical mediante
        /// PermisosList. El encabezado, selector, buscador y acciones
        /// masivas forman parte del Header del CollectionView.
        ///
        /// Aquí solo se ajustan las acciones inferiores para conservar
        /// una presentación compacta en teléfono y ventanas estrechas.
        /// </summary>
        private void AjustarDistribucion(
            double width,
            double height)
        {
            if (width <= 0 ||
                height <= 0 ||
                PermisosList == null ||
                AccionesInferioresGrid == null ||
                BotonGuardar == null ||
                BotonRevertir == null)
            {
                return;
            }

            bool telefono = width < 600;
            bool alturaCompacta = height < 760;
            bool anchoCompacto = width < 720;

            /*
             * La lista ocupa la fila flexible de la pantalla. El mínimo evita
             * que quede inutilizable en ventanas muy bajas sin crear un segundo
             * ScrollView vertical.
             */
            PermisosList.MinimumHeightRequest =
                telefono
                    ? 220
                    : alturaCompacta
                        ? 230
                        : 290;

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
