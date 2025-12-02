using CONATRADEC;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class elementoQuimicoPage : ContentPage
    {
        private readonly ElementoQuimicoViewModel viewModel = new();

        public elementoQuimicoPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Carga permisos para esta página
            viewModel.LoadPagePermissions("elementoQuimicoPage");

            if (!viewModel.CanView)
            {
                await GlobalService.MostrarToastAsync("No tiene permisos para ver elementos químicos.");
                await Shell.Current.GoToAsync("//MainPage");
                return;
            }

            await viewModel.LoadElementoQuimico(true);
        }
    }
}
