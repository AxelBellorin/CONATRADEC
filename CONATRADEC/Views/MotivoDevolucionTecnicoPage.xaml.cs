using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class MotivoDevolucionTecnicoPage : ContentPage
    {
        private readonly MotivoDevolucionTecnicoViewModel viewModel = new();

        public MotivoDevolucionTecnicoPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.InicializarAsync();
        }
    }
}
