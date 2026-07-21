using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class bitacoraPage : ContentPage
    {
        private readonly BitacoraViewModel viewModel = new();
        private bool inicializada;

        public bitacoraPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (inicializada)
                return;

            inicializada = true;
            await viewModel.InicializarAsync();
        }
    }
}
