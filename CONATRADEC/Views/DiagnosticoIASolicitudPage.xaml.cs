using CONATRADEC.Models;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class DiagnosticoIASolicitudPage :
        ContentPage,
        IQueryAttributable
    {
        private readonly DiagnosticoIASolicitudViewModel viewModel;

        public DiagnosticoIASolicitudPage()
        {
            InitializeComponent();
            viewModel = new DiagnosticoIASolicitudViewModel();
            BindingContext = viewModel;
        }

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            if (query.TryGetValue("modo", out object? modo))
            {
                viewModel.AplicarModo(modo?.ToString());
                query.Remove("modo");
            }

            if (query.TryGetValue(
                    "TerrenoSeleccionado",
                    out object? valor) &&
                valor is TerrenoBusquedaIAItem terreno)
            {
                viewModel.AplicarTerrenoSeleccionado(terreno);
                query.Remove("TerrenoSeleccionado");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.InicializarAsync();
        }
    }
}
