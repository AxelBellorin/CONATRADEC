using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(Item), "Item")]
    public partial class tipoCultivoFormPage : ContentPage
    {
        private readonly TipoCultivoFormViewModel viewModel = new();

        public FormModeSelect Mode { set => viewModel.Mode = value; }
        public TipoCultivoRequest Item { set => viewModel.Item = value; }

        public tipoCultivoFormPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            viewModel.LoadPagePermissions("tipoCultivoPage");

            bool denied = !viewModel.CanView
                || (viewModel.Mode == FormModeSelect.Create && !viewModel.CanAdd)
                || (viewModel.Mode == FormModeSelect.Edit && !viewModel.CanEdit);

            if (denied)
            {
                await DisplayAlert("Permiso denegado", "No tiene permisos para realizar esta operación.", "Aceptar");
                await Shell.Current.GoToAsync(AppRoutes.TiposCultivo);
                return;
            }


        }
    }
}
