using CONATRADEC.Models;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    // Recibe desde Shell los parámetros "Pais", "Departamento" y "TitlePage"

    [QueryProperty(nameof(Pais), "Pais")]   // Recibe el objeto Cargo (CargoRequest)
    [QueryProperty(nameof(Departamento), "Departamento")]
    [QueryProperty(nameof(TitlePage), "TitlePage")]
    public partial class municipioPage : ContentPage
    {
        private readonly MunicipioViewModel viewModel = new MunicipioViewModel();

        // Título dinámico de la página (si lo usás en el VM)
        public string TitlePage
        {
            set => viewModel.TitlePage = value;
        }
        public PaisRequest Pais
        {
            set => viewModel.PaisRequest = value;
        }

        // Recibe el Departamento seleccionado y dispara la carga de municipios
        public DepartamentoRequest Departamento
        {
            set
            {
                viewModel.DepartamentoRequest = value;
                _ = viewModel.LoadMunicipio(true);
            }
        }

        public municipioPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
            InitializeComponent();
        }
    }
}
