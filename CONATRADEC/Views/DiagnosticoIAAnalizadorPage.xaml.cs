using CONATRADEC.Models;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class DiagnosticoIAAnalizadorPage : ContentPage
    {
        private DiagnosticoIAAnalizadorViewModel viewModel;
        private bool selectorTecnicoAbierto;
        private bool paginaMostrada;

        public DiagnosticoIAAnalizadorPage()
        {
            InitializeComponent();
            viewModel = new DiagnosticoIAAnalizadorViewModel();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            /*
             * El resultado se abre sobre esta bandeja. Al volver se crea una
             * bandeja nueva para consultar asignaciones y estados actuales sin
             * conservar técnico, pestaña o paginación de la visita anterior.
             */
            if (paginaMostrada)
            {
                viewModel = new DiagnosticoIAAnalizadorViewModel();
                BindingContext = viewModel;
                selectorTecnicoAbierto = false;
            }
            else
            {
                paginaMostrada = true;
            }

            await viewModel.InicializarAsync();
        }

        private async void OnSeleccionarTecnicoClicked(
            object? sender,
            EventArgs e)
        {
            if (selectorTecnicoAbierto || viewModel.IsBusy ||
                viewModel.TecnicosFiltro.Count == 0)
            {
                return;
            }

            selectorTecnicoAbierto = true;
            try
            {
                string[] opciones = viewModel.TecnicosFiltro
                    .Select(item => item.TextoMostrar)
                    .ToArray();

                string? seleccion = await DisplayActionSheet(
                    "Técnico responsable",
                    "Cancelar",
                    null,
                    opciones);

                if (string.IsNullOrWhiteSpace(seleccion) ||
                    string.Equals(
                        seleccion,
                        "Cancelar",
                        StringComparison.Ordinal))
                {
                    return;
                }

                TecnicoInspeccionFiltroItem? tecnico =
                    viewModel.TecnicosFiltro.FirstOrDefault(item =>
                        string.Equals(
                            item.TextoMostrar,
                            seleccion,
                            StringComparison.Ordinal));

                if (tecnico != null)
                    viewModel.TecnicoSeleccionado = tecnico;
            }
            finally
            {
                selectorTecnicoAbierto = false;
            }
        }
    }
}
