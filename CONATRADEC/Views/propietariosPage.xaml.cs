using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class propietariosPage : ContentPage
    {
        private readonly PropietariosViewModel viewModel;

        public propietariosPage()
        {
            InitializeComponent();

            viewModel = new PropietariosViewModel();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.InicializarAsync();
        }
    }
}
