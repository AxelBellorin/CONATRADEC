using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Pais), "Pais")]
    [QueryProperty(nameof(Departamento), "Departamento")]
    [QueryProperty(nameof(TitlePage), "TitlePage")]
    public partial class municipioPage : ContentPage
    {
        private readonly MunicipioViewModel viewModel = new();
        private string paisNombre;

        public string TitlePage
        {
            set => viewModel.TitlePage = value;
        }

        public PaisRequest Pais
        {
            set 
            { 
                viewModel.PaisRequest = value;
                paisNombre = value.NombrePais;
            }
        }

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

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (!PermissionService.Instance.HasRead("municipioPage"))
            {
                _ = GlobalService.MostrarToastAsync("No tiene permisos para ver municipios.");

                Shell.Current.GoToAsync("//MainPage");
                return;
            }

            viewModel.LoadPagePermissions("municipioPage");
        }
    }
}
