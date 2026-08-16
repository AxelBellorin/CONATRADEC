using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    public partial class municipioFormPage : ContentPage, IQueryAttributable
    {
        private readonly MunicipioFormViewModel viewModel = new();
        private readonly SemaphoreSlim inicializacionLock = new(1, 1);

        private FormModeSelect modePendiente;
        private PaisRequest paisPendiente = new();
        private DepartamentoRequest departamentoPendiente = new();
        private MunicipioRequest municipioPendiente = new();
        private long versionParametros;
        private long versionInicializada;
        private bool parametrosValidos;
        private bool paginaVisible;

        public municipioFormPage()
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
                paisValue is PaisRequest pais &&
                pais.PaisId > 0;

            bool tieneDepartamento =
                query.TryGetValue("Departamento", out object? departamentoValue) &&
                departamentoValue is DepartamentoRequest departamento &&
                departamento.DepartamentoId is > 0;

            bool tieneMunicipio =
                query.TryGetValue("Municipio", out object? municipioValue) &&
                municipioValue is MunicipioRequest;

            parametrosValidos =
                tieneModo && tienePais && tieneDepartamento && tieneMunicipio;

            if (tieneModo)
                modePendiente = (FormModeSelect)modeValue!;

            if (tienePais)
                paisPendiente = (PaisRequest)paisValue!;

            if (tieneDepartamento)
                departamentoPendiente = (DepartamentoRequest)departamentoValue!;

            if (tieneMunicipio)
                municipioPendiente = (MunicipioRequest)municipioValue!;

            if (parametrosValidos)
            {
                if ((modePendiente == FormModeSelect.Edit ||
                     modePendiente == FormModeSelect.View) &&
                    municipioPendiente.MunicipioId is not > 0)
                {
                    parametrosValidos = false;
                }

                if (departamentoPendiente.PaisId.HasValue &&
                    departamentoPendiente.PaisId.Value != paisPendiente.PaisId)
                {
                    parametrosValidos = false;
                }
                else
                {
                    departamentoPendiente.PaisId = paisPendiente.PaisId;
                }

                if (municipioPendiente.DepartamentoId.HasValue &&
                    municipioPendiente.DepartamentoId !=
                    departamentoPendiente.DepartamentoId)
                {
                    parametrosValidos = false;
                }
                else
                {
                    municipioPendiente.DepartamentoId =
                        departamentoPendiente.DepartamentoId;
                }
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
                viewModel.PaisRequest = paisPendiente;
                viewModel.DepartamentoRequest = departamentoPendiente;
                viewModel.MunicipioRequest = municipioPendiente;
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
                        "No tiene permisos para realizar esta operación sobre municipios.",
                        "Aceptar");

                    await RegresarAListadoAsync();
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
                "No fue posible abrir el municipio",
                "No se recibieron correctamente la ubicación y los datos del formulario.",
                "Aceptar");

            await Shell.Current.GoToAsync(AppRoutes.Paises);
        }

        private Task RegresarAListadoAsync()
        {
            return Shell.Current.GoToAsync(
                "//MunicipioPage",
                new Dictionary<string, object>
                {
                    ["Pais"] = paisPendiente,
                    ["Departamento"] = departamentoPendiente,
                    ["TitlePage"] =
                        $"Municipios de {departamentoPendiente.NombreDepartamento} - {paisPendiente.NombrePais}"
                });
        }

        private void AjustarDiseno(double width)
        {
            if (width <= 0 || ContenidoMunicipioFormulario == null)
                return;

            ContenidoMunicipioFormulario.Padding =
                width < 600
                    ? new Thickness(12, 10, 12, 20)
                    : width < 900
                        ? new Thickness(20, 14, 20, 26)
                        : new Thickness(28, 16, 28, 30);
        }
    }
}
