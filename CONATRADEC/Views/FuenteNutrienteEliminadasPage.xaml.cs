using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class FuenteNutrienteEliminadasPage : ContentPage
    {
        private readonly FuenteNutrienteEliminadasViewModel viewModel = new();
        private int cantidadColumnasActual;

        public FuenteNutrienteEliminadasPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            AjustarCantidadColumnas(Width);
            await viewModel.InicializarAsync();
        }

        /// <summary>
        /// Esta pantalla se abre como una ventana modal.
        /// En Android el botón físico y el gesto de retroceso se consumen
        /// para que únicamente los botones de la aplicación puedan cerrarla.
        /// </summary>
        protected override bool OnBackButtonPressed()
        {
#if ANDROID
            return true;
#else
            return base.OnBackButtonPressed();
#endif
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarCantidadColumnas(width);
        }

        private void AjustarCantidadColumnas(double width)
        {
            if (width <= 0 || FuentesEliminadasGrid == null)
                return;

            int nuevasColumnas =
                width >= 1180
                    ? 3
                    : width >= 720
                        ? 2
                        : 1;

            if (cantidadColumnasActual == nuevasColumnas)
                return;

            cantidadColumnasActual = nuevasColumnas;
            FuentesEliminadasGrid.Span = nuevasColumnas;
        }
    }
}
