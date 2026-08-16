using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class propietarioTerrenosPage : ContentPage
    {
        private readonly PropietarioTerrenosViewModel viewModel;
        private int paginaSelectorAntesCambio = -1;

        public propietarioTerrenosPage()
        {
            InitializeComponent();

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

            viewModel =
                new PropietarioTerrenosViewModel();

            BindingContext =
                viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            Shell.SetNavBarIsVisible(
                this,
                false);

            AplicarDiseno(Width);

            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarCarga();
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
            if (viewModel.RegresarCommand.CanExecute(null))
                viewModel.RegresarCommand.Execute(null);

            return true;
        }

        private void AplicarDiseno(
            double width)
        {
            if (width <= 0)
                return;

            ContenidoTerrenosPropietario.Padding =
                width < 600
                    ? new Thickness(12, 12, 12, 20)
                    : width < 900
                        ? new Thickness(18, 16, 18, 24)
                        : new Thickness(24, 20, 24, 28);

            AplicarBusquedaSelector();
            AplicarPaginacionSelector();
        }

        private void AplicarBusquedaSelector()
        {
            if (BusquedaSelectorGrid == null)
                return;

            double ancho =
                SelectorContainer?.Width > 0
                    ? SelectorContainer.Width
                    : Width;

            if (ancho <= 0)
                return;

            bool compacto =
                ancho < 520;

            BusquedaSelectorGrid.ColumnDefinitions.Clear();
            BusquedaSelectorGrid.RowDefinitions.Clear();

            if (compacto)
            {
                BusquedaSelectorGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        GridLength.Star));

                BusquedaSelectorGrid.RowDefinitions.Add(
                    new RowDefinition(
                        GridLength.Auto));
                BusquedaSelectorGrid.RowDefinitions.Add(
                    new RowDefinition(
                        GridLength.Auto));

                Grid.SetRow(
                    BuscarSelectorButton,
                    1);
                Grid.SetColumn(
                    BuscarSelectorButton,
                    0);

                BuscarSelectorButton.HorizontalOptions =
                    LayoutOptions.Fill;
            }
            else
            {
                BusquedaSelectorGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        GridLength.Star));
                BusquedaSelectorGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        GridLength.Auto));

                BusquedaSelectorGrid.RowDefinitions.Add(
                    new RowDefinition(
                        GridLength.Auto));

                Grid.SetRow(
                    BuscarSelectorButton,
                    0);
                Grid.SetColumn(
                    BuscarSelectorButton,
                    1);
            }

            BuscarSelectorButton.MinimumWidthRequest = 0;
        }

        private void AplicarPaginacionSelector()
        {
            if (PaginacionSelectorPropietarios == null)
                return;

            double ancho =
                SelectorContainer?.Width > 0
                    ? SelectorContainer.Width
                    : Width;

            if (ancho <= 0)
                return;

            PaginacionSelectorPropietarios.WidthRequest =
                Math.Min(
                    520,
                    Math.Max(
                        0,
                        ancho - 28));
        }

        private void PaginacionSelector_Pressed(
            object? sender,
            EventArgs e)
        {
            paginaSelectorAntesCambio =
                viewModel.PaginaActualPropietarios;
        }

        private async void PaginacionSelector_Clicked(
            object? sender,
            EventArgs e)
        {
            int paginaOrigen =
                paginaSelectorAntesCambio > 0
                    ? paginaSelectorAntesCambio
                    : viewModel.PaginaActualPropietarios;

            bool operacionDetectada = false;

            for (int intento = 0;
                 intento < 240;
                 intento++)
            {
                if (viewModel.IsBusy ||
                    viewModel.PaginaActualPropietarios !=
                        paginaOrigen)
                {
                    operacionDetectada = true;
                }

                if (operacionDetectada &&
                    !viewModel.IsBusy)
                {
                    if (viewModel.PaginaActualPropietarios !=
                            paginaOrigen &&
                        viewModel.PropietariosDestino.Count > 0)
                    {
                        await DesplazarSelectorAlInicioAsync();
                    }

                    paginaSelectorAntesCambio = -1;
                    return;
                }

                await Task.Delay(50);
            }

            paginaSelectorAntesCambio = -1;
        }

        private async Task DesplazarSelectorAlInicioAsync()
        {
            if (PropietariosDestinoCollection == null ||
                viewModel.PropietariosDestino.Count == 0)
            {
                return;
            }

            await Task.Delay(60);

            PropietariosDestinoCollection.ScrollTo(
                0,
                position: ScrollToPosition.Start,
                animate: false);
        }
    }
}
