using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class DiagnosticoIAConfiguracionPage : ContentPage
    {
        private readonly DiagnosticoIAConfiguracionViewModel viewModel;

        public DiagnosticoIAConfiguracionPage()
        {
            InitializeComponent();
            viewModel = new DiagnosticoIAConfiguracionViewModel();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.InicializarAsync();
        }
    }
}
