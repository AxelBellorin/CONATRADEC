using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class AnalisisGuardadoDetallePage : ContentPage
    {
        private readonly AnalisisGuardadoDetalleViewModel viewModel = new();

        public AnalisisGuardadoDetallePage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("MainPage");

            if (!viewModel.CanView)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para visualizar análisis.",
                    "Aceptar");

                await Shell.Current.GoToAsync("..");
            }
        }
    }
}
