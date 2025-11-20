using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class elementoQuimicoPage : ContentPage
    {
        private ElementoQuimicoViewModel viewModel = new ElementoQuimicoViewModel();

        public elementoQuimicoPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.LoadElementoQuimico(true); // Carga los elementos químicos desde la VM.
        }
    }
}
