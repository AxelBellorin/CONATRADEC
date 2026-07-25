using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class rolPage : ContentPage
    {
        private readonly RolViewModel viewModel = new();
        private int columnasActuales;

        public rolPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();
            AjustarColumnas(Width);

            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarColumnas(width);
        }

        private void AjustarColumnas(double width)
        {
            if (width <= 0 ||
                RolesGridLayout == null)
            {
                return;
            }

            int columnas =
                width >= 1280
                    ? 3
                    : width >= 760
                        ? 2
                        : 1;

            if (columnasActuales == columnas)
                return;

            columnasActuales = columnas;
            RolesGridLayout.Span = columnas;
        }
    }
}
