using CONATRADEC.Models;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(Pais), "Pais")]
    [QueryProperty(nameof(Departamento), "Departamento")]
    public partial class departamentoFormPage : ContentPage
    {
        private readonly DepartamentoFormViewModel
            viewModel = new();

        public departamentoFormPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;
        }

        public FormMode.FormModeSelect Mode
        {
            set =>
                viewModel.Mode =
                    value;
        }

        public PaisRequest Pais
        {
            set =>
                viewModel.PaisRequest =
                    value ??
                    new PaisRequest();
        }

        public DepartamentoRequest Departamento
        {
            set =>
                viewModel.Departamento =
                    value ??
                    new DepartamentoRequest();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            viewModel.CancelarOperaciones();
        }
    }
}
