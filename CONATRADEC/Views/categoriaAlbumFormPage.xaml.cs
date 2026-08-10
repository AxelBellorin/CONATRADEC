using CONATRADEC.Controls;
using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using static CONATRADEC.Models.FormMode;
using System;
using System.Linq;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(Item), "Item")]
    public partial class categoriaAlbumFormPage :
        ContentPage
    {
        private const double PortadaHorizontalBreakpoint = 820;

        private readonly CategoriaAlbumFormViewModel
            viewModel = new();

        private Grid? portadaGrid;
        private Border? portadaPreview;
        private VerticalStackLayout? portadaInfo;
        private bool? portadaCompacta;
        private bool portadaGridObservado;

        public FormModeSelect Mode
        {
            set => viewModel.Mode = value;
        }

        public CategoriaAlbumBotanicoRequest Item
        {
            set => viewModel.Item = value;
        }

        public categoriaAlbumFormPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;
            BindingContext = viewModel;

            Loaded += OnPaginaLoaded;
            SizeChanged += OnPaginaSizeChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            AplicarDisenoResponsivo();
            viewModel.ActualizarPermisos();

            bool denied =
                !viewModel.CanView ||
                (
                    viewModel.Mode ==
                    FormModeSelect.Create &&
                    !viewModel.CanAdd
                ) ||
                (
                    viewModel.Mode ==
                    FormModeSelect.Edit &&
                    !viewModel.CanEdit
                );

            if (!denied)
                return;

            await DisplayAlert(
                "Permiso denegado",
                "No tiene permisos para realizar esta operación.",
                "Aceptar");

            await Shell.Current.GoToAsync(
                AppRoutes.Regresar,
                false);
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

        /// <summary>
        /// La portada se reorganiza con el ancho real disponible y no con
        /// DeviceInfo.Idiom. De esta forma una ventana estrecha de Windows
        /// utiliza el mismo diseño apilado que una pantalla móvil.
        /// </summary>
        private void AplicarDisenoResponsivo()
        {
            ResolverControlesPortada();

            if (portadaGrid == null ||
                portadaPreview == null ||
                portadaInfo == null)
            {
                return;
            }

            double anchoDisponible =
                portadaGrid.Width > 0
                    ? portadaGrid.Width
                    : Width;

            if (anchoDisponible <= 0)
                return;

            bool compacta =
                anchoDisponible <
                PortadaHorizontalBreakpoint;

            if (portadaCompacta == compacta)
                return;

            portadaCompacta = compacta;

            if (compacta)
            {
                ResponsiveLayoutUtility.ConfigureStackedPair(
                    portadaGrid,
                    portadaPreview,
                    portadaInfo);

                portadaPreview.HeightRequest = 210;
                portadaPreview.MinimumHeightRequest = 210;
                portadaPreview.VerticalOptions =
                    LayoutOptions.Fill;
            }
            else
            {
                ResponsiveLayoutUtility.ConfigureHorizontalPair(
                    portadaGrid,
                    portadaPreview,
                    portadaInfo,
                    new GridLength(0.9, GridUnitType.Star),
                    new GridLength(1.1, GridUnitType.Star));

                /*
                 * La fila toma la altura del contenido más alto. La imagen
                 * puede crecer con su tarjeta en lugar de imponer 250 px al
                 * contenedor completo.
                 */
                portadaPreview.HeightRequest = -1;
                portadaPreview.MinimumHeightRequest = 250;
                portadaPreview.VerticalOptions =
                    LayoutOptions.Fill;
            }

            portadaGrid.InvalidateMeasure();
        }

        private void ResolverControlesPortada()
        {
            if (portadaGrid != null &&
                portadaPreview != null &&
                portadaInfo != null)
            {
                return;
            }

            Label? tituloPortada =
                ResponsiveLayoutUtility.FindDescendant<Label>(
                    this,
                    label =>
                        string.Equals(
                            label.Text?.Trim(),
                            "Imagen de portada",
                            StringComparison.OrdinalIgnoreCase));

            if (tituloPortada == null)
                return;

            Grid? grid =
                ResponsiveLayoutUtility.FindAncestor<Grid>(
                    tituloPortada);

            if (grid == null)
                return;

            View? info =
                ResponsiveLayoutUtility.FindDirectChildContaining(
                    grid,
                    tituloPortada);

            Border? preview =
                grid.Children
                    .OfType<Border>()
                    .FirstOrDefault(
                        border =>
                            !ResponsiveLayoutUtility.Contains(
                                border,
                                tituloPortada));

            if (info is not VerticalStackLayout infoLayout ||
                preview == null)
            {
                return;
            }

            portadaGrid = grid;
            portadaPreview = preview;
            portadaInfo = infoLayout;

            if (!portadaGridObservado)
            {
                portadaGridObservado = true;
                portadaGrid.SizeChanged += OnPaginaSizeChanged;
            }
        }
    }
}
