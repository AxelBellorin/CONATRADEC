using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class MotivoDevolucionTecnicoEliminadosPage : ContentPage
    {
        private readonly MotivoDevolucionTecnicoEliminadosViewModel viewModel = new();
        private bool? accionesCompactas;

        public MotivoDevolucionTecnicoEliminadosPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.InicializarAsync();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (width <= 0)
                return;

            bool compactas = width < 480;
            if (accionesCompactas == compactas)
                return;

            accionesCompactas = compactas;
            AplicarDistribucionAcciones(compactas);
        }

        private void AplicarDistribucionAcciones(bool compactas)
        {
            AccionesEliminadosGrid.RowDefinitions.Clear();
            AccionesEliminadosGrid.ColumnDefinitions.Clear();

            if (compactas)
            {
                AccionesEliminadosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                AccionesEliminadosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                AccionesEliminadosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                AccionesEliminadosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Ubicar(BuscarButton, 0, 0, 1);
                Ubicar(LimpiarButton, 0, 1, 1);
                Ubicar(ActualizarButton, 1, 0, 2);
            }
            else
            {
                for (int indice = 0; indice < 3; indice++)
                {
                    AccionesEliminadosGrid.ColumnDefinitions.Add(
                        new ColumnDefinition(GridLength.Star));
                }

                AccionesEliminadosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Ubicar(BuscarButton, 0, 0, 1);
                Ubicar(LimpiarButton, 0, 1, 1);
                Ubicar(ActualizarButton, 0, 2, 1);
            }
        }

        private static void Ubicar(
            View vista,
            int fila,
            int columna,
            int columnas)
        {
            Grid.SetRow(vista, fila);
            Grid.SetColumn(vista, columna);
            Grid.SetColumnSpan(vista, columnas);
        }
    }
}
