using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    public partial class rangoNutrienteFormPage :
        ContentPage,
        IQueryAttributable
    {
        private readonly
            RangoNutrienteFormViewModel viewModel =
                new();

        private bool parametrosAplicados;

        public rangoNutrienteFormPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;
        }

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            if (!query.TryGetValue(
                    "Mode",
                    out object? modeObject) ||
                modeObject is not FormModeSelect mode)
            {
                return;
            }

            if (!query.TryGetValue(
                    "Categoria",
                    out object? categoriaObject) ||
                categoriaObject is not
                    RangoNutrienteCategoriaItem
                    tipoCultivo)
            {
                return;
            }

            if (!query.TryGetValue(
                    "Item",
                    out object? itemObject) ||
                itemObject is not
                    RangoNutrienteRequest item)
            {
                return;
            }

            viewModel.PrepararNavegacion(
                mode,
                tipoCultivo,
                item);

            parametrosAplicados = true;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions(
                "rangoNutrientePage");

            bool denied =
                !viewModel.CanView ||
                (viewModel.Mode ==
                    FormModeSelect.Create &&
                 !viewModel.CanAdd) ||
                (viewModel.Mode ==
                    FormModeSelect.Edit &&
                 !viewModel.CanEdit);

            if (denied)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para realizar esta operación.",
                    "Aceptar");

                await Shell.Current.GoToAsync(
                    AppRoutes.RangosNutrientes);

                return;
            }

            for (int intento = 0;
                 intento < 20 &&
                 (!parametrosAplicados ||
                  !viewModel.TieneTipoCultivoValido);
                 intento++)
            {
                await Task.Delay(25);
            }

            if (!parametrosAplicados ||
                !viewModel.TieneTipoCultivoValido)
            {
                await DisplayAlert(
                    "Tipo de cultivo no válido",
                    "No fue posible identificar el tipo de cultivo seleccionado.",
                    "Aceptar");

                await Shell.Current.GoToAsync(
                    AppRoutes.RangosNutrientes);

                return;
            }

            await viewModel.InitializeAsync();
        }
    }
}
