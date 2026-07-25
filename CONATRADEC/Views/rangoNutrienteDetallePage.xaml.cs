using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class rangoNutrienteDetallePage :
        ContentPage,
        IQueryAttributable
    {
        private readonly RangoNutrienteDetalleViewModel
            viewModel = new();

        private bool parametrosAplicados;
        private int cantidadColumnasActual;

        public rangoNutrienteDetallePage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;
        }

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            if (query.TryGetValue(
                    "Categoria",
                    out object? categoria) &&
                categoria is RangoNutrienteCategoriaItem item)
            {
                viewModel.Categoria = item;
                parametrosAplicados = true;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();

            if (!viewModel.CanView)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para consultar los rangos nutricionales.",
                    "Aceptar");

                await Shell.Current.GoToAsync(
                    AppRoutes.RangosNutrientes);

                return;
            }

            AjustarCantidadColumnas(Width);

            for (int intento = 0;
                 intento < 20 &&
                 !parametrosAplicados;
                 intento++)
            {
                await Task.Delay(25);
            }

            if (!parametrosAplicados ||
                viewModel.Categoria == null ||
                viewModel.Categoria.TipoCultivoId <= 0)
            {
                await DisplayAlert(
                    "Tipo de cultivo no válido",
                    "No fue posible identificar el tipo de cultivo seleccionado.",
                    "Aceptar");

                await Shell.Current.GoToAsync(
                    AppRoutes.RangosNutrientes);

                return;
            }

            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarCantidadColumnas(width);
        }

        private void AjustarCantidadColumnas(double width)
        {
            if (width <= 0 ||
                RangosGridLayout == null)
            {
                return;
            }

            int nuevasColumnas =
                width >= 1200
                    ? 3
                    : width >= 700
                        ? 2
                        : 1;

            if (cantidadColumnasActual == nuevasColumnas)
                return;

            cantidadColumnasActual = nuevasColumnas;
            RangosGridLayout.Span = nuevasColumnas;
        }
    }
}
