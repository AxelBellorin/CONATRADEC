using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class DiagnosticoIAAprobadorPage : ContentPage
    {
        private readonly DiagnosticoIAAprobadorViewModel viewModel;

        public DiagnosticoIAAprobadorPage()
        {
            InitializeComponent();
            viewModel = new DiagnosticoIAAprobadorViewModel();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.InicializarAsync();
        }
    }
}
