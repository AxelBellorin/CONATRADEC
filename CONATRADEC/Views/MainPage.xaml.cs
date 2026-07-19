using CONATRADEC.ViewModels;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel viewModel = new();

        public MainPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("MainPage");

            if (!viewModel.CanView)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para ver la pantalla principal.",
                    "Aceptar");
                return;
            }

            // El listado ya no se carga automáticamente.
            // El usuario decide cuándo consultar mediante
            // el botón Listar análisis.
        }
    }
}
