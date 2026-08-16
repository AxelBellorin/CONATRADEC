using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;

namespace CONATRADEC.Views
{
    public partial class fuenteNutrienteFormPage :
        ContentPage,
        IQueryAttributable
    {
        private readonly FuenteNutrienteFormViewModel
            viewModel = new();

        private bool parametrosRecibidos;
        private bool parametrosValidos;
        private bool errorParametrosMostrado;
        private bool inicializacionSolicitada;
        private bool datosBasicosCompactos;
        private bool enmiendaCompacta;
        private bool accionesCompactas;

        public fuenteNutrienteFormPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;
        }

        /// <summary>
        /// Recibe el contexto completo de navegación en una sola operación.
        /// Se mantiene compatibilidad con los parámetros históricos Mode/Fuente
        /// para no romper consumidores internos que todavía los utilicen.
        /// </summary>
        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            parametrosRecibidos = true;
            parametrosValidos = false;
            inicializacionSolicitada = false;

            FuenteNutrienteFormNavigationContext? contexto =
                query.TryGetValue(
                    "ContextoFuente",
                    out object? contextoValue) &&
                contextoValue is FuenteNutrienteFormNavigationContext recibido
                    ? recibido
                    : CrearContextoHistorico(query);

            parametrosValidos =
                viewModel.AplicarContexto(contexto);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();
            AjustarDiseno(Width);

            if ((!parametrosRecibidos ||
                 !parametrosValidos) &&
                !errorParametrosMostrado)
            {
                errorParametrosMostrado = true;

                await DisplayAlert(
                    "Fuente de nutriente",
                    "No se recibió una fuente de nutriente válida para abrir este formulario.",
                    "Aceptar");

                await viewModel.GoToAsyncParameters(
                    "//FuenteNutrientePage");

                return;
            }

