using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Threading;

namespace CONATRADEC.Views
{
    public partial class rangoNutrienteDetallePage :
        ContentPage,
        IQueryAttributable
    {
        private readonly RangoNutrienteDetalleViewModel
            viewModel = new();

        private readonly SemaphoreSlim inicializacionLock =
            new(1, 1);

        private bool parametrosNavegacionValidos;
        private bool paginaVisible;
        private long versionParametros;
        private long versionInicializada;
        private long generacionVisitaCargada;
        private int tipoCultivoCargado;
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
            parametrosNavegacionValidos =
                query.TryGetValue(
                    "Categoria",
                    out object? categoria) &&
                categoria is RangoNutrienteCategoriaItem item &&
                item.TipoCultivoId > 0;

            if (parametrosNavegacionValidos)
            {
                viewModel.Categoria =
                    (RangoNutrienteCategoriaItem)categoria!;
            }

            Interlocked.Increment(ref versionParametros);

            /*
             * Shell puede aplicar los atributos antes o después de
             * OnAppearing. La versión de parámetros evita temporizadores y
             * garantiza que cada navegación se procese una sola vez.
             */
            if (paginaVisible)
            {
                Dispatcher.Dispatch(
                    () =>
                        _ = InicializarParametrosPendientesAsync());
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            paginaVisible = true;
            viewModel.ActualizarPermisos();
            AjustarCantidadColumnas(Width);

            await InicializarParametrosPendientesAsync();
        }

        protected override void OnDisappearing()
        {
            paginaVisible = false;
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

        private async Task InicializarParametrosPendientesAsync()
        {
            await inicializacionLock.WaitAsync();

            try
            {
                long versionActual =
                    Volatile.Read(ref versionParametros);

                if (versionActual <= 0 &&
                    !parametrosNavegacionValidos)
                {
                    return;
                }

                if (versionActual > 0 &&
                    versionInicializada != versionActual)
                {
                    versionInicializada = versionActual;
                }

                if (!viewModel.CanView)
                {
                    await DisplayAlert(
                        "Permiso denegado",
                        "No tiene permisos para consultar los rangos nutricionales.",
                        "Aceptar");

                    await Shell.Current.GoToAsync(
                        AppRoutes.Regresar);

                    return;
                }

                if (!parametrosNavegacionValidos ||
                    viewModel.Categoria == null ||
                    viewModel.Categoria.TipoCultivoId <= 0)
                {
                    await DisplayAlert(
                        "Tipo de cultivo no válido",
                        "No fue posible identificar el tipo de cultivo seleccionado.",
                        "Aceptar");

                    await Shell.Current.GoToAsync(
                        AppRoutes.Regresar);

                    return;
                }

                RangoNutrienteVisitaService.AsegurarVisita();

                long generacionActual =
                    RangoNutrienteVisitaService.GeneracionActual;

                int tipoCultivoActual =
                    viewModel.Categoria.TipoCultivoId;

                bool cambioContexto =
                    generacionVisitaCargada != generacionActual ||
                    tipoCultivoCargado != tipoCultivoActual;

                if (cambioContexto)
                {
                    generacionVisitaCargada = generacionActual;
                    tipoCultivoCargado = tipoCultivoActual;

                    /*
                     * Una visita nueva o un cultivo distinto siempre parte de
                     * datos frescos. Los retornos desde formularios dentro de
                     * la misma visita conservan filtro, páginas y colección.
                     */
                    RangoNutrienteVisitaService
                        .ConsumirRecargaDetalle(tipoCultivoActual);

                    await viewModel.IniciarNuevaVisitaAsync();
                    return;
                }

                if (RangoNutrienteVisitaService
                    .ConsumirRecargaDetalle(tipoCultivoActual))
                {
                    await viewModel.RecargarVentanaActualAsync();
                    return;
                }

                if (!viewModel.TienePaginaCargada)
                    await viewModel.InicializarAsync();
            }
            finally
            {
                inicializacionLock.Release();
            }
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
