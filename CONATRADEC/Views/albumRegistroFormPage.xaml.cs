using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Diagnostics;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(RegistroId), "RegistroId")]
    [QueryProperty(nameof(CategoriaId), "CategoriaId")]
    public partial class albumRegistroFormPage : ContentPage
    {
        private const double IdentificacionApiladaBreakpoint = 760;
        private const double AccionesApiladasBreakpoint = 560;

        private readonly AlbumRegistroFormViewModel viewModel = new();
        private bool? identificacionApilada;
        private bool? accionesApiladas;

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
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;

            Loaded += (_, _) => AplicarDisenoResponsivo();
            SizeChanged += (_, _) => AplicarDisenoResponsivo();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                AplicarDisenoResponsivo();
                viewModel.ActualizarPermisos();

                bool accesoDenegado =
                    !viewModel.CanView ||
                    (viewModel.Mode == FormModeSelect.Create && !viewModel.CanAdd) ||
                    (viewModel.Mode == FormModeSelect.Edit && !viewModel.CanEdit);

                if (accesoDenegado)
                {
                    await DisplayAlert(
                        "Permiso denegado",
                        "No tiene permisos para realizar esta operación.",
                        "Aceptar");

                    await Shell.Current.GoToAsync(AppRoutes.Regresar, false);
                    return;
                }

                await viewModel.InicializarAsync();
            }
            catch (OperationCanceledException)
            {
                // La navegación canceló la carga del formulario.
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al abrir formulario del álbum: {ex}");
                await DisplayAlert(
                    "No fue posible",
                    "No fue posible cargar el formulario del Álbum Botánico.",
                    "Aceptar");
            }
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            AplicarDisenoResponsivo();
        }

        private void AplicarDisenoResponsivo()
        {
            double anchoPagina = Width;
            if (anchoPagina <= 0)
                return;

            FormularioStack.Padding = anchoPagina < 600
                ? new Thickness(12, 10, 12, 26)
                : anchoPagina < 950
                    ? new Thickness(20, 16, 20, 32)
                    : new Thickness(28, 20, 28, 38);

            double ancho = FormularioStack.Width > 0
                ? FormularioStack.Width
                : anchoPagina;

            double anchoIdentificacion = ancho;

            bool apilarIdentificacion =
                anchoIdentificacion < IdentificacionApiladaBreakpoint;

            if (identificacionApilada != apilarIdentificacion)
            {
                identificacionApilada = apilarIdentificacion;
                ConfigurarPar(
                    IdentificacionCamposGrid,
                    SubcategoriaCampo,
                    NombreCientificoCampo,
                    apilarIdentificacion);
            }

            bool apilarAcciones =
                ancho < AccionesApiladasBreakpoint;
            if (accionesApiladas != apilarAcciones)
            {
                accionesApiladas = apilarAcciones;
                ConfigurarPar(
                    AccionesFormularioGrid,
                    CancelarFormularioButton,
                    GuardarFormularioButton,
                    apilarAcciones);
            }
        }

        private static void ConfigurarPar(
            Grid grid,
            View primero,
            View segundo,
            bool apilado)
        {
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();

            Grid.SetRowSpan(primero, 1);
            Grid.SetRowSpan(segundo, 1);
            Grid.SetColumnSpan(primero, 1);
            Grid.SetColumnSpan(segundo, 1);

            if (apilado)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

                Grid.SetRow(primero, 0);
                Grid.SetColumn(primero, 0);
                Grid.SetRow(segundo, 1);
                Grid.SetColumn(segundo, 0);
            }
            else
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

                Grid.SetRow(primero, 0);
                Grid.SetColumn(primero, 0);
                Grid.SetRow(segundo, 0);
                Grid.SetColumn(segundo, 1);
            }

            grid.InvalidateMeasure();
        }
    }
}
