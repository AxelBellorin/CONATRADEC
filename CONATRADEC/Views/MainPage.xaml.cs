using CONATRADEC.ViewModels;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel viewModel = new();

        public MainPage()
        {
            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("MainPage");
            viewModel.PrepararPantalla();

            if (!viewModel.CanView)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para ver la pantalla principal.",
                    "Aceptar");

                return;
            }

            // El listado se mantiene bajo demanda.
            // No se realiza ninguna consulta al abrir MainPage.
        }

        private async void OnListarAnalisisClicked(
            object? sender,
            EventArgs e)
        {
            if (viewModel.IsBusy)
                return;

            await viewModel.CargarAnalisisAsync(true);
        }
    }
}