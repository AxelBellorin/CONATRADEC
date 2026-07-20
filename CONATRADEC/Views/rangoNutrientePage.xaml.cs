using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class rangoNutrientePage : ContentPage
    {
        private readonly RangoNutrienteViewModel viewModel = new();

        public rangoNutrientePage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            viewModel.LoadPagePermissions("rangoNutrientePage");

            if (!viewModel.CanView)
            {
                await DisplayAlert("Permiso denegado", "No tiene permisos para ver rangos nutricionales.", "Aceptar");
                await Shell.Current.GoToAsync(AppRoutes.Principal);
                return;
            }

            await viewModel.LoadAsync(true);
        }
    }
}
