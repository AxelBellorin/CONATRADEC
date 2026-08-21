using CONATRADEC.Models;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class DiagnosticoIAAgregarFotografiasPage :
        ContentPage,
        IQueryAttributable
    {
        private readonly DiagnosticoIAAgregarFotografiasViewModel viewModel;

        public DiagnosticoIAAgregarFotografiasPage()
        {
            InitializeComponent();
            viewModel = new DiagnosticoIAAgregarFotografiasViewModel();
            BindingContext = viewModel;
        }

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            int inspeccionId = 0;

            if (query.TryGetValue("inspeccionId", out object? idValor))
                int.TryParse(idValor?.ToString(), out inspeccionId);

            IEnumerable<InspeccionFotoPreparacionLocal> fotografias = [];

            if (query.TryGetValue("fotografias", out object? fotosValor) &&
                fotosValor is IEnumerable<InspeccionFotoPreparacionLocal> fotos)
            {
                fotografias = fotos;
            }

            viewModel.AplicarParametros(
                inspeccionId,
                fotografias);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            /*
             * Esta página no abre subflujos internos. Si desaparece por volver,
             * guardar o navegar a otro módulo, libera cualquier archivo temporal
             * que todavía no haya sido incorporado al expediente.
             */
            viewModel.LiberarTemporalesSiCorresponde();
            base.OnDisappearing();
        }

        private async void OnVolverClicked(
            object? sender,
            EventArgs e)
        {
            if (viewModel.IsBusy || Shell.Current == null)
                return;

            bool confirmar = await DisplayAlert(
                "Cancelar preparación",
                "Las fotografías seleccionadas todavía no se han agregado a la inspección. ¿Desea regresar?",
                "Regresar",
                "Permanecer");

            if (!confirmar)
                return;

            viewModel.LiberarTemporalesSiCorresponde();
            await Shell.Current.GoToAsync("..");
        }

        protected override bool OnBackButtonPressed()
        {
            Dispatcher.Dispatch(() =>
                OnVolverClicked(this, EventArgs.Empty));
            return true;
        }
    }
}
