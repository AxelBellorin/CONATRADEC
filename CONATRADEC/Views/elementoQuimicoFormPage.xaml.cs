using CONATRADEC.Models;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(ElementoQuimico), "ElementoQuimico")]
    public partial class elementoQuimicoFormPage : ContentPage
    {
        private readonly ElementoQuimicoFormViewModel
            viewModel = new();

        public elementoQuimicoFormPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;
        }

        public FormMode.FormModeSelect Mode
        {
            set =>
                viewModel.Mode = value;
        }

        public ElementoQuimicoRequest ElementoQuimico
        {
            set =>
                viewModel.ElementoQuimico =
                    value ??
                    new ElementoQuimicoRequest();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            viewModel.ActualizarPermisos();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarOperaciones();
            base.OnDisappearing();
        }
    }
}
