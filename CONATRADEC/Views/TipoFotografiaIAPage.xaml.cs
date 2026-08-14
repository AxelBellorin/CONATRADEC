using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class TipoFotografiaIAPage : ContentPage
    {
        private TipoFotografiaIAViewModel viewModel = new();
        private bool paginaMostrada;

        public TipoFotografiaIAPage()
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
                viewModel = new TipoFotografiaIAViewModel();
                BindingContext = viewModel;
            }
            else
            {
                paginaMostrada = true;
            }

            viewModel.ActualizarPermisos();
            await viewModel.InicializarAsync();
        }
    }
}
