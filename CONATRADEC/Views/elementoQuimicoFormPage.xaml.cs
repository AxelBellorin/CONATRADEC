using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;

namespace CONATRADEC.Views
{
    public partial class elementoQuimicoFormPage :
        ContentPage,
        IQueryAttributable
    {
        private readonly ElementoQuimicoFormViewModel
            viewModel = new();

        private bool parametrosRecibidos;
        private bool parametrosValidos;
        private bool errorParametrosMostrado;
        private bool disenoCompacto;

        public elementoQuimicoFormPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;
        }

        /// <summary>
        /// Shell entrega el diccionario completo en una sola llamada. De esta
        /// manera modo y elemento se validan juntos y nunca queda un formulario
        /// Edit/View asociado accidentalmente a un objeto vacío.
        /// </summary>
        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            parametrosRecibidos = true;
            parametrosValidos = false;

            if (!query.TryGetValue(
                    "Mode",
                    out object? modeValue) ||
                !TryObtenerModo(
                    modeValue,
                    out FormMode.FormModeSelect mode))
            {
                return;
            }

            ElementoQuimicoRequest elemento =
                query.TryGetValue(
                    "ElementoQuimico",
                    out object? elementoValue) &&
                elementoValue is ElementoQuimicoRequest recibido
                    ? recibido
                    : new ElementoQuimicoRequest();

            if (mode != FormMode.FormModeSelect.Create &&
                elemento.ElementoQuimicosId is not > 0)
            {
                return;
            }

            viewModel.Configurar(
                mode,
                elemento);

            parametrosValidos = true;
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
                    "Elemento químico",
                    "No se recibió un elemento químico válido para abrir este formulario.",
                    "Aceptar");

                await viewModel
                    .GoToAsyncParameters(
                        AppRoutes.ElementosQuimicos);
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

        private void AjustarDiseno(
            double anchoPagina)
        {
            if (anchoPagina <= 0 ||
                FormularioContainer == null)
            {
                return;
            }

            AjustarPadding(anchoPagina);
            AjustarAnchoFormulario(anchoPagina);
            AjustarCampos(anchoPagina);
            AjustarAcciones(anchoPagina);
        }

        private void AjustarPadding(
            double anchoPagina)
        {
            if (ContenidoFormulario == null)
                return;

            ContenidoFormulario.Padding =
                anchoPagina < 600
                    ? new Thickness(12, 12, 12, 20)
                    : anchoPagina < 900
                        ? new Thickness(20, 18, 20, 26)
                        : new Thickness(28, 22, 28, 30);
        }

        private void AjustarAnchoFormulario(
            double anchoPagina)
        {
            double margenHorizontal =
                anchoPagina < 600
                    ? 24
                    : DeviceInfo.Platform == DevicePlatform.WinUI
                        ? 72
                        : 40;

            double anchoDisponible =
                Math.Max(
                    280,
                    anchoPagina - margenHorizontal);

            FormularioContainer.WidthRequest =
                Math.Min(
                    anchoDisponible,
                    1000);
        }

        private void AjustarCampos(
            double anchoPagina)
        {
            if (CamposGrid == null ||
                CampoSimbolo == null ||
                CampoNombre == null ||
                CampoPeso == null)
            {
                return;
            }

            double anchoUtil =
                FormularioContainer.Width > 0
                    ? FormularioContainer.Width
                    : anchoPagina;

            bool compacto =
                anchoUtil < 700;

            if (disenoCompacto == compacto &&
                CamposGrid.ColumnDefinitions.Count ==
                    (compacto ? 1 : 2))
            {
                return;
            }

            disenoCompacto = compacto;

            CamposGrid.ColumnDefinitions.Clear();
            CamposGrid.RowDefinitions.Clear();

            if (compacto)
            {
                CamposGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                CamposGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                CamposGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                CamposGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(CampoSimbolo, 0);
                Grid.SetColumn(CampoSimbolo, 0);
                Grid.SetColumnSpan(CampoSimbolo, 1);

                Grid.SetRow(CampoNombre, 1);
                Grid.SetColumn(CampoNombre, 0);
                Grid.SetColumnSpan(CampoNombre, 1);

                Grid.SetRow(CampoPeso, 2);
                Grid.SetColumn(CampoPeso, 0);
                Grid.SetColumnSpan(CampoPeso, 1);
            }
            else
            {
                CamposGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                CamposGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                CamposGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                CamposGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(CampoSimbolo, 0);
                Grid.SetColumn(CampoSimbolo, 0);
                Grid.SetColumnSpan(CampoSimbolo, 1);

                Grid.SetRow(CampoNombre, 0);
                Grid.SetColumn(CampoNombre, 1);
                Grid.SetColumnSpan(CampoNombre, 1);

                Grid.SetRow(CampoPeso, 1);
                Grid.SetColumn(CampoPeso, 0);
                Grid.SetColumnSpan(CampoPeso, 2);
            }

            CamposGrid.InvalidateMeasure();
        }

        private void AjustarAcciones(
            double anchoPagina)
        {
            if (AccionesGrid == null ||
                GuardarButton == null ||
                CancelarButton == null ||
                RegresarButton == null)
            {
                return;
            }

            double anchoUtil =
                FormularioContainer.Width > 0
                    ? FormularioContainer.Width
                    : anchoPagina;

            bool compacto =
                anchoUtil < 520;

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
