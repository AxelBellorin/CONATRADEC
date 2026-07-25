using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(User), "User")]
    public partial class userFormPage : ContentPage
    {
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