            if (!inicializacionSolicitada)
            {
                inicializacionSolicitada = true;
                await viewModel.InitializeAsync();
            }
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarOperaciones();
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarDiseno(width);
        }

        private void AjustarDiseno(double anchoPagina)
        {
            if (anchoPagina <= 0 ||
                FormularioContainer == null)
            {
                return;
            }

            AjustarContenedor(anchoPagina);
            AjustarDatosBasicos(anchoPagina);
            AjustarEnmienda(anchoPagina);
            AjustarAcciones(anchoPagina);
        }

        private void AjustarContenedor(double anchoPagina)
        {
            FormularioContainer.Padding =
                anchoPagina < 600
                    ? new Thickness(14, 14, 14, 24)
                    : anchoPagina < 900
                        ? new Thickness(22, 18, 22, 28)
                        : new Thickness(28, 22, 28, 32);

            double margenHorizontal =
                anchoPagina < 600
                    ? 28
                    : DeviceInfo.Platform == DevicePlatform.WinUI
                        ? 72
                        : 44;

            double anchoDisponible =
                Math.Max(
                    280,
                    anchoPagina - margenHorizontal);

            FormularioContainer.WidthRequest =
                Math.Min(
                    anchoDisponible,
                    1100);
        }

        private void AjustarDatosBasicos(double anchoPagina)
        {
            if (DatosBasicosGrid == null ||
                NombreSection == null ||
                PrecioSection == null ||
                DescripcionSection == null)
            {
                return;
            }

            bool compacto =
                ObtenerAnchoUtil(anchoPagina) < 700;

            if (datosBasicosCompactos == compacto &&
                DatosBasicosGrid.ColumnDefinitions.Count ==
                    (compacto ? 1 : 2))
            {
                return;
            }

            datosBasicosCompactos = compacto;
            DatosBasicosGrid.ColumnDefinitions.Clear();
            DatosBasicosGrid.RowDefinitions.Clear();

            if (compacto)
            {
                DatosBasicosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                DatosBasicosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                DatosBasicosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                DatosBasicosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(NombreSection, 0);
                Grid.SetColumn(NombreSection, 0);
                Grid.SetColumnSpan(NombreSection, 1);
                Grid.SetRow(PrecioSection, 1);
                Grid.SetColumn(PrecioSection, 0);
                Grid.SetColumnSpan(PrecioSection, 1);
                Grid.SetRow(DescripcionSection, 2);
                Grid.SetColumn(DescripcionSection, 0);
                Grid.SetColumnSpan(DescripcionSection, 1);
            }
            else
            {
                DatosBasicosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        new GridLength(2, GridUnitType.Star)));
                DatosBasicosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        new GridLength(1, GridUnitType.Star)));
                DatosBasicosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                DatosBasicosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(NombreSection, 0);
                Grid.SetColumn(NombreSection, 0);
                Grid.SetColumnSpan(NombreSection, 1);
                Grid.SetRow(PrecioSection, 0);
                Grid.SetColumn(PrecioSection, 1);
                Grid.SetColumnSpan(PrecioSection, 1);
                Grid.SetRow(DescripcionSection, 1);
                Grid.SetColumn(DescripcionSection, 0);
                Grid.SetColumnSpan(DescripcionSection, 2);
            }

            DatosBasicosGrid.InvalidateMeasure();
        }

        private void AjustarEnmienda(double anchoPagina)
        {
            if (EnmiendaDatosGrid == null ||
                PrntSection == null ||
                DescripcionParametroSection == null)
            {
                return;
            }

            bool compacto =
                ObtenerAnchoUtil(anchoPagina) < 760;

            if (enmiendaCompacta == compacto &&
                EnmiendaDatosGrid.ColumnDefinitions.Count ==
                    (compacto ? 1 : 2))
            {
                return;
            }

            enmiendaCompacta = compacto;
            EnmiendaDatosGrid.ColumnDefinitions.Clear();
            EnmiendaDatosGrid.RowDefinitions.Clear();

            if (compacto)
            {
                EnmiendaDatosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                EnmiendaDatosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                EnmiendaDatosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(PrntSection, 0);
                Grid.SetColumn(PrntSection, 0);
                Grid.SetColumnSpan(PrntSection, 1);
                Grid.SetRow(DescripcionParametroSection, 1);
                Grid.SetColumn(DescripcionParametroSection, 0);
                Grid.SetColumnSpan(DescripcionParametroSection, 1);
            }
            else
            {
                EnmiendaDatosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        new GridLength(1, GridUnitType.Star)));
                EnmiendaDatosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        new GridLength(2, GridUnitType.Star)));
                EnmiendaDatosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(PrntSection, 0);
                Grid.SetColumn(PrntSection, 0);
                Grid.SetColumnSpan(PrntSection, 1);
                Grid.SetRow(DescripcionParametroSection, 0);
                Grid.SetColumn(DescripcionParametroSection, 1);
                Grid.SetColumnSpan(DescripcionParametroSection, 1);
            }

            EnmiendaDatosGrid.InvalidateMeasure();
        }

        private void AjustarAcciones(double anchoPagina)
        {
            if (AccionesGrid == null ||
                GuardarButton == null ||
                CancelarButton == null ||
                RegresarButton == null)
            {
                return;
            }

            bool compacto =
                ObtenerAnchoUtil(anchoPagina) < 560;

            if (accionesCompactas == compacto &&
                AccionesGrid.ColumnDefinitions.Count ==
                    (compacto ? 1 : 2))
            {
                return;
            }

            accionesCompactas = compacto;
            AccionesGrid.ColumnDefinitions.Clear();
            AccionesGrid.RowDefinitions.Clear();

            if (compacto)
            {
                AccionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                AccionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                AccionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(GuardarButton, 0);
                Grid.SetColumn(GuardarButton, 0);
                Grid.SetColumnSpan(GuardarButton, 1);
                Grid.SetRow(CancelarButton, 1);
                Grid.SetColumn(CancelarButton, 0);
                Grid.SetColumnSpan(CancelarButton, 1);
                Grid.SetRow(RegresarButton, 0);
                Grid.SetColumn(RegresarButton, 0);
                Grid.SetColumnSpan(RegresarButton, 1);
            }
            else
            {
                AccionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                AccionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                AccionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(GuardarButton, 0);
                Grid.SetColumn(GuardarButton, 0);
                Grid.SetColumnSpan(GuardarButton, 1);
                Grid.SetRow(CancelarButton, 0);
                Grid.SetColumn(CancelarButton, 1);
                Grid.SetColumnSpan(CancelarButton, 1);
                Grid.SetRow(RegresarButton, 0);
                Grid.SetColumn(RegresarButton, 0);
                Grid.SetColumnSpan(RegresarButton, 2);
            }

            AccionesGrid.InvalidateMeasure();
        }

        private double ObtenerAnchoUtil(double anchoPagina)
        {
            double anchoFormulario =
                FormularioContainer.Width;

            return anchoFormulario > 0
                ? anchoFormulario
                : anchoPagina;
        }

        private static FuenteNutrienteFormNavigationContext?
            CrearContextoHistorico(
                IDictionary<string, object> query)
        {
            if (!query.TryGetValue(
                    "Mode",
                    out object? modoValor) ||
                !TryObtenerModo(
                    modoValor,
                    out FormMode.FormModeSelect modo))
            {
                return null;
            }

            FuenteNutrienteRequest fuente =
                query.TryGetValue(
                    "Fuente",
                    out object? fuenteValor) &&
                fuenteValor is FuenteNutrienteRequest recibida
                    ? recibida
                    : new FuenteNutrienteRequest();

            return new FuenteNutrienteFormNavigationContext
            {
                Mode = modo,
                Fuente = fuente
            };
        }

        private static bool TryObtenerModo(
            object? valor,
            out FormMode.FormModeSelect mode)
        {
            if (valor is FormMode.FormModeSelect directo)
            {
                mode = directo;
                return true;
            }

            if (valor != null &&
                Enum.TryParse(
                    valor.ToString(),
                    ignoreCase: true,
                    out mode))
            {
                return true;
            }

            mode = FormMode.FormModeSelect.Create;
            return false;
        }
    }
}
