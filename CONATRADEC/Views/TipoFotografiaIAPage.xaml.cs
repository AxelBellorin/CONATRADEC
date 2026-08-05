using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class TipoFotografiaIAPage : ContentPage
    {
        private readonly TipoFotografiaIAViewModel viewModel = new();

        public TipoFotografiaIAPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            viewModel.ActualizarPermisos();
            await viewModel.InicializarAsync();
        }
    }
}
