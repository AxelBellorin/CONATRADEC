using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(Item), "Item")]
    public partial class extraccionNutrienteFormPage : ContentPage
    {
        private readonly ExtraccionNutrienteFormViewModel viewModel = new();

        public FormModeSelect Mode
        {
            set => viewModel.Mode = value;
        }

        public ExtraccionNutrienteRequest Item
        {
            set => viewModel.Item = value ?? new ExtraccionNutrienteRequest();
        }

        public extraccionNutrienteFormPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("extraccionNutrientePage");

            bool denied =
                !viewModel.CanView ||
                (viewModel.Mode == FormModeSelect.Create && !viewModel.CanAdd) ||
                (viewModel.Mode == FormModeSelect.Edit && !viewModel.CanEdit);

            if (denied)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para realizar esta operación.",
                    "Aceptar");

                await Shell.Current.GoToAsync(AppRoutes.ExtraccionNutrientes);
                return;
            }

            AjustarDiseno(Width);
            await viewModel.InitializeAsync();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarDiseno(width);
        }

        private void AjustarDiseno(double anchoPagina)
        {
            if (anchoPagina <= 0 ||
                FormularioContainer == null ||
                CamposPrincipalesGrid == null)
            {
                return;
            }

            double margenHorizontal =
                DeviceInfo.Platform == DevicePlatform.WinUI
                    ? 72
                    : 32;

            double anchoDisponible =
                Math.Max(280, anchoPagina - margenHorizontal);

            FormularioContainer.WidthRequest =
                Math.Min(anchoDisponible, 1100);

            bool disenoAmplio = anchoPagina >= 760;

            CamposPrincipalesGrid.ColumnDefinitions.Clear();
            CamposPrincipalesGrid.RowDefinitions.Clear();

            if (disenoAmplio)
            {
                CamposPrincipalesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                CamposPrincipalesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                CamposPrincipalesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(ElementoSection, 0);
                Grid.SetColumn(ElementoSection, 0);

                Grid.SetRow(CantidadSection, 0);
                Grid.SetColumn(CantidadSection, 1);
                return;
            }

            CamposPrincipalesGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));

            CamposPrincipalesGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            CamposPrincipalesGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Grid.SetRow(ElementoSection, 0);
            Grid.SetColumn(ElementoSection, 0);

            Grid.SetRow(CantidadSection, 1);
            Grid.SetColumn(CantidadSection, 0);
        }
    }
}
