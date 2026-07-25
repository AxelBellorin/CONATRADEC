using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(Item), "Item")]
    public partial class tipoCultivoFormPage : ContentPage
    {
        private readonly TipoCultivoFormViewModel
            viewModel = new();

        public tipoCultivoFormPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext =
                viewModel;
        }

        public FormModeSelect Mode
        {
            set =>
                viewModel.Mode =
                    value;
        }

        public TipoCultivoRequest Item
        {
            set =>
                viewModel.Item =
                    value ??
                    new TipoCultivoRequest();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();
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
            base.OnSizeAllocated(
                width,
                height);

            AjustarAnchoFormulario(
                width);
        }

        private void AjustarAnchoFormulario(
            double anchoPagina)
        {
            if (FormularioContainer == null ||
                anchoPagina <= 0)
            {
                return;
            }

            double margenHorizontal =
                DeviceInfo.Platform ==
                DevicePlatform.WinUI
                    ? 72
                    : 32;

            double anchoDisponible =
                Math.Max(
                    280,
                    anchoPagina -
                    margenHorizontal);

            FormularioContainer.WidthRequest =
                Math.Min(
                    anchoDisponible,
                    1100);
        }
    }
}
