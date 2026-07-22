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

        private bool paginaVisible;
        private bool permisosCargados;

        public string TitlePage
        {
            set => viewModel.TitlePage = value;
        }

        public PaisRequest Pais
        {
            set
            {
                viewModel.PaisRequest =
                    value ?? new PaisRequest();

                // Las QueryProperty pueden llegar en distinto orden.
                // La carga inicia cuando País y Departamento sean válidos.
                if (paginaVisible && permisosCargados)
                    _ = IntentarCargarMunicipiosAsync(true);
            }
        }

        public DepartamentoRequest Departamento
        {
            set
            {
                viewModel.DepartamentoRequest =
                    value ?? new DepartamentoRequest();

                if (paginaVisible && permisosCargados)
                    _ = IntentarCargarMunicipiosAsync(true);
            }
        }

        public municipioPage()
        {
            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            paginaVisible = true;

            if (!PermissionService.Instance.HasRead(
                    "municipioPage"))
            {
                paginaVisible = false;

                await GlobalService.MostrarToastAsync(
                    "No tiene permisos para ver municipios.");

                await Shell.Current.GoToAsync("//MainPage");
                return;
            }

            viewModel.LoadPagePermissions(
                "municipioPage");

            permisosCargados = true;

            // Si los parámetros ya llegaron, carga ahora.
            // Si aún falta alguno, su setter ejecutará la carga después.
            await IntentarCargarMunicipiosAsync(true);
        }

        protected override void OnDisappearing()
        {
            paginaVisible = false;
            base.OnDisappearing();
        }

        private async Task IntentarCargarMunicipiosAsync(
            bool mostrarIndicadorCarga)
        {
            int? departamentoId =
                viewModel.DepartamentoRequest.DepartamentoId;

            if (!paginaVisible ||
                !permisosCargados ||
                viewModel.PaisRequest.PaisId <= 0 ||
                !departamentoId.HasValue ||
                departamentoId.Value <= 0)
            {
                return;
            }

            await viewModel.LoadMunicipio(
                mostrarIndicadorCarga);
        }
    }
}
