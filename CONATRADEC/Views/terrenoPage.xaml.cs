using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class terrenoPage : ContentPage
    {
        private readonly TerrenoViewModel viewModel = new();

        public terrenoPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();

            ContenidoPrincipal.IsVisible = viewModel.CanView;
            ContenidoSinPermiso.IsVisible = !viewModel.CanView;

            if (!viewModel.CanView)
                return;

            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            viewModel.CancelarCarga();
        }
    }
}
