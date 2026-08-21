using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class DiagnosticoIAPrepararAnalisisPage :
        ContentPage,
        IQueryAttributable
    {
        private readonly DiagnosticoIAPrepararAnalisisViewModel viewModel;

        public DiagnosticoIAPrepararAnalisisPage()
        {
            InitializeComponent();
            viewModel = new DiagnosticoIAPrepararAnalisisViewModel();
            BindingContext = viewModel;
        }

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            int diagnosticoId = 0;

            if (query.TryGetValue("diagnosticoId", out object? idValor))
                int.TryParse(idValor?.ToString(), out diagnosticoId);

            IEnumerable<int> fotografiaIds = [];

            if (query.TryGetValue("fotografiaIds", out object? fotosValor))
            {
                fotografiaIds = fotosValor switch
                {
                    IEnumerable<int> ids => ids,
                    int id when id > 0 => [id],
                    _ => []
                };
            }

            viewModel.AplicarParametros(
                diagnosticoId,
                fotografiaIds);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.InicializarAsync();
        }

        private async void OnVolverClicked(
            object? sender,
            EventArgs e)
        {
            if (viewModel.IsBusy || Shell.Current == null)
                return;

            await Shell.Current.GoToAsync("..");
        }

        protected override bool OnBackButtonPressed()
        {
            if (viewModel.IsBusy)
                return true;

            Dispatcher.Dispatch(() =>
                OnVolverClicked(this, EventArgs.Empty));
            return true;
        }
    }
}
