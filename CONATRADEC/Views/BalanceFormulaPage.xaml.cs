using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class BalanceFormulaPage : ContentPage
    {
        private readonly BalanceFormulaViewModel viewModel = new BalanceFormulaViewModel();

        public BalanceFormulaPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("BalanceFormulaPage");

            if (!viewModel.CanView)
            {
                await GlobalService.MostrarToastAsync("No tiene permisos para ver balance de fórmula.");
                await Shell.Current.GoToAsync("//MainPage");
                return;
            }
        }
    }
}