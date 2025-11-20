using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class terrenoPage : ContentPage
    {
        private TerrenoViewModel viewModel = new TerrenoViewModel();

        public terrenoPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.LoadTerrenosAsync(true);
        }
    }
}
