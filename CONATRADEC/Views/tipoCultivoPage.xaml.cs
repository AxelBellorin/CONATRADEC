using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class tipoCultivoPage : ContentPage
    {
        private readonly TipoCultivoViewModel viewModel = new();

        public tipoCultivoPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            viewModel.LoadPagePermissions("tipoCultivoPage");

            if (!viewModel.CanView)
            {
                await DisplayAlert("Permiso denegado", "No tiene permisos para ver tipos de cultivo.", "Aceptar");
                await Shell.Current.GoToAsync(AppRoutes.Principal);
                return;
            }

            await viewModel.LoadAsync(true);
        }
    }
}
