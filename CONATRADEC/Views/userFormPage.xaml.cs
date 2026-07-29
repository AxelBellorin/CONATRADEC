using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;
using System.Linq;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(User), "User")]
    public partial class userFormPage : ContentPage
    {
        private const string MarcaEncabezadoPropio =
            "CONATRADEC_FORM_BACK_WRAPPER";

        private readonly UserFormViewModel viewModel = new();

        public FormModeSelect Mode
        {
            set => viewModel.Mode = value;
        }

        public UserRequest User
        {
            set => viewModel.User =
                value ?? new UserRequest();
        }

        public userFormPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;

            ConfigurarBotonRegresarSuperior();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("userPage");

            bool denegado =
                !viewModel.CanView ||
                (viewModel.Mode == FormModeSelect.Create &&
                 !viewModel.CanAdd) ||
                (viewModel.Mode == FormModeSelect.Edit &&
                 !viewModel.CanEdit);

            if (denegado)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para realizar esta operación.",
                    "Aceptar");

                await Shell.Current.GoToAsync(AppRoutes.Usuarios);
                return;
            }

            AjustarDiseno(Width);
            await viewModel.InicializarAsync();
        }

        /// <summary>
        /// Configura una flecha propia para el formulario de usuarios.
        ///
        /// Se enlaza directamente con CancelCommand para conservar:
        /// - la confirmación de cambios sin guardar;
        /// - la limpieza del formulario;
        /// - el regreso correcto al listado de usuarios.
        ///
        /// También se marca el contenido para impedir que el servicio global
        /// agregue una segunda flecha sobre esta página.
        /// </summary>
        private void ConfigurarBotonRegresarSuperior()
        {
            if (Content != null)
            {
                Content.StyleId =
                    MarcaEncabezadoPropio;
            }

            if (FormularioContainer == null)
                return;

            bool yaExiste =
                FormularioContainer
                    .Children
                    .OfType<View>()
                    .Any(view =>
                        string.Equals(
                            view.AutomationId,
                            "EncabezadoRegresarUsuario",
                            StringComparison.Ordinal));

            if (yaExiste)
                return;

            var botonRegresar =
                new Button
                {
                    Text = "←",
                    WidthRequest = 48,
                    HeightRequest = 48,
                    MinimumWidthRequest = 48,
                    MinimumHeightRequest = 48,
                    Padding = 0,
                    CornerRadius = 14,
                    FontSize = 23,
                    FontAttributes =
                        FontAttributes.Bold,
                    BackgroundColor =
                        Color.FromArgb("#F3F5F4"),
                    TextColor =
                        Color.FromArgb("#263238"),
                    HorizontalOptions =
                        LayoutOptions.Start,
                    VerticalOptions =
                        LayoutOptions.Center,
                    AutomationId =
                        "BotonRegresarUsuario"
                };

            botonRegresar.SetBinding(
                Button.CommandProperty,
                nameof(
                    UserFormViewModel
                        .CancelCommand));

            SemanticProperties.SetDescription(
                botonRegresar,
                "Regresar al listado de usuarios");

            var encabezado =
                new Grid
                {
                    AutomationId =
                        "EncabezadoRegresarUsuario",
                    Padding =
                        new Thickness(0, 0, 0, 2),
                    HorizontalOptions =
                        LayoutOptions.Fill,
                    VerticalOptions =
                        LayoutOptions.Start
                };

            encabezado.Children.Add(
                botonRegresar);

            FormularioContainer
                .Children
                .Insert(
                    0,
                    encabezado);
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarDiseno(width);
        }

        private void AjustarDiseno(double ancho)
        {
            if (ancho <= 0 ||
                FormularioContainer == null)
            {
                return;
            }

            double margen =
                DeviceInfo.Platform == DevicePlatform.WinUI
                    ? 72
                    : 32;

            FormularioContainer.WidthRequest =
                Math.Min(
                    Math.Max(280, ancho - margen),
                    1100);

            bool amplio = ancho >= 700;

            AjustarGrid(
                AccesoGrid,
                new[]
                {
                    UsuarioSection,
                    ClaveSection,
                    NombreSection,
                    IdentificacionSection
                },
                amplio,
                2);

            AjustarGrid(
                ContactoGrid,
                new[]
                {
                    CorreoSection,
                    TelefonoSection,
                    FechaSection,
                    RolSection
                },
                amplio,
                2);

            AjustarGrid(
                UbicacionGrid,
                new[]
                {
                    PaisSection,
                    DepartamentoSection,
                    MunicipioSection
                },
                amplio,
                3);
        }

        private static void AjustarGrid(
            Grid grid,
            IReadOnlyList<View> secciones,
            bool amplio,
            int columnasAmplias)
        {
            grid.ColumnDefinitions.Clear();
            grid.RowDefinitions.Clear();

            int columnas = amplio ? columnasAmplias : 1;
            int filas =
                (int)Math.Ceiling(
                    secciones.Count / (double)columnas);

            for (int i = 0; i < columnas; i++)
            {
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
            }

            for (int i = 0; i < filas; i++)
            {
                grid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
            }

            for (int i = 0; i < secciones.Count; i++)
            {
                Grid.SetRow(secciones[i], i / columnas);
                Grid.SetColumn(secciones[i], i % columnas);
            }
        }
    }
}
