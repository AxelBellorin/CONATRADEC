using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class EditarAnalisisGuardadoPage : ContentPage
    {
        private readonly EditarAnalisisGuardadoViewModel viewModel = new();

        public EditarAnalisisGuardadoPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("MainPage");

            if (!viewModel.CanEdit)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para editar análisis.",
                    "Aceptar");

                await Shell.Current.GoToAsync("..");
            }
        }
    }
}
