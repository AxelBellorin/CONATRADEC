using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class DiagnosticoIAAnalizadorPage : ContentPage
    {
        private readonly DiagnosticoIAAnalizadorViewModel viewModel;

        public DiagnosticoIAAnalizadorPage()
        {
            InitializeComponent();
            viewModel = new DiagnosticoIAAnalizadorViewModel();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.InicializarAsync();
        }
    }
}
