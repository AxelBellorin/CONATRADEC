using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(RegistroId), "RegistroId")]
    [QueryProperty(nameof(CategoriaId), "CategoriaId")]
    public partial class albumRegistroFormPage :
        ContentPage
    {
        private readonly AlbumRegistroFormViewModel
            viewModel = new();

        public FormModeSelect Mode
        {
            set => viewModel.Mode = value;
        }

        public int RegistroId
        {
            set => viewModel.RegistroId = value;
        }

        public int CategoriaId
        {
            set => viewModel.CategoriaInicialId = value;
        }

        public albumRegistroFormPage()
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
                    viewModel.Mode ==
                    FormModeSelect.Create &&
                    !viewModel.CanAdd
                ) ||
                (
                    viewModel.Mode ==
                    FormModeSelect.Edit &&
                    !viewModel.CanEdit
                );

            if (denied)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para realizar esta operación.",
                    "Aceptar");

                await Shell.Current.GoToAsync(
                    AppRoutes.Regresar,
                    false);

                return;
            }

            await viewModel.InicializarAsync();
        }
    }
}
