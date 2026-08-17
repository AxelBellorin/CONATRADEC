using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;
using System.Threading;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    public partial class rangoNutrienteCategoriaFormPage :
        ContentPage,
        IQueryAttributable
    {
        private readonly RangoNutrienteCategoriaFormViewModel
            viewModel = new();

        private readonly SemaphoreSlim inicializacionLock =
            new(1, 1);

        private bool parametrosNavegacionValidos;
        private bool paginaVisible;
        private long versionParametros;
        private long versionInicializada;

        public rangoNutrienteCategoriaFormPage()
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

            bool tieneItem =
                query.TryGetValue(
                    "Item",
                    out object? itemObject) &&
                itemObject is TipoCultivoRequest;

            parametrosNavegacionValidos =
                tieneModo && tieneItem;

            if (parametrosNavegacionValidos)
            {
                viewModel.Mode =
                    (FormModeSelect)modeObject!;

                viewModel.Item =
                    (TipoCultivoRequest)itemObject!;
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
            AjustarAnchoFormulario(Width);

            await InicializarParametrosPendientesAsync();
        }

        protected override void OnDisappearing()
        {
            paginaVisible = false;
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarAnchoFormulario(width);
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

                if (!parametrosNavegacionValidos)
                {
                    await DisplayAlert(
                        "Tipo de cultivo no válido",
                        "No fue posible recibir correctamente los datos del formulario.",
                        "Aceptar");

                    await Shell.Current.GoToAsync(
                        AppRoutes.Regresar);

                    return;
                }

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
                        "No tiene permisos para administrar tipos de cultivo.",
                        "Aceptar");

                    await Shell.Current.GoToAsync(
                        AppRoutes.Regresar);

                    return;
                }

                RangoNutrienteVisitaService.AsegurarVisita();
            }
            finally
            {
                inicializacionLock.Release();
            }
        }

        private void AjustarAnchoFormulario(double anchoPagina)
        {
            if (FormularioContainer == null ||
                anchoPagina <= 0)
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
        }
    }
}
