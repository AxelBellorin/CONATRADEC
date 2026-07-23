using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class publicacionesAdminPage : ContentPage
    {
        private readonly PublicacionesAdminViewModel viewModel = new();

        public publicacionesAdminPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();
            ContenidoPrincipal.IsVisible = viewModel.CanAdministrar;
            ContenidoSinPermiso.IsVisible = !viewModel.CanAdministrar;

            if (viewModel.CanAdministrar)
                await viewModel.InicializarAsync();
        }
    }
}
