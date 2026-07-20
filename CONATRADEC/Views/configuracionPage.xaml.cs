using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class configuracionPage : ContentPage
    {
        private readonly ConfiguracionViewModel viewModel = new();

        public configuracionPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            viewModel.ActualizarVisibilidad();
        }
    }
}
