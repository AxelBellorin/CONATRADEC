using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(Item), "Item")]
    public partial class rangoNutrienteCategoriaFormPage :
        ContentPage
    {
        private readonly RangoNutrienteCategoriaFormViewModel
            viewModel = new();

        public FormModeSelect Mode
        {
            set => viewModel.Mode = value;
        }

        public TipoCultivoRequest Item
        {
            set => viewModel.Item =
                value ?? new TipoCultivoRequest();
        }

        public rangoNutrienteCategoriaFormPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions(
                "rangoNutrientePage");

            bool denied =
                !viewModel.CanView ||
                (viewModel.Mode == FormModeSelect.Create &&
                 !viewModel.CanAdd) ||
                (viewModel.Mode == FormModeSelect.Edit &&
                 !viewModel.CanEdit);

            if (denied)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para administrar tipos de cultivo.",
                    "Aceptar");

                await Shell.Current.GoToAsync(
                    AppRoutes.RangosNutrientes);

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

        private void AjustarAnchoFormulario(double anchoPagina)
        {
            if (FormularioContainer == null ||
                anchoPagina <= 0)
            {
                return;
            }

            double margenHorizontal =
                DeviceInfo.Platform == DevicePlatform.WinUI
                    ? 72
                    : 32;

            double anchoDisponible =
                Math.Max(
                    280,
                    anchoPagina - margenHorizontal);

            FormularioContainer.WidthRequest =
                Math.Min(anchoDisponible, 1100);
        }
    }
}
