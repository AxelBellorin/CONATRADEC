using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(RegistroId), "RegistroId")]
    public partial class albumDetallePage : ContentPage
    {
        private readonly AlbumDetalleViewModel viewModel = new();

        public int RegistroId
        {
            set => viewModel.Id = value;
        }

        public albumDetallePage()
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

            if (!viewModel.CanView)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para consultar el álbum botánico.",
                    "Aceptar");

                await Shell.Current.GoToAsync(
                    AppRoutes.AlbumFotos);

                return;
            }

            await viewModel.LoadAsync(true);
        }
    }
}
