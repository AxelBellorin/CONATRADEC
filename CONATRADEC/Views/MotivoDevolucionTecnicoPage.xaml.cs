using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class MotivoDevolucionTecnicoPage : ContentPage
    {
        private MotivoDevolucionTecnicoViewModel viewModel = new();
        private bool paginaMostrada;

        public MotivoDevolucionTecnicoPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (paginaMostrada)
            {
                viewModel = new MotivoDevolucionTecnicoViewModel();
                BindingContext = viewModel;
            }
            else
            {
                paginaMostrada = true;
            }

            await viewModel.InicializarAsync();
        }
    }
}
