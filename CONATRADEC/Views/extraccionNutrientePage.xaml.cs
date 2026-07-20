using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class extraccionNutrientePage : ContentPage
    {
        private readonly ExtraccionNutrienteViewModel viewModel = new();

        public extraccionNutrientePage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            viewModel.LoadPagePermissions("extraccionNutrientePage");

            if (!viewModel.CanView)
            {
                await DisplayAlert("Permiso denegado", "No tiene permisos para ver parámetros de extracción.", "Aceptar");
                await Shell.Current.GoToAsync(AppRoutes.Principal);
                return;
            }

            await viewModel.LoadAsync(true);
        }
    }
}
