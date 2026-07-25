using CONATRADEC.Models;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(Pais), "Pais")]
    [QueryProperty(nameof(Departamento), "Departamento")]
    [QueryProperty(nameof(Municipio), "Municipio")]
    public partial class municipioFormPage : ContentPage
    {
        private readonly MunicipioFormViewModel viewModel = new();

        public municipioFormPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        public FormMode.FormModeSelect Mode
        {
            set => viewModel.Mode = value;
        }

        public PaisRequest Pais
        {
            set => viewModel.PaisRequest = value ?? new PaisRequest();
        }

        public DepartamentoRequest Departamento
        {
            set => viewModel.DepartamentoRequest =
                value ?? new DepartamentoRequest();
        }

        public MunicipioRequest Municipio
        {
            set => viewModel.MunicipioRequest =
                value ?? new MunicipioRequest();
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
