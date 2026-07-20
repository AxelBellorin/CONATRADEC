using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(Item), "Item")]
    public partial class extraccionNutrienteFormPage : ContentPage
    {
        private readonly ExtraccionNutrienteFormViewModel viewModel = new();

        public FormModeSelect Mode { set => viewModel.Mode = value; }
        public ExtraccionNutrienteRequest Item { set => viewModel.Item = value; }

        public extraccionNutrienteFormPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            viewModel.LoadPagePermissions("extraccionNutrientePage");

            bool denied = !viewModel.CanView
                || (viewModel.Mode == FormModeSelect.Create && !viewModel.CanAdd)
                || (viewModel.Mode == FormModeSelect.Edit && !viewModel.CanEdit);

            if (denied)
            {
                await DisplayAlert("Permiso denegado", "No tiene permisos para realizar esta operación.", "Aceptar");
                await Shell.Current.GoToAsync(AppRoutes.ExtraccionNutrientes);
                return;
            }

            await viewModel.InitializeAsync();
        }
    }
}
