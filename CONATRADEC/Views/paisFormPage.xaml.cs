using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    public partial class paisFormPage : ContentPage, IQueryAttributable
    {
        private readonly PaisFormViewModel viewModel = new();
        private readonly SemaphoreSlim inicializacionLock = new(1, 1);

        private FormModeSelect modePendiente;
        private PaisRequest paisPendiente = new();
        private long versionParametros;
        private long versionInicializada;
        private bool parametrosValidos;
        private bool paginaVisible;

        public paisFormPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            bool tieneModo =
                query.TryGetValue("Mode", out object? modeValue) &&
                modeValue is FormModeSelect;

            bool tienePais =
                query.TryGetValue("Pais", out object? paisValue) &&
                paisValue is PaisRequest;

            parametrosValidos = tieneModo && tienePais;

            if (tieneModo)
                modePendiente = (FormModeSelect)modeValue!;

            if (tienePais)
                paisPendiente = (PaisRequest)paisValue!;

            if (parametrosValidos &&
                (modePendiente == FormModeSelect.Edit ||
                 modePendiente == FormModeSelect.View) &&
                paisPendiente.PaisId <= 0)
            {
                parametrosValidos = false;
            }

            Interlocked.Increment(ref versionParametros);

            if (paginaVisible)
            {
                Dispatcher.Dispatch(
                    () => _ = InicializarParametrosPendientesAsync());
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            paginaVisible = true;
            UbicacionVisitaService.AsegurarVisita();
            AjustarDiseno(Width);
            await InicializarParametrosPendientesAsync();
        }

        protected override void OnDisappearing()
        {
            paginaVisible = false;
            viewModel.CancelarOperaciones();
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarDiseno(width);
        }

        private async Task InicializarParametrosPendientesAsync()
        {
            await inicializacionLock.WaitAsync();

            try
            {
                long actual = Volatile.Read(ref versionParametros);

                // Shell puede entregar IQueryAttributable después de OnAppearing.
                // Hasta recibir una versión de parámetros no se inicializa ni
                // se interpreta el modo predeterminado como Crear.
                if (actual <= 0)
                    return;

                if (!parametrosValidos)
                {
                    if (versionInicializada != actual)
                    {
                        versionInicializada = actual;
                        await MostrarErrorNavegacionAsync();
                    }

                    return;
                }

                if (versionInicializada == actual)
                    return;

                viewModel.Mode = modePendiente;
                viewModel.Pais = paisPendiente;
                viewModel.ActualizarPermisos();
                versionInicializada = actual;

                bool denegado =
                    !viewModel.CanView ||
                    (modePendiente == FormModeSelect.Create && !viewModel.CanAdd) ||
                    (modePendiente == FormModeSelect.Edit && !viewModel.CanEdit);

                if (denegado)
                {
                    await DisplayAlert(
                        "Permiso denegado",
                        "No tiene permisos para realizar esta operación sobre países.",
                        "Aceptar");

                    await Shell.Current.GoToAsync(AppRoutes.Paises);
                }
            }
            finally
            {
                inicializacionLock.Release();
            }
        }

        private async Task MostrarErrorNavegacionAsync()
        {
            await DisplayAlert(
                "No fue posible abrir el país",
                "No se recibieron correctamente los datos requeridos para el formulario.",
                "Aceptar");

            await Shell.Current.GoToAsync(AppRoutes.Paises);
        }

        private void AjustarDiseno(double width)
        {
            if (width <= 0)
                return;

            if (ContenidoPaisFormulario != null)
            {
                ContenidoPaisFormulario.Padding =
                    width < 600
                        ? new Thickness(12, 12, 12, 20)
                        : width < 900
                            ? new Thickness(20, 18, 20, 26)
                            : new Thickness(28, 22, 28, 30);
            }

            if (CamposPaisGrid == null || CampoCodigoIso == null)
                return;

            CamposPaisGrid.ColumnDefinitions.Clear();
            CamposPaisGrid.RowDefinitions.Clear();

            if (width < 700)
            {
                CamposPaisGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                CamposPaisGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                CamposPaisGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(CampoCodigoIso, 1);
                Grid.SetColumn(CampoCodigoIso, 0);
            }
            else
            {
                CamposPaisGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(new GridLength(2, GridUnitType.Star)));
                CamposPaisGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                CamposPaisGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(CampoCodigoIso, 0);
                Grid.SetColumn(CampoCodigoIso, 1);
            }
        }
    }
}
