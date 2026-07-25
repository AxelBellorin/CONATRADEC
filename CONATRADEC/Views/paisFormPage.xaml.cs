using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(Pais), "Pais")]
    public partial class paisFormPage : ContentPage
    {
        private readonly PaisFormViewModel viewModel = new();

        public paisFormPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;
        }

        public FormModeSelect Mode
        {
            set => viewModel.Mode = value;
        }

        public PaisRequest Pais
        {
            set => viewModel.Pais = value;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            viewModel.CancelarOperaciones();
        }
    }
}
