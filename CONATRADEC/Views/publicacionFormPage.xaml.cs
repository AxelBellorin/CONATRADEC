using CONATRADEC.Controls;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.Views
{
    public partial class publicacionFormPage : ContentPage, IQueryAttributable
    {
        private const double PrincipalHorizontalBreakpoint = 980;
        private const double PublicacionEventoHorizontalBreakpoint = 1080;
        private const double CamposHorizontalBreakpoint = 640;
        private const double EnlaceHorizontalBreakpoint = 720;
        private const double EncabezadoHorizontalBreakpoint = 680;

        private readonly PublicacionFormViewModel viewModel = new();
        private readonly Dictionary<Grid, bool> modosCompactos = new();
        private readonly HashSet<Grid> gridsObservados = new();

        private int publicacionId;
        private bool controlesResponsivosResueltos;

        private Grid? encabezadoGrid;
        private View? encabezadoTexto;
        private Button? encabezadoGuardar;

        private Grid? principalGrid;
        private Border? informacionPrincipalCard;
        private Border? portadaCard;
        private Border? portadaPreview;

        private Grid? publicacionEventoGrid;
        private Border? publicacionCard;
        private Border? eventoCard;

        private Grid? enlaceGrid;
        private View? enlaceDireccionSection;
        private View? enlaceTextoSection;

        private readonly List<(Grid Grid, View First, View Second)>
            paresCampos = new();

        public publicacionFormPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;

            Loaded += OnPaginaLoaded;
            SizeChanged += OnPaginaSizeChanged;
        }

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            if (query.TryGetValue("PublicacionId", out object? value) &&
                int.TryParse(value?.ToString(), out int id))
            {
                publicacionId = Math.Max(0, id);
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            AplicarDisenoResponsivo();
            viewModel.ActualizarPermisos();

            bool puedeAcceder =
                viewModel.CanView &&
                (publicacionId > 0
                    ? viewModel.CanEdit
                    : viewModel.CanAdd);

            ContenidoPrincipal.IsVisible = puedeAcceder;
            ContenidoSinPermiso.IsVisible = !puedeAcceder;

            if (puedeAcceder)
            {
                await viewModel.InicializarAsync(publicacionId);
                AplicarDisenoResponsivo();
            }
        }

        protected override void OnDisappearing()
        {
            try
            {
                viewModel.CancelarCarga();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"No fue posible cancelar la operación del formulario de publicación: {ex}");
            }

            base.OnDisappearing();
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

        private void OnSubLayoutSizeChanged(
            object? sender,
            EventArgs e)
        {
            AplicarDisenoResponsivo();
        }

        /// <summary>
        /// Reorganiza las tarjetas y los pares de campos según el ancho real
        /// disponible. Esto evita que WinUI mantenga dos columnas únicamente
        /// por ser Desktop cuando la ventana ya no tiene espacio suficiente.
        /// </summary>
        private void AplicarDisenoResponsivo()
        {
            ResolverControlesResponsivos();

            AplicarEncabezado();
            AplicarPrincipalYPortada();
            AplicarPublicacionYEvento();
            AplicarCamposInternos();
            AplicarEnlace();
            AplicarAlturaPortada();
        }

        private void AplicarEncabezado()
        {
            if (encabezadoGrid == null ||
                encabezadoTexto == null ||
                encabezadoGuardar == null)
            {
                return;
            }

            double ancho = ObtenerAncho(encabezadoGrid);

            if (ancho <= 0)
                return;

            bool compacto =
                ancho < EncabezadoHorizontalBreakpoint;

            AplicarPar(
                encabezadoGrid,
                encabezadoTexto,
                encabezadoGuardar,
                compacto,
                GridLength.Star,
                GridLength.Auto);

            encabezadoGuardar.HorizontalOptions =
                compacto
                    ? LayoutOptions.Fill
                    : LayoutOptions.End;

            encabezadoGuardar.MinimumWidthRequest =
                compacto ? 0 : 170;
        }

        private void AplicarPrincipalYPortada()
        {
            if (principalGrid == null ||
                informacionPrincipalCard == null ||
                portadaCard == null)
            {
                return;
            }

            double ancho = ObtenerAncho(principalGrid);

            if (ancho <= 0)
                return;

            bool compacto =
                ancho < PrincipalHorizontalBreakpoint;

            AplicarPar(
                principalGrid,
                informacionPrincipalCard,
                portadaCard,
                compacto,
                new GridLength(1.25, GridUnitType.Star),
                new GridLength(0.75, GridUnitType.Star));
        }

        private void AplicarPublicacionYEvento()
        {
            if (publicacionEventoGrid == null ||
                publicacionCard == null ||
                eventoCard == null)
            {
                return;
            }

            double ancho = ObtenerAncho(publicacionEventoGrid);

            if (ancho <= 0)
                return;

            bool compacto =
                ancho < PublicacionEventoHorizontalBreakpoint;

            AplicarPar(
                publicacionEventoGrid,
                publicacionCard,
                eventoCard,
                compacto,
                GridLength.Star,
                GridLength.Star);
        }

        private void AplicarCamposInternos()
        {
            foreach ((Grid grid, View first, View second) in paresCampos)
            {
                double ancho = ObtenerAncho(grid);

                if (ancho <= 0)
                    continue;

                bool compacto =
                    ancho < CamposHorizontalBreakpoint;

                AplicarPar(
                    grid,
                    first,
                    second,
                    compacto,
                    GridLength.Star,
                    GridLength.Star);
            }
        }

        private void AplicarEnlace()
        {
            if (enlaceGrid == null ||
                enlaceDireccionSection == null ||
                enlaceTextoSection == null)
            {
                return;
            }

            double ancho = ObtenerAncho(enlaceGrid);

            if (ancho <= 0)
                return;

            bool compacto =
                ancho < EnlaceHorizontalBreakpoint;

            AplicarPar(
                enlaceGrid,
                enlaceDireccionSection,
                enlaceTextoSection,
                compacto,
                new GridLength(1.4, GridUnitType.Star),
                new GridLength(0.6, GridUnitType.Star));
        }

        private void AplicarAlturaPortada()
        {
            if (portadaPreview == null || portadaCard == null)
                return;

            double ancho =
                portadaCard.Width > 0
                    ? portadaCard.Width
                    : Width;

            if (ancho <= 0)
                return;

            double altura =
                ancho < 560
                    ? 220
                    : ancho < 820
                        ? 260
                        : 310;

            if (Math.Abs(
                    portadaPreview.HeightRequest -
                    altura) > 0.5)
            {
                portadaPreview.HeightRequest = altura;
            }
        }

        private void AplicarPar(
            Grid grid,
            View first,
            View second,
            bool compacto,
            GridLength firstWidth,
            GridLength secondWidth)
        {
            if (modosCompactos.TryGetValue(
                    grid,
                    out bool modoActual) &&
                modoActual == compacto)
            {
                return;
            }

            modosCompactos[grid] = compacto;

            if (compacto)
            {
                ResponsiveLayoutUtility.ConfigureStackedPair(
                    grid,
                    first,
                    second);
            }
            else
            {
                ResponsiveLayoutUtility.ConfigureHorizontalPair(
                    grid,
                    first,
                    second,
                    firstWidth,
                    secondWidth);
            }

            grid.InvalidateMeasure();
        }

        private void ResolverControlesResponsivos()
        {
            if (controlesResponsivosResueltos)
                return;

            ResolverEncabezado();
            ResolverPrincipalYPortada();
            ResolverPublicacionYEvento();
            ResolverEnlace();
            ResolverCamposInternos();

            controlesResponsivosResueltos =
                encabezadoGrid != null &&
                principalGrid != null &&
                publicacionEventoGrid != null &&
                enlaceGrid != null;
        }

        private void ResolverEncabezado()
        {
            Label? subtitulo =
                ResponsiveLayoutUtility.FindDescendant<Label>(
                    this,
                    label =>
                        string.Equals(
                            label.Text?.Trim(),
                            "Prepare el contenido, vigencia, portada y datos opcionales del evento.",
                            StringComparison.Ordinal));

            if (subtitulo == null)
                return;

            Grid? grid =
                ResponsiveLayoutUtility.FindAncestor<Grid>(
                    subtitulo);

            if (grid == null)
                return;

            View? texto =
                ResponsiveLayoutUtility.FindDirectChildContaining(
                    grid,
                    subtitulo);

            Button? guardar =
                grid.Children
                    .OfType<Button>()
                    .FirstOrDefault();

            if (texto == null || guardar == null)
                return;

            encabezadoGrid = grid;
            encabezadoTexto = texto;
            encabezadoGuardar = guardar;
            ObservarGrid(grid);
        }

        private void ResolverPrincipalYPortada()
        {
            Border? informacion =
                ResponsiveLayoutUtility.FindSectionCard(
                    this,
                    "Información principal");

            Border? portada =
                ResponsiveLayoutUtility.FindSectionCard(
                    this,
                    "Imagen de portada");

            if (informacion == null ||
                portada == null ||
                informacion.Parent is not Grid grid ||
                !ReferenceEquals(portada.Parent, grid))
            {
                return;
            }

            principalGrid = grid;
            informacionPrincipalCard = informacion;
            portadaCard = portada;
            ObservarGrid(grid);

            Label? placeholder =
                ResponsiveLayoutUtility.FindDescendant<Label>(
                    portada,
                    label =>
                        string.Equals(
                            label.Text?.Trim(),
                            "PORTADA",
                            StringComparison.OrdinalIgnoreCase));

            if (placeholder != null)
            {
                portadaPreview =
                    ResponsiveLayoutUtility.FindAncestor<Border>(
                        placeholder);
            }
        }

        private void ResolverPublicacionYEvento()
        {
            Border? publicacion =
                ResponsiveLayoutUtility.FindSectionCard(
                    this,
                    "Publicación y vigencia");

            if (publicacion == null ||
                publicacion.Parent is not Grid grid)
            {
                return;
            }

            Border? evento =
                grid.Children
                    .OfType<Border>()
                    .FirstOrDefault(
                        border =>
                            !ReferenceEquals(
                                border,
                                publicacion));

            if (evento == null)
                return;

            publicacionEventoGrid = grid;
            publicacionCard = publicacion;
            eventoCard = evento;
            ObservarGrid(grid);
        }

        private void ResolverEnlace()
        {
            Grid? grid =
                ResponsiveLayoutUtility.FindNearestGridByLabel(
                    this,
                    "Dirección web");

            if (grid == null)
                return;

            Label? direccionLabel =
                ResponsiveLayoutUtility.FindDescendant<Label>(
                    grid,
                    label =>
                        string.Equals(
                            label.Text?.Trim(),
                            "Dirección web",
                            StringComparison.OrdinalIgnoreCase));

            Label? textoLabel =
                ResponsiveLayoutUtility.FindDescendant<Label>(
                    grid,
                    label =>
                        string.Equals(
                            label.Text?.Trim(),
                            "Texto del botón",
                            StringComparison.OrdinalIgnoreCase));

            if (direccionLabel == null || textoLabel == null)
                return;

            View? direccion =
                ResponsiveLayoutUtility.FindDirectChildContaining(
                    grid,
                    direccionLabel);

            View? texto =
                ResponsiveLayoutUtility.FindDirectChildContaining(
                    grid,
                    textoLabel);

            if (direccion == null || texto == null)
                return;

            enlaceGrid = grid;
            enlaceDireccionSection = direccion;
            enlaceTextoSection = texto;
            ObservarGrid(grid);
        }

        private void ResolverCamposInternos()
        {
            if (paresCampos.Count > 0)
                return;

            string[] etiquetas =
            {
                "Fecha de inicio *",
                "Fecha final",
                "Estado",
                "Fecha de inicio del evento",
                "Fecha final del evento"
            };

            var gridsAgregados = new HashSet<Grid>();

            foreach (string etiqueta in etiquetas)
            {
                Grid? grid =
                    ResponsiveLayoutUtility.FindNearestGridByLabel(
                        this,
                        etiqueta);

                if (grid == null ||
                    !gridsAgregados.Add(grid))
                {
                    continue;
                }

                List<View> children =
                    grid.Children
                        .OfType<View>()
                        .ToList();

                if (children.Count != 2)
                    continue;

                paresCampos.Add(
                    (grid, children[0], children[1]));
                ObservarGrid(grid);
            }

            Button? seleccionarImagen =
                ResponsiveLayoutUtility.FindDescendant<Button>(
                    this,
                    button =>
                        string.Equals(
                            button.Text?.Trim(),
                            "Seleccionar imagen",
                            StringComparison.OrdinalIgnoreCase));

            if (seleccionarImagen != null)
            {
                Grid? grid =
                    ResponsiveLayoutUtility.FindAncestor<Grid>(
                        seleccionarImagen);

                if (grid != null &&
                    gridsAgregados.Add(grid))
                {
                    List<View> children =
                        grid.Children
                            .OfType<View>()
                            .ToList();

                    if (children.Count == 2)
                    {
                        paresCampos.Add(
                            (grid, children[0], children[1]));
                        ObservarGrid(grid);
                    }
                }
            }
        }

        private void ObservarGrid(Grid grid)
        {
            if (!gridsObservados.Add(grid))
                return;

            grid.SizeChanged += OnSubLayoutSizeChanged;
        }

        private static double ObtenerAncho(Grid grid) =>
            grid.Width > 0
                ? grid.Width
                : 0;

        private async void OnSeleccionarPortadaClicked(
            object sender,
            EventArgs e)
        {
            if (viewModel.IsBusy)
                return;

            try
            {
                FileResult? archivo = await FilePicker.Default.PickAsync(
                    new PickOptions
                    {
                        PickerTitle = "Seleccione la portada",
                        FileTypes = FilePickerFileType.Images
                    });

                if (archivo != null)
                    await viewModel.SeleccionarPortadaAsync(archivo);
            }
            catch
            {
                await DisplayAlert(
                    "Imagen",
                    "No fue posible abrir el selector de imágenes.",
                    "Aceptar");
            }
        }
    }
}
