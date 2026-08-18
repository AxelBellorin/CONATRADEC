using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class DiagnosticoIAConfiguracionPage : ContentPage
    {
        private readonly DiagnosticoIAConfiguracionViewModel viewModel;
        private bool? distribucionCompacta;

        public DiagnosticoIAConfiguracionPage()
        {
            InitializeComponent();
            viewModel = new DiagnosticoIAConfiguracionViewModel();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.InicializarAsync();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (width <= 0)
                return;

            bool compacta = width < 700;
            if (distribucionCompacta == compacta)
                return;

            distribucionCompacta = compacta;
            AplicarDistribucionTipos(compacta);
        }

        private void AplicarDistribucionTipos(bool compacta)
        {
            TiposFotografiaGrid.RowDefinitions.Clear();
            TiposFotografiaGrid.ColumnDefinitions.Clear();

            if (compacta)
            {
                TiposFotografiaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                TiposFotografiaGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                TiposFotografiaGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(TiposFotografiaTexto, 0);
                Grid.SetColumn(TiposFotografiaTexto, 0);
                Grid.SetRow(AdministrarTiposButton, 1);
                Grid.SetColumn(AdministrarTiposButton, 0);

                AdministrarTiposButton.WidthRequest = -1;
                AdministrarTiposButton.HorizontalOptions = LayoutOptions.Fill;
            }
            else
            {
                TiposFotografiaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                TiposFotografiaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                TiposFotografiaGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(TiposFotografiaTexto, 0);
                Grid.SetColumn(TiposFotografiaTexto, 0);
                Grid.SetRow(AdministrarTiposButton, 0);
                Grid.SetColumn(AdministrarTiposButton, 1);

                AdministrarTiposButton.WidthRequest = 190;
                AdministrarTiposButton.HorizontalOptions = LayoutOptions.End;
            }
        }
    }
}
