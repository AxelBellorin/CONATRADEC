using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;

namespace CONATRADEC.Views
{
    public partial class rolFormPage : ContentPage
    {
        private readonly RolFormViewModel viewModel = new();

        public rolFormPage()
        {
            InitializeComponent();
            BindingContext = viewModel;

            Shell.SetNavBarIsVisible(
                this,
                false);

            Shell.SetBackButtonBehavior(
                this,
                new BackButtonBehavior
                {
                    IsVisible = false,
                    IsEnabled = false
                });

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            Shell.SetNavBarIsVisible(
                this,
                false);

            viewModel.ActualizarPermisos();

            if (!await viewModel.ValidarNavegacionAsync())
                return;

            bool denegado =
                !viewModel.CanView ||
                (viewModel.Mode == FormMode.FormModeSelect.Create &&
                 !viewModel.CanAdd) ||
                (viewModel.Mode == FormMode.FormModeSelect.Edit &&
                 !viewModel.CanEdit);

            if (denegado)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para realizar esta operación.",
                    "Aceptar");

                if (viewModel.CancelCommand.CanExecute(null))
                    viewModel.CancelCommand.Execute(null);

                return;
            }

            AjustarAnchoFormulario(Width);
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarOperaciones();
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarAnchoFormulario(width);
        }

        protected override bool OnBackButtonPressed()
        {
            if (viewModel.CancelCommand.CanExecute(null))
                viewModel.CancelCommand.Execute(null);

            return true;
        }

        private void AjustarAnchoFormulario(double ancho)
        {
            if (FormularioContainer == null ||
                ancho <= 0)
            {
                return;
            }

            double margen =
                ancho < 600
                    ? 24
                    : ancho < 900
                        ? 40
                        : DeviceInfo.Current.Platform == DevicePlatform.WinUI
                            ? 72
                            : 56;

            FormularioContainer.WidthRequest =
                Math.Min(
                    Math.Max(280, ancho - margen),
                    900);
        }
    }
}
