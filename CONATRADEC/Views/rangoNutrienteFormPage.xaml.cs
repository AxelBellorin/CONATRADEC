using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;
using System.Threading;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    public partial class rangoNutrienteFormPage :
        ContentPage,
        IQueryAttributable
    {
        private readonly RangoNutrienteFormViewModel
            viewModel = new();

        private readonly SemaphoreSlim inicializacionLock =
            new(1, 1);

        private bool parametrosNavegacionValidos;
        private bool paginaVisible;
        private long versionParametros;
        private long versionInicializada;

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
            bool tieneModo =
                query.TryGetValue(
                    "Mode",
                    out object? modeObject) &&
                modeObject is FormModeSelect;

            bool tieneCategoria =
                query.TryGetValue(
                    "Categoria",
                    out object? categoriaObject) &&
                categoriaObject is RangoNutrienteCategoriaItem categoria &&
                categoria.TipoCultivoId > 0;

            bool tieneItem =
                query.TryGetValue(
                    "Item",
                    out object? itemObject) &&
                itemObject is RangoNutrienteRequest;

            parametrosNavegacionValidos =
                tieneModo &&
                tieneCategoria &&
                tieneItem;

            if (parametrosNavegacionValidos)
            {
                viewModel.PrepararNavegacion(
                    (FormModeSelect)modeObject!,
                    (RangoNutrienteCategoriaItem)categoriaObject!,
                    (RangoNutrienteRequest)itemObject!);
            }

            Interlocked.Increment(ref versionParametros);

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
            AjustarDiseno(Width);

            await InicializarParametrosPendientesAsync();
        }

        protected override void OnDisappearing()
        {
            paginaVisible = false;
            viewModel.CancelarOperaciones();
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarDiseno(width);
        }

        private async Task InicializarParametrosPendientesAsync()
        {
            await inicializacionLock.WaitAsync();

            try
            {
                long versionActual =
                    Volatile.Read(ref versionParametros);

                if (versionActual <= 0 ||
                    versionInicializada == versionActual)
                {
                    return;
                }

                versionInicializada = versionActual;

                viewModel.LoadPagePermissions(
                    "rangoNutrientePage");

                bool denied =
                    !viewModel.CanView ||
                    (viewModel.Mode == FormModeSelect.Create &&
                     !viewModel.CanAdd) ||
                    (viewModel.Mode == FormModeSelect.Edit &&
                     !viewModel.CanEdit);

                if (denied)
                {
                    await DisplayAlert(
                        "Permiso denegado",
                        "No tiene permisos para realizar esta operación.",
                        "Aceptar");

                    await Shell.Current.GoToAsync(
                        AppRoutes.Regresar);

                    return;
                }

                if (!parametrosNavegacionValidos ||
                    !viewModel.TieneTipoCultivoValido)
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
                await viewModel.InitializeAsync();
            }
            finally
            {
                inicializacionLock.Release();
            }
        }

        private void AjustarDiseno(double anchoPagina)
        {
            if (anchoPagina <= 0 ||
                FormularioContainer == null ||
                DatosReferenciaGrid == null ||
                ValoresGrid == null)
            {
                return;
            }

            double margenHorizontal =
                DeviceInfo.Platform == DevicePlatform.WinUI
                    ? 72
                    : 32;

            double anchoDisponible =
                Math.Max(
                    280,
                    anchoPagina - margenHorizontal);

            FormularioContainer.WidthRequest =
                Math.Min(anchoDisponible, 1100);

            bool amplio =
                anchoPagina >= 700;

            AjustarGrid(
                DatosReferenciaGrid,
                CultivoSection,
                ElementoSection,
                amplio);

            AjustarGrid(
                ValoresGrid,
                MinimoSection,
                MaximoSection,
                amplio);
        }

        private static void AjustarGrid(
            Grid grid,
            View primeraSeccion,
            View segundaSeccion,
            bool amplio)
        {
            grid.ColumnDefinitions.Clear();
            grid.RowDefinitions.Clear();

            if (amplio)
            {
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                grid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(primeraSeccion, 0);
                Grid.SetColumn(primeraSeccion, 0);
                Grid.SetRow(segundaSeccion, 0);
                Grid.SetColumn(segundaSeccion, 1);

                return;
            }

            grid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));

            grid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            grid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Grid.SetRow(primeraSeccion, 0);
            Grid.SetColumn(primeraSeccion, 0);
            Grid.SetRow(segundaSeccion, 1);
            Grid.SetColumn(segundaSeccion, 0);
        }
    }
}
