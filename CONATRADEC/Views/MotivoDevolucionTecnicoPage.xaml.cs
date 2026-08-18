using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class MotivoDevolucionTecnicoPage : ContentPage
    {
        private readonly MotivoDevolucionTecnicoViewModel viewModel = new();
        private bool primeraAparicion = true;
        private bool? accionesCompactas;

        public MotivoDevolucionTecnicoPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            viewModel.ActualizarPermisos();

            if (primeraAparicion)
            {
                primeraAparicion = false;
                await viewModel.InicializarAsync();
            }
            else
            {
                // Crear, editar y consultar Eliminados son subflujos internos.
                // Al regresar se conserva la visita y sus filtros, pero se
                // refrescan los datos del servidor.
                await viewModel.RecargarVisitaAsync();
            }
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (width <= 0)
                return;

            bool compactas = width < 620;
            if (accionesCompactas == compactas)
                return;

            accionesCompactas = compactas;
            AplicarDistribucionAcciones(compactas);
        }

        private void AplicarDistribucionAcciones(bool compactas)
        {
            AccionesBusquedaGrid.RowDefinitions.Clear();
            AccionesBusquedaGrid.ColumnDefinitions.Clear();

            if (compactas)
            {
                AccionesBusquedaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                AccionesBusquedaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                AccionesBusquedaGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                AccionesBusquedaGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Ubicar(BuscarButton, 0, 0);
                Ubicar(LimpiarButton, 0, 1);
                Ubicar(ActualizarButton, 1, 0);
                Ubicar(EliminadosButton, 1, 1);
            }
            else
            {
                for (int indice = 0; indice < 4; indice++)
                {
                    AccionesBusquedaGrid.ColumnDefinitions.Add(
                        new ColumnDefinition(GridLength.Star));
                }

                AccionesBusquedaGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Ubicar(BuscarButton, 0, 0);
                Ubicar(LimpiarButton, 0, 1);
                Ubicar(ActualizarButton, 0, 2);
                Ubicar(EliminadosButton, 0, 3);
            }
        }

        private static void Ubicar(View vista, int fila, int columna)
        {
            Grid.SetRow(vista, fila);
            Grid.SetColumn(vista, columna);
        }
    }
}
