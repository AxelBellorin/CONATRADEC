using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class MotivoDevolucionTecnicoFormPage :
        ContentPage,
        IQueryAttributable
    {
        private readonly MotivoDevolucionTecnicoFormViewModel viewModel = new();
        private bool? distribucionCompacta;

        public MotivoDevolucionTecnicoFormPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            int id = 0;
            if (query.TryGetValue("id", out object? valor))
                int.TryParse(valor?.ToString(), out id);

            viewModel.AplicarId(id);
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

            bool compacta = width < 620;
            if (distribucionCompacta == compacta)
                return;

            distribucionCompacta = compacta;
            AplicarDistribucionIdentificacion(compacta);
            AplicarDistribucionPie(compacta);
        }

        private void AplicarDistribucionIdentificacion(bool compacta)
        {
            IdentificacionGrid.RowDefinitions.Clear();
            IdentificacionGrid.ColumnDefinitions.Clear();

            if (compacta)
            {
                IdentificacionGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                IdentificacionGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                IdentificacionGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(CodigoStack, 0);
                Grid.SetColumn(CodigoStack, 0);
                Grid.SetRow(OrdenStack, 1);
                Grid.SetColumn(OrdenStack, 0);
            }
            else
            {
                IdentificacionGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                IdentificacionGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(new GridLength(170)));
                IdentificacionGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(CodigoStack, 0);
                Grid.SetColumn(CodigoStack, 0);
                Grid.SetRow(OrdenStack, 0);
                Grid.SetColumn(OrdenStack, 1);
            }
        }

        private void AplicarDistribucionPie(bool compacta)
        {
            PieAccionesGrid.RowDefinitions.Clear();
            PieAccionesGrid.ColumnDefinitions.Clear();

            if (compacta)
            {
                PieAccionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                PieAccionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                PieAccionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(CamposObligatoriosLabel, 0);
                Grid.SetColumn(CamposObligatoriosLabel, 0);
                Grid.SetRow(GuardarButton, 1);
                Grid.SetColumn(GuardarButton, 0);
                GuardarButton.MinimumWidthRequest = 0;
                GuardarButton.HorizontalOptions = LayoutOptions.Fill;
            }
            else
            {
                PieAccionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                PieAccionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                PieAccionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(CamposObligatoriosLabel, 0);
                Grid.SetColumn(CamposObligatoriosLabel, 0);
                Grid.SetRow(GuardarButton, 0);
                Grid.SetColumn(GuardarButton, 1);
                GuardarButton.MinimumWidthRequest = 170;
                GuardarButton.HorizontalOptions = LayoutOptions.End;
            }
        }
    }
}
