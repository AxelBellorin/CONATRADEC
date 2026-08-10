using CONATRADEC.Controls;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(RegistroId), "RegistroId")]
    public partial class albumFotosAdminPage : ContentPage
    {
        private const double FormularioHorizontalBreakpoint = 900;
        private const double OpcionesHorizontalBreakpoint = 620;
        private const double DosColumnasBreakpoint = 720;
        private const double TresColumnasBreakpoint = 1180;

        private readonly AlbumFotosAdminViewModel
            viewModel = new();

        private bool regresando;
        private int spanGaleriaActual = -1;
        private bool? formularioCompacto;
        private bool? opcionesCompactas;

        private Grid? formularioCargaGrid;
        private Border? panelImagen;
        private VerticalStackLayout? panelDatos;
        private Grid? ordenPortadaGrid;
        private View? ordenSection;
        private Border? portadaSection;
        private readonly HashSet<Grid> gridsObservados = new();

        public int RegistroId
        {
            set => viewModel.Id = value;
        }

        public albumFotosAdminPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;

            Loaded += OnPaginaLoaded;
            SizeChanged += OnPaginaSizeChanged;
            FotosCollectionView.SizeChanged +=
                OnFotosCollectionViewSizeChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            AplicarDisenoResponsivo();
            viewModel.ActualizarPermisos();

            bool denied =
                !viewModel.CanView ||
                (
                    !viewModel.CanAdd &&
                    !viewModel.CanEdit &&
                    !viewModel.CanDelete
                );

            if (denied)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para administrar fotografías.",
                    "Aceptar");

                await Shell.Current.GoToAsync(
                    AppRoutes.Regresar,
                    false);

                return;
            }

            await viewModel.LoadAsync(true);
            AplicarDisenoResponsivo();
            await RestablecerAlInicioAsync();
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

        private void OnFotosCollectionViewSizeChanged(
            object? sender,
            EventArgs e)
        {
            AplicarColumnasGaleria();
            AplicarDisenoFormulario();
        }

        /// <summary>
        /// Ajusta la galería y el formulario con el ancho real disponible.
        /// Así WinUI puede pasar de 3 a 2 o 1 columna al reducir la ventana,
        /// mientras Android conserva una composición adecuada a su viewport.
        /// </summary>
        private void AplicarDisenoResponsivo()
        {
            ResolverControlesResponsivos();
            AplicarColumnasGaleria();
            AplicarDisenoFormulario();
            AplicarDisenoOpciones();
        }

        private void AplicarColumnasGaleria()
        {
            if (FotosCollectionView.ItemsLayout
                    is not GridItemsLayout layout)
            {
                return;
            }

            double ancho = FotosCollectionView.Width;

            if (ancho <= 0)
                return;

            int span =
                ancho >= TresColumnasBreakpoint
                    ? 3
                    : ancho >= DosColumnasBreakpoint
                        ? 2
                        : 1;

            if (spanGaleriaActual == span &&
                layout.Span == span)
            {
                return;
            }

            spanGaleriaActual = span;
            layout.Span = span;
        }

        private void AplicarDisenoFormulario()
        {
            if (formularioCargaGrid == null ||
                panelImagen == null ||
                panelDatos == null)
            {
                return;
            }

            double ancho =
                formularioCargaGrid.Width > 0
                    ? formularioCargaGrid.Width
                    : FotosCollectionView.Width;

            if (ancho <= 0)
                return;

            bool compacto =
                ancho < FormularioHorizontalBreakpoint;

            if (formularioCompacto == compacto)
                return;

            formularioCompacto = compacto;

            if (compacto)
            {
                ResponsiveLayoutUtility.ConfigureStackedPair(
                    formularioCargaGrid,
                    panelImagen,
                    panelDatos);
            }
            else
            {
                ResponsiveLayoutUtility.ConfigureHorizontalPair(
                    formularioCargaGrid,
                    panelImagen,
                    panelDatos,
                    new GridLength(0.85, GridUnitType.Star),
                    new GridLength(1.15, GridUnitType.Star));
            }

            if (panelImagen.Content is Grid previewGrid)
            {
                previewGrid.MinimumHeightRequest =
                    compacto ? 250 : 315;
            }

            formularioCargaGrid.InvalidateMeasure();
        }

        private void AplicarDisenoOpciones()
        {
            if (ordenPortadaGrid == null ||
                ordenSection == null ||
                portadaSection == null)
            {
                return;
            }

            double ancho =
                ordenPortadaGrid.Width > 0
                    ? ordenPortadaGrid.Width
                    : formularioCargaGrid?.Width ?? Width;

            if (ancho <= 0)
                return;

            bool compacto =
                ancho < OpcionesHorizontalBreakpoint;

            if (opcionesCompactas == compacto)
                return;

            opcionesCompactas = compacto;

            if (compacto)
            {
                ResponsiveLayoutUtility.ConfigureStackedPair(
                    ordenPortadaGrid,
                    ordenSection,
                    portadaSection);
            }
            else
            {
                ResponsiveLayoutUtility.ConfigureHorizontalPair(
                    ordenPortadaGrid,
                    ordenSection,
                    portadaSection,
                    new GridLength(0.7, GridUnitType.Star),
                    new GridLength(1.3, GridUnitType.Star));
            }

            ordenPortadaGrid.InvalidateMeasure();
        }

        private void ResolverControlesResponsivos()
        {
            if (formularioCargaGrid == null)
            {
                Label? titulo =
                    ResponsiveLayoutUtility.FindDescendant<Label>(
                        this,
                        label =>
                            string.Equals(
                                label.Text?.Trim(),
                                "Nueva fotografía",
                                StringComparison.OrdinalIgnoreCase));

                if (titulo != null)
                {
                    Grid? grid =
                        ResponsiveLayoutUtility.FindAncestor<Grid>(
                            titulo);

                    if (grid != null)
                    {
                        View? datos =
                            ResponsiveLayoutUtility
                                .FindDirectChildContaining(
                                    grid,
                                    titulo);

                        Border? imagen =
                            grid.Children
                                .OfType<Border>()
                                .FirstOrDefault(
                                    border =>
                                        !ResponsiveLayoutUtility.Contains(
                                            border,
                                            titulo));

                        if (datos is VerticalStackLayout datosLayout &&
                            imagen != null)
                        {
                            formularioCargaGrid = grid;
                            panelImagen = imagen;
                            panelDatos = datosLayout;
                            ObservarGrid(grid);
                        }
                    }
                }
            }

            if (ordenPortadaGrid == null)
            {
                Label? ordenLabel =
                    ResponsiveLayoutUtility.FindDescendant<Label>(
                        this,
                        label =>
                            string.Equals(
                                label.Text?.Trim(),
                                "Orden",
                                StringComparison.OrdinalIgnoreCase));

                if (ordenLabel != null)
                {
                    Grid? grid =
                        ResponsiveLayoutUtility.FindAncestor<Grid>(
                            ordenLabel);

                    if (grid != null)
                    {
                        View? orden =
                            ResponsiveLayoutUtility
                                .FindDirectChildContaining(
                                    grid,
                                    ordenLabel);

                        Border? portada =
                            grid.Children
                                .OfType<Border>()
                                .FirstOrDefault();

                        if (orden != null &&
                            portada != null)
                        {
                            ordenPortadaGrid = grid;
                            ordenSection = orden;
                            portadaSection = portada;
                            ObservarGrid(grid);
                        }
                    }
                }
            }
        }

        private void ObservarGrid(Grid grid)
        {
            if (!gridsObservados.Add(grid))
                return;

            grid.SizeChanged += OnPaginaSizeChanged;
        }

        private async void OnRegresarClicked(
            object sender,
            EventArgs e)
        {
            if (regresando || viewModel.IsBusy)
                return;

            regresando = true;
            RegresarButton.IsEnabled = false;

            try
            {
                /*
                 * Retroceso real en la pila.
                 * No crea otra instancia de albumDetallePage.
                 */
                await Shell.Current.GoToAsync(
                    AppRoutes.Regresar,
                    false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"No fue posible regresar desde fotografías: {ex}");

                await DisplayAlert(
                    "No fue posible regresar",
                    "Ocurrió un problema al volver a la pantalla anterior.",
                    "Aceptar");
            }
            finally
            {
                regresando = false;

                if (Handler != null)
                    RegresarButton.IsEnabled = true;
            }
        }

        private async Task RestablecerAlInicioAsync()
        {
            /*
             * WinUI puede enfocar automáticamente un Entry de una tarjeta
             * y desplazar el contenido. El foco queda en el botón fijo.
             */
            await Task.Delay(120);

#if WINDOWS
            await Microsoft.Maui.ApplicationModel.MainThread
                .InvokeOnMainThreadAsync(() =>
                {
                    RegresarButton.Focus();

                    if (FotosCollectionView.Handler?.PlatformView
                        is Microsoft.UI.Xaml.DependencyObject nativeView)
                    {
                        Microsoft.UI.Xaml.Controls.ScrollViewer?
                            scrollViewer =
                                BuscarScrollViewer(nativeView);

                        scrollViewer?.ChangeView(
                            null,
                            0,
                            null,
                            true);
                    }
                });
#endif
        }

#if WINDOWS
        private static Microsoft.UI.Xaml.Controls.ScrollViewer?
            BuscarScrollViewer(
                Microsoft.UI.Xaml.DependencyObject elemento)
        {
            if (elemento
                is Microsoft.UI.Xaml.Controls.ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            int cantidad =
                Microsoft.UI.Xaml.Media.VisualTreeHelper
                    .GetChildrenCount(elemento);

            for (int i = 0; i < cantidad; i++)
            {
                Microsoft.UI.Xaml.DependencyObject hijo =
                    Microsoft.UI.Xaml.Media.VisualTreeHelper
                        .GetChild(elemento, i);

                Microsoft.UI.Xaml.Controls.ScrollViewer?
                    encontrado =
                        BuscarScrollViewer(hijo);

                if (encontrado != null)
                    return encontrado;
            }

            return null;
        }
#endif
    }
}
