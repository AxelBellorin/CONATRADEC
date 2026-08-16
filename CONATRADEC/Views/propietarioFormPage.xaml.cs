using CONATRADEC.ViewModels;
using System.ComponentModel;

namespace CONATRADEC.Views
{
    public partial class propietarioFormPage : ContentPage
    {
        private const double AnchoDosColumnas = 720;

        private readonly PropietarioFormViewModel viewModel;
        private bool? usandoDosColumnas;

        public propietarioFormPage()
        {
            InitializeComponent();

            viewModel =
                new PropietarioFormViewModel();

            BindingContext =
                viewModel;

            viewModel.PropertyChanged +=
                ViewModel_PropertyChanged;

            OcultarNavegacionNativa();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            OcultarNavegacionNativa();
            AplicarDiseno(Width);

            /*
             * Valida el paquete completo de navegación antes de permitir que
             * View/Edit interactúen con el formulario.
             */
            await viewModel.ValidarNavegacionAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(
                width,
                height);

            AplicarDiseno(width);
        }

        protected override bool OnBackButtonPressed()
        {
            if (viewModel.CancelarCommand.CanExecute(null))
                viewModel.CancelarCommand.Execute(null);

            return true;
        }

        private void ViewModel_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName ==
                    nameof(
                        PropietarioFormViewModel
                            .ShowSaveButton) ||
                e.PropertyName ==
                    nameof(
                        PropietarioFormViewModel
                            .Mode))
            {
                Dispatcher.Dispatch(
                    () => AplicarAcciones(
                        usandoDosColumnas == true));
            }
        }

        private void AplicarDiseno(
            double width)
        {
            if (width <= 0)
                return;

            AjustarPadding(width);

            double anchoFormulario =
                FormularioContainer?.Width > 0
                    ? FormularioContainer.Width
                    : ObtenerAnchoUtil(width);

            bool dosColumnas =
                anchoFormulario >=
                AnchoDosColumnas;

            if (usandoDosColumnas !=
                dosColumnas)
            {
                usandoDosColumnas =
                    dosColumnas;

                AplicarCampos(
                    dosColumnas);
            }

            AplicarAcciones(
                dosColumnas);
        }

        private void AjustarPadding(
            double width)
        {
            if (ContenidoFormulario == null)
                return;

            ContenidoFormulario.Padding =
                width < 600
                    ? new Thickness(
                        12,
                        12,
                        12,
                        22)
                    : width < 900
                        ? new Thickness(
                            20,
                            18,
                            20,
                            26)
                        : new Thickness(
                            28,
                            22,
                            28,
                            30);
        }

        private static double ObtenerAnchoUtil(
            double width)
        {
            double paddingHorizontal =
                width < 600
                    ? 24
                    : width < 900
                        ? 40
                        : 56;

            return Math.Min(
                960,
                Math.Max(
                    0,
                    width -
                    paddingHorizontal));
        }

        private void AplicarCampos(
            bool dosColumnas)
        {
            CamposGrid.ColumnDefinitions.Clear();
            CamposGrid.RowDefinitions.Clear();

            if (dosColumnas)
            {
                CamposGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        GridLength.Star));
                CamposGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        GridLength.Star));

                CamposGrid.RowDefinitions.Add(
                    new RowDefinition(
                        GridLength.Auto));
                CamposGrid.RowDefinitions.Add(
                    new RowDefinition(
                        GridLength.Auto));
                CamposGrid.RowDefinitions.Add(
                    new RowDefinition(
                        GridLength.Auto));

                Posicionar(
                    CampoIdentificacion,
                    0,
                    0);

                Posicionar(
                    CampoNombre,
                    0,
                    1);

                Posicionar(
                    CampoTelefono,
                    1,
                    0);

                Posicionar(
                    CampoCorreo,
                    1,
                    1);

                Posicionar(
                    CampoDireccion,
                    2,
                    0,
                    2);
            }
            else
            {
                CamposGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        GridLength.Star));

                for (int i = 0;
                     i < 5;
                     i++)
                {
                    CamposGrid.RowDefinitions.Add(
                        new RowDefinition(
                            GridLength.Auto));
                }

                Posicionar(
                    CampoIdentificacion,
                    0,
                    0);

                Posicionar(
                    CampoNombre,
                    1,
                    0);

                Posicionar(
                    CampoTelefono,
                    2,
                    0);

                Posicionar(
                    CampoCorreo,
                    3,
                    0);

                Posicionar(
                    CampoDireccion,
                    4,
                    0);
            }

            CamposGrid.InvalidateMeasure();
        }

        private void AplicarAcciones(
            bool dosColumnas)
        {
            if (AccionesGrid == null)
                return;

            AccionesGrid.ColumnDefinitions.Clear();
            AccionesGrid.RowDefinitions.Clear();

            bool mostrarGuardar =
                viewModel.ShowSaveButton;

            if (dosColumnas &&
                mostrarGuardar)
            {
                AccionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        GridLength.Star));
                AccionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        GridLength.Star));

                AccionesGrid.RowDefinitions.Add(
                    new RowDefinition(
                        GridLength.Auto));

                Posicionar(
                    GuardarButton,
                    0,
                    0);

                Posicionar(
                    RegresarButton,
                    0,
                    1);
            }
            else
            {
                AccionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        GridLength.Star));

                if (mostrarGuardar)
                {
                    AccionesGrid.RowDefinitions.Add(
                        new RowDefinition(
                            GridLength.Auto));
                    AccionesGrid.RowDefinitions.Add(
                        new RowDefinition(
                            GridLength.Auto));

                    Posicionar(
                        GuardarButton,
                        0,
                        0);

                    Posicionar(
                        RegresarButton,
                        1,
                        0);
                }
                else
                {
                    AccionesGrid.RowDefinitions.Add(
                        new RowDefinition(
                            GridLength.Auto));

                    Posicionar(
                        RegresarButton,
                        0,
                        0);
                }
            }

            GuardarButton.MinimumWidthRequest = 0;
            RegresarButton.MinimumWidthRequest = 0;
            GuardarButton.HorizontalOptions =
                LayoutOptions.Fill;
            RegresarButton.HorizontalOptions =
                LayoutOptions.Fill;

            AccionesGrid.InvalidateMeasure();
        }

        private static void Posicionar(
            View view,
            int fila,
            int columna,
            int columnSpan = 1)
        {
            Grid.SetRow(
                view,
                fila);

            Grid.SetColumn(
                view,
                columna);

            Grid.SetColumnSpan(
                view,
                Math.Max(
                    1,
                    columnSpan));
        }

        private void OcultarNavegacionNativa()
        {
            Shell.SetNavBarIsVisible(
                this,
                false);

            Shell.SetBackButtonBehavior(
                this,
                new BackButtonBehavior
                {
                    IsVisible = false,
                    IsEnabled = false
                });
        }
    }
}
