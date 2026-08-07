using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class MotivoDevolucionTecnicoFormPage :
        ContentPage,
        IQueryAttributable
    {
        private readonly MotivoDevolucionTecnicoFormViewModel viewModel = new();

        public MotivoDevolucionTecnicoFormPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            int id = 0;
            if (query.TryGetValue("id", out object? valor))
                int.TryParse(valor?.ToString(), out id);
            viewModel.AplicarId(id);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.InicializarAsync();
        }
    }
}
