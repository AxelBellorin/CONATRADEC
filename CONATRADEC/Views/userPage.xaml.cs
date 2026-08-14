using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class userPage : ContentPage
    {
        private readonly UserViewModel viewModel = new();
        private int columnasActuales;

        public userPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();
            AjustarDiseno(Width);

            if (!viewModel.CanView)
                return;

            bool nuevaVisita =
                UsuarioVisitaService.AsegurarVisita();

            if (nuevaVisita)
            {
                await viewModel.IniciarNuevaVisitaAsync();
                return;
            }

            // Regresar desde Ver/Editar/Crear pertenece a la misma visita.
            // Normalmente se aplican únicamente los cambios confirmados por el
            // servidor sin ejecutar otro GET. La única excepción es una
            // reactivación desde Usuarios inactivos: al cambiar la composición
            // global del listado paginado se renueva solo la página visible.
            if (UsuarioVisitaService.ConsumirRecargaListado())
            {
                await viewModel.RecargarPaginaActualAsync();
                return;
            }

            viewModel.AplicarCambiosPendientes();

            if (!viewModel.TienePaginaCargada)
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
            AjustarDiseno(width);
        }

        private void AjustarDiseno(double width)
        {
            if (width <= 0)
                return;

            AjustarColumnas(width);
            AjustarPaginacion(width);
        }

        private void AjustarColumnas(double width)
        {
            if (UsuariosGridLayout == null)
                return;

            int columnas =
                width >= 1200
                    ? 3
                    : width >= 700
                        ? 2
                        : 1;

            if (columnasActuales == columnas)
                return;

            columnasActuales = columnas;
            UsuariosGridLayout.Span = columnas;
        }

        private void AjustarPaginacion(double width)
        {
            if (PaginacionUsuarios == null)
                return;

            // Se usa el ancho real disponible, no únicamente OnIdiom. Esto
            // mantiene el paginador correcto también al redimensionar Windows.
            double margenHorizontal =
                width < 480
                    ? 24
                    : width < 800
                        ? 36
                        : 48;

            double anchoDisponible =
                Math.Max(0, width - margenHorizontal);

            PaginacionUsuarios.WidthRequest =
                Math.Min(560, anchoDisponible);
        }
    }
}
