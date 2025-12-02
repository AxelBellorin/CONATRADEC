using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class userPage : ContentPage
    {
        private readonly UserViewModel viewModel = new UserViewModel();

        public userPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Cargar permisos
            viewModel.LoadPagePermissions("userPage");

            if (!viewModel.CanView)
            {
                await GlobalService.MostrarToastAsync("No tiene permisos para ver usuarios.");
                await Shell.Current.GoToAsync("//MainPage");
                return;
            }

            await viewModel.LoadUsers(true);
        }
    }
}
