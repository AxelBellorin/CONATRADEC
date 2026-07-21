using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(RegistroId), "RegistroId")]
    public partial class albumFotosAdminPage : ContentPage
    {
        private readonly AlbumFotosAdminViewModel
            viewModel = new();

        public int RegistroId
        {
            set => viewModel.Id = value;
        }

        public albumFotosAdminPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();

            bool denied =
                !viewModel.CanView ||
                (
                    !viewModel.CanAdd &&
                    !viewModel.CanEdit &&
                    !viewModel.CanDelete
                );

            if (denied)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para administrar fotografías.",
                    "Aceptar");

                await Shell.Current.GoToAsync(
                    AppRoutes.AlbumFotos);

                return;
            }

            await viewModel.LoadAsync(true);
        }
    }
}
