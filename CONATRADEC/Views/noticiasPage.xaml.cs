using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class noticiasPage : ContentPage
    {
        private readonly NoticiasViewModel viewModel = new();

        public noticiasPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();
            ContenidoPrincipal.IsVisible = viewModel.CanView;
            ContenidoSinPermiso.IsVisible = !viewModel.CanView;

            if (!viewModel.CanView)
                return;

            // Muestra primero la estructura y el indicador de carga en Android.
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
