using CONATRADEC.Controls;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Collections.Specialized;
using System;
using System.Linq;

namespace CONATRADEC.Views
{
    public partial class CatalogoEliminadosPage : ContentPage
    {
        private const double AccionesCompactasBreakpoint = 560;
        private const double DosColumnasBreakpoint = 900;
        private const double TresColumnasBreakpoint = 1280;

        private int spanActual = -1;
        private bool? accionesCompactas;
        private Grid? accionesGrid;
        private Label? resumenLabel;
        private Button? buscarButton;
        private Button? limpiarButton;

        public CatalogoEliminadosPage(
            CatalogoEliminadoConfiguracion configuracion)
        {
            InitializeComponent();

            var viewModel =
                new CatalogoEliminadosViewModel(
                    configuracion);

            BindingContext = viewModel;

            viewModel.Registros.CollectionChanged +=
                OnRegistrosCollectionChanged;

            Loaded += OnPaginaLoaded;
            SizeChanged += OnPaginaSizeChanged;
            RegistrosCollection.SizeChanged +=
                OnRegistrosCollectionSizeChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is
                CatalogoEliminadosViewModel viewModel)
            {
                await viewModel.InicializarAsync();
                AplicarDisenoResponsivo();
            }
        }

        /// <summary>
        /// Esta pantalla se abre como una ventana modal.
        /// En Android el botón físico y el gesto de retroceso se consumen
        /// para que únicamente los botones de la aplicación puedan cerrarla.
        /// </summary>
        protected override bool OnBackButtonPressed()
        {
#if ANDROID
            return true;
#else
            return base.OnBackButtonPressed();
#endif
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(
                width,
                height);

            AplicarDisenoResponsivo();
        }

        private void OnPaginaLoaded(
            object? sender,
            EventArgs e)
        {
            AplicarDisenoResponsivo();
        }

        private void OnPaginaSizeChanged(
            object? sender,
            EventArgs e)
        {
            AplicarDisenoResponsivo();
        }

        private void OnRegistrosCollectionSizeChanged(
            object? sender,
            EventArgs e)
        {
            AplicarColumnas();
        }

        private void OnRegistrosCollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            AplicarColumnas();
        }

        /// <summary>
        /// La cuadrícula se calcula con el ancho útil del CollectionView.
        /// Cuando no hay elementos se fuerza una sola columna para que el
        /// estado vacío utilice todo el ancho y permanezca centrado.
        /// </summary>
        private void AplicarDisenoResponsivo()
        {
            ResolverAccionesBusqueda();
            AplicarColumnas();
            AplicarAccionesBusqueda();
        }

        private void AplicarColumnas()
        {
            double ancho =
                RegistrosCollection.Width > 0
                    ? RegistrosCollection.Width
                    : Width;

            if (ancho <= 0)
                return;

            bool sinRegistros =
                BindingContext is CatalogoEliminadosViewModel viewModel &&
                viewModel.Registros.Count == 0;

            int nuevoSpan = sinRegistros
                ? 1
                : ancho >= TresColumnasBreakpoint
                    ? 3
                    : ancho >= DosColumnasBreakpoint
                        ? 2
                        : 1;

            if (spanActual == nuevoSpan &&
                RegistrosGrid.Span == nuevoSpan)
            {
                return;
            }

            spanActual = nuevoSpan;
            RegistrosGrid.Span = nuevoSpan;
        }

        private void AplicarAccionesBusqueda()
        {
            if (accionesGrid == null ||
                resumenLabel == null ||
                buscarButton == null ||
                limpiarButton == null)
            {
                return;
            }

            double ancho =
                accionesGrid.Width > 0
                    ? accionesGrid.Width
                    : Width;

            if (ancho <= 0)
                return;

            bool compacto =
                ancho < AccionesCompactasBreakpoint;

            if (accionesCompactas == compacto)
                return;

            accionesCompactas = compacto;

            accionesGrid.ColumnDefinitions.Clear();
            accionesGrid.RowDefinitions.Clear();

            if (compacto)
            {
                accionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                accionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                accionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                accionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(resumenLabel, 0);
                Grid.SetColumn(resumenLabel, 0);
                Grid.SetColumnSpan(resumenLabel, 2);

                Grid.SetRow(buscarButton, 1);
                Grid.SetColumn(buscarButton, 0);
                Grid.SetColumnSpan(buscarButton, 1);

                Grid.SetRow(limpiarButton, 1);
                Grid.SetColumn(limpiarButton, 1);
                Grid.SetColumnSpan(limpiarButton, 1);

                buscarButton.HorizontalOptions =
                    LayoutOptions.Fill;
                limpiarButton.HorizontalOptions =
                    LayoutOptions.Fill;
                buscarButton.MinimumWidthRequest = 0;
                limpiarButton.MinimumWidthRequest = 0;
            }
            else
            {
                accionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                accionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                accionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                accionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(resumenLabel, 0);
                Grid.SetColumn(resumenLabel, 0);
                Grid.SetColumnSpan(resumenLabel, 1);

                Grid.SetRow(buscarButton, 0);
                Grid.SetColumn(buscarButton, 1);
                Grid.SetColumnSpan(buscarButton, 1);

                Grid.SetRow(limpiarButton, 0);
                Grid.SetColumn(limpiarButton, 2);
                Grid.SetColumnSpan(limpiarButton, 1);
            }

            accionesGrid.InvalidateMeasure();
        }

        private void ResolverAccionesBusqueda()
        {
            if (accionesGrid != null)
                return;

            Button? buscar =
                ResponsiveLayoutUtility.FindDescendant<Button>(
                    this,
                    button =>
                        string.Equals(
                            button.Text?.Trim(),
                            "Buscar",
                            StringComparison.OrdinalIgnoreCase));

            Button? limpiar =
                ResponsiveLayoutUtility.FindDescendant<Button>(
                    this,
                    button =>
                        string.Equals(
                            button.Text?.Trim(),
                            "Limpiar",
                            StringComparison.OrdinalIgnoreCase));

            if (buscar == null || limpiar == null)
                return;

            Grid? grid =
                ResponsiveLayoutUtility.FindAncestor<Grid>(
                    buscar);

            if (grid == null ||
                !ReferenceEquals(
                    ResponsiveLayoutUtility.FindAncestor<Grid>(limpiar),
                    grid))
            {
                return;
            }

            Label? resumen =
                grid.Children
                    .OfType<Label>()
                    .FirstOrDefault();

            if (resumen == null)
                return;

            accionesGrid = grid;
            resumenLabel = resumen;
            buscarButton = buscar;
            limpiarButton = limpiar;
        }
    }
}
