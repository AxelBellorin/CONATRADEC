using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class TerrenoBusquedaIAPage : ContentPage
    {
        private readonly TerrenoBusquedaIAViewModel viewModel;

        public TerrenoBusquedaIAPage()
        {
            InitializeComponent();
            viewModel = new TerrenoBusquedaIAViewModel();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.InicializarAsync();
        }
    }
}
