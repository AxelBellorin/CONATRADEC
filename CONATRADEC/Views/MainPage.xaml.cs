using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Controls;
using System;

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

            /*
             * La primera consulta sigue siendo manual. Si el usuario ya
             * listó los análisis y vuelve después de guardar una edición,
             * se actualizan las tarjetas automáticamente.
             */
            bool debeActualizar =
                viewModel.SeHaListado ||
                AnalisisListadoEstadoService
                    .HayActualizacionPendiente;

            if (debeActualizar &&
                !viewModel.IsBusy)
            {
                await viewModel.CargarAnalisisAsync(false);

                if (viewModel.SeHaListado)
                {
                    AnalisisListadoEstadoService
                        .ConfirmarActualizacion();
                }
            }
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
