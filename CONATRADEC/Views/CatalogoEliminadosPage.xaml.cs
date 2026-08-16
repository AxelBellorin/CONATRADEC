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
        private Button? paginaAnteriorButton;
        private Button? paginaSiguienteButton;
        private bool inicializacionSolicitada;
        private bool eventosPaginacionConfigurados;
        private int paginaAntesCambio = -1;

        public CatalogoEliminadosPage(
            CatalogoEliminadoConfiguracion configuracion)
            : this(configuracion, null)
        {
        }

        public CatalogoEliminadosPage(
            CatalogoEliminadoConfiguracion configuracion,
            int? parentId)
        {
            InitializeComponent();

            var viewModel =
                new CatalogoEliminadosViewModel(
                    configuracion,
                    new CatalogosEliminadosApiService(
                        configuracion.Codigo,
                        parentId),
                    new UsuariosInactivosApiService(),
                    new TerrenosInactivosApiService());

            BindingContext = viewModel;

            viewModel.Registros.CollectionChanged +=
                OnRegistrosCollectionChanged;

            Loaded += OnPaginaLoaded;
            SizeChanged += OnPaginaSizeChanged;
            RegistrosCollection.SizeChanged +=
                OnRegistrosCollectionSizeChanged;

            ConfigurarScrollPaginacion();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ConfigurarScrollPaginacion();
            AplicarDisenoResponsivo();
        }

        /// <summary>
        /// La carga inicial se ejecuta desde CatalogoEliminadosLauncher
        /// después de que PushModalAsync haya terminado. Esto garantiza que
        /// el relay ya esté visible antes de iniciar la consulta al servidor.
        /// </summary>
        public async Task InicializarDespuesDeMostrarAsync()
        {
            if (inicializacionSolicitada)
                return;

            inicializacionSolicitada = true;

            if (BindingContext is not CatalogoEliminadosViewModel viewModel)
                return;

            await viewModel.InicializarAsync();
            AplicarDisenoResponsivo();
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
            base.OnSizeAllocated(width, height);
            AplicarDisenoResponsivo();
        }

        private void OnPaginaLoaded(
            object? sender,
            EventArgs e)
        {
            ConfigurarScrollPaginacion();
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
        /// Localiza los botones de paginación de la pantalla común y conecta
        /// los eventos de presentación sin reemplazar los Command del ViewModel.
        /// Así Usuarios inactivos, Terrenos eliminados y cualquier catálogo
        /// paginado que reutilice esta vista conserva una sola implementación.
        /// </summary>
        private void ConfigurarScrollPaginacion()
        {
            if (eventosPaginacionConfigurados)
                return;

            paginaAnteriorButton ??=
                ResponsiveLayoutUtility.FindDescendant<Button>(
                    this,
                    button =>
                        string.Equals(
                            button.Text?.Trim(),
                            "← Anterior",
                            StringComparison.OrdinalIgnoreCase));

            paginaSiguienteButton ??=
                ResponsiveLayoutUtility.FindDescendant<Button>(
                    this,
                    button =>
                        string.Equals(
                            button.Text?.Trim(),
                            "Siguiente →",
                            StringComparison.OrdinalIgnoreCase));

            if (paginaAnteriorButton == null ||
                paginaSiguienteButton == null)
            {
                return;
            }

            paginaAnteriorButton.Pressed +=
                PaginacionEliminados_Pressed;
            paginaSiguienteButton.Pressed +=
                PaginacionEliminados_Pressed;

            paginaAnteriorButton.Clicked +=
                PaginacionEliminados_Clicked;
            paginaSiguienteButton.Clicked +=
                PaginacionEliminados_Clicked;

            eventosPaginacionConfigurados = true;
        }

        /// <summary>
        /// Captura la página visible antes de que el Command solicite la nueva
        /// página. Pressed ocurre antes de Clicked y permite detectar el cambio
        /// incluso si la respuesta del servidor es muy rápida.
        /// </summary>
        private void PaginacionEliminados_Pressed(
            object? sender,
            EventArgs e)
        {
            if (BindingContext is CatalogoEliminadosViewModel viewModel)
            {
                paginaAntesCambio =
                    viewModel.PaginaActual;
            }
        }

        /// <summary>
        /// Después de Anterior/Siguiente espera a que termine la consulta y
        /// posiciona el primer registro de la nueva página al inicio visible.
        /// El ViewModel continúa siendo el único responsable de la paginación.
        /// </summary>
        private async void PaginacionEliminados_Clicked(
            object? sender,
            EventArgs e)
        {
            if (BindingContext is not CatalogoEliminadosViewModel viewModel)
                return;

            int paginaOrigen =
                paginaAntesCambio > 0
                    ? paginaAntesCambio
                    : viewModel.PaginaActual;

            bool operacionDetectada = false;

            for (int intento = 0; intento < 240; intento++)
            {
                if (viewModel.IsBusy ||
                    viewModel.PaginaActual != paginaOrigen)
                {
                    operacionDetectada = true;
                }

                if (operacionDetectada &&
                    !viewModel.IsBusy)
                {
                    if (viewModel.PaginaActual != paginaOrigen &&
                        viewModel.Registros.Count > 0)
                    {
                        await DesplazarRegistrosAlInicioAsync();
                    }

                    paginaAntesCambio = -1;
                    return;
                }

                await Task.Delay(50);
            }

            paginaAntesCambio = -1;
        }

        private async Task DesplazarRegistrosAlInicioAsync()
        {
            if (RegistrosCollection == null ||
                BindingContext is not CatalogoEliminadosViewModel viewModel ||
                viewModel.Registros.Count == 0)
            {
                return;
            }

            // Permite que CollectionView materialice la nueva página.
            await Task.Delay(60);

            RegistrosCollection.ScrollTo(
                0,
                position: ScrollToPosition.Start,
                animate: false);
        }

        /// <summary>
        /// El diseño depende del ancho real disponible. Esto cubre teléfono,
        /// tablet y también una ventana Windows estrecha sin depender solo de
        /// OnIdiom Desktop.
        /// </summary>
        private void AplicarDisenoResponsivo()
        {
            ResolverAccionesBusqueda();
            AplicarColumnas();
            AplicarAccionesBusqueda();
            AplicarPaginacion();
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

        private void AplicarPaginacion()
        {
            if (PaginacionEliminados == null)
                return;

            double ancho =
                RegistrosCollection.Width > 0
                    ? RegistrosCollection.Width
                    : Width;

            if (ancho <= 0)
                return;

            double margenHorizontal =
                ancho < 480
                    ? 8
                    : ancho < 800
                        ? 20
                        : 32;

            double anchoDisponible =
                Math.Max(0, ancho - margenHorizontal);

            PaginacionEliminados.WidthRequest =
                Math.Min(560, anchoDisponible);
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

                buscarButton.HorizontalOptions = LayoutOptions.Fill;
                limpiarButton.HorizontalOptions = LayoutOptions.Fill;
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

                buscarButton.HorizontalOptions = LayoutOptions.End;
                limpiarButton.HorizontalOptions = LayoutOptions.End;
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
                ResponsiveLayoutUtility.FindAncestor<Grid>(buscar);

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
