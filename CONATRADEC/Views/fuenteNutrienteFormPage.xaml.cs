using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(Fuente), "Fuente")]
    public partial class fuenteNutrienteFormPage : ContentPage
    {
        private readonly FuenteNutrienteFormViewModel
            viewModel = new();

        public fuenteNutrienteFormPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext =
                viewModel;
        }

        public FormMode.FormModeSelect Mode
        {
            get =>
                viewModel.Mode;

            set =>
                viewModel.Mode =
                    value;
        }

        public FuenteNutrienteRequest Fuente
        {
            get =>
                viewModel.Fuente;

            set =>
                viewModel.Fuente =
                    value ??
                    new FuenteNutrienteRequest();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            AjustarDiseno(
                Width);

            await viewModel.InitializeAsync();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(
                width,
                height);

            AjustarDiseno(
                width);
        }

        private void AjustarDiseno(
            double anchoPagina)
        {
            if (anchoPagina <= 0 ||
                FormularioContainer == null ||
                DatosBasicosGrid == null)
            {
                return;
            }

            double margenHorizontal =
                DeviceInfo.Platform ==
                DevicePlatform.WinUI
                    ? 72
                    : 32;

            double anchoDisponible =
                Math.Max(
                    280,
                    anchoPagina -
                    margenHorizontal);

            FormularioContainer.WidthRequest =
                Math.Min(
                    anchoDisponible,
                    1100);

            bool disenoAmplio =
                anchoPagina >=
                760;

            AjustarDatosBasicos(
                disenoAmplio);
        }

        private void AjustarDatosBasicos(
            bool disenoAmplio)
        {
            DatosBasicosGrid
                .ColumnDefinitions
                .Clear();

            DatosBasicosGrid
                .RowDefinitions
                .Clear();

            if (disenoAmplio)
            {
                DatosBasicosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        new GridLength(
                            2,
                            GridUnitType.Star)));

                DatosBasicosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        new GridLength(
                            1,
                            GridUnitType.Star)));

                DatosBasicosGrid.RowDefinitions.Add(
                    new RowDefinition(
                        GridLength.Auto));

                DatosBasicosGrid.RowDefinitions.Add(
                    new RowDefinition(
                        GridLength.Auto));

                Grid.SetRow(
                    NombreSection,
                    0);

                Grid.SetColumn(
                    NombreSection,
                    0);

                Grid.SetColumnSpan(
                    NombreSection,
                    1);

                Grid.SetRow(
                    PrecioSection,
                    0);

                Grid.SetColumn(
                    PrecioSection,
                    1);

                Grid.SetColumnSpan(
                    PrecioSection,
                    1);

                Grid.SetRow(
                    DescripcionSection,
                    1);

                Grid.SetColumn(
                    DescripcionSection,
                    0);

                Grid.SetColumnSpan(
                    DescripcionSection,
                    2);

                return;
            }

            DatosBasicosGrid.ColumnDefinitions.Add(
                new ColumnDefinition(
                    GridLength.Star));

            DatosBasicosGrid.RowDefinitions.Add(
                new RowDefinition(
                    GridLength.Auto));

            DatosBasicosGrid.RowDefinitions.Add(
                new RowDefinition(
                    GridLength.Auto));

            DatosBasicosGrid.RowDefinitions.Add(
                new RowDefinition(
                    GridLength.Auto));

            Grid.SetRow(
                NombreSection,
                0);

            Grid.SetColumn(
                NombreSection,
                0);

            Grid.SetColumnSpan(
                NombreSection,
                1);

            Grid.SetRow(
                PrecioSection,
                1);

            Grid.SetColumn(
                PrecioSection,
                0);

            Grid.SetColumnSpan(
                PrecioSection,
                1);

            Grid.SetRow(
                DescripcionSection,
                2);

            Grid.SetColumn(
                DescripcionSection,
                0);

            Grid.SetColumnSpan(
                DescripcionSection,
                1);
        }
    }
}
