using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class ResultadoAnalisisSueloPage : ContentPage
    {
        private readonly ResultadoAnalisisSueloViewModel viewModel = new ResultadoAnalisisSueloViewModel();

        public ResultadoAnalisisSueloPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("ResultadoAnalisisSueloPage");

            if (!viewModel.CanView)
            {
                await GlobalService.MostrarToastAsync("No tiene permisos para ver el resultado del análisis de suelo.");
                await Shell.Current.GoToAsync("//MainPage");
                return;
            }
        }
    }
}