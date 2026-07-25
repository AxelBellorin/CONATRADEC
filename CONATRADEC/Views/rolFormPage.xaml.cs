using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(Rol), "Rol")]
    public partial class rolFormPage : ContentPage
    {
        private readonly RolFormViewModel viewModel = new();

        public FormModeSelect Mode
        {
            set => viewModel.Mode = value;
        }

        public RolRequest Rol
        {
            set => viewModel.Rol =
                value ?? new RolRequest();
        }

        public rolFormPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("rolPage");

            bool denegado =
                !viewModel.CanView ||
                (viewModel.Mode == FormModeSelect.Create &&
                 !viewModel.CanAdd) ||
                (viewModel.Mode == FormModeSelect.Edit &&
                 !viewModel.CanEdit);

            if (denegado)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para realizar esta operación.",
                    "Aceptar");

                await Shell.Current.GoToAsync(AppRoutes.Roles);
                return;
            }

            AjustarAnchoFormulario(Width);
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarAnchoFormulario(width);
        }

        private void AjustarAnchoFormulario(double ancho)
        {
            if (FormularioContainer == null ||
                ancho <= 0)
            {
                return;
            }

            double margen =
                DeviceInfo.Platform == DevicePlatform.WinUI
                    ? 72
                    : 32;

            FormularioContainer.WidthRequest =
                Math.Min(
                    Math.Max(280, ancho - margen),
                    1100);
        }
    }
}
