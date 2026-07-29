using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class CatalogoEliminadosPage : ContentPage
    {
        private int spanActual = 1;

        public CatalogoEliminadosPage(
            CatalogoEliminadoConfiguracion configuracion)
        {
            InitializeComponent();

            BindingContext =
                new CatalogoEliminadosViewModel(
                    configuracion);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is
                CatalogoEliminadosViewModel viewModel)
            {
                await viewModel.InicializarAsync();
            }
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
            base.OnSizeAllocated(
                width,
                height);

            if (width <= 0)
                return;

            int nuevoSpan =
                width >= 1120
                    ? 3
                    : width >= 720
                        ? 2
                        : 1;

            if (spanActual ==
                nuevoSpan)
            {
                return;
            }

            spanActual =
                nuevoSpan;

            RegistrosGrid.Span =
                nuevoSpan;
        }
    }
}
