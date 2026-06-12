using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class EnmiendaCalcareaPage : ContentPage
    {
        private readonly EnmiendaCalcareaViewModel viewModel = new EnmiendaCalcareaViewModel();

        public EnmiendaCalcareaPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("EnmiendaCalcareaPage");

            if (!viewModel.CanView)
            {
                await GlobalService.MostrarToastAsync("No tiene permisos para ver enmienda calcárea.");
                await Shell.Current.GoToAsync("//MainPage");
                return;
            }
        }
    }
}