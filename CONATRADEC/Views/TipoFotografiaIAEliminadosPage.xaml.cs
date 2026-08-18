using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class TipoFotografiaIAEliminadosPage : ContentPage
    {
        private readonly TipoFotografiaIAEliminadosViewModel viewModel = new();

        public TipoFotografiaIAEliminadosPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.InicializarAsync();
        }
    }
}
