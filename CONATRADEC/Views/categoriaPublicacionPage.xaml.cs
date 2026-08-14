using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class categoriaPublicacionPage : ContentPage
    {
        private CategoriaPublicacionViewModel viewModel = new();
        private bool paginaMostrada;

        public categoriaPublicacionPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (paginaMostrada)
            {
                viewModel.CancelarCarga();
                viewModel = new CategoriaPublicacionViewModel();
                BindingContext = viewModel;
            }
            else
            {
                paginaMostrada = true;
            }

            viewModel.ActualizarPermisos();
            ContenidoPrincipal.IsVisible = viewModel.CanView;
            ContenidoSinPermiso.IsVisible = !viewModel.CanView;

            if (!viewModel.CanView)
                return;

            await Task.Yield();
            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }
    }
}
