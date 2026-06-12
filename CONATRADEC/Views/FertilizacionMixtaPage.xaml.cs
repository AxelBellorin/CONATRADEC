using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class FertilizacionMixtaPage : ContentPage
    {
        private readonly FertilizacionMixtaViewModel viewModel = new FertilizacionMixtaViewModel();

        public FertilizacionMixtaPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("FertilizacionMixtaPage");

            if (!viewModel.CanView)
            {
                await GlobalService.MostrarToastAsync("No tiene permisos para ver fertilización mixta.");
                await Shell.Current.GoToAsync("//MainPage");
                return;
            }
        }
    }
}