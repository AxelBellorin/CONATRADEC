using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class MultiCalculoPage : ContentPage
    {
        private readonly MultiCalculoViewModel viewModel =
            new MultiCalculoViewModel();

        public MultiCalculoPage()
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
                await GlobalService.MostrarToastAsync(
                    "No tiene permisos para ver los cálculos complementarios.");

                await Shell.Current.GoToAsync("//MainPage");
                return;
            }

            await RestaurarCalculosEdicionUiService.Instance
                .RestaurarAsync(viewModel);
        }
    }
}
