using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class terrenoPage : ContentPage
    {
        private readonly TerrenoViewModel viewModel = new();

        public terrenoPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Carga permisos
            viewModel.LoadPagePermissions("terrenoPage");

            if (!viewModel.CanView)
            {
                await App.Current.MainPage.DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para ver los terrenos.",
                    "Aceptar");

                await Shell.Current.GoToAsync("//MainPage");
                return;
            }

            await viewModel.LoadTerrenosAsync(true);
        }
    }
}
