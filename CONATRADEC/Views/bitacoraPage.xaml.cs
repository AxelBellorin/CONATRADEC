using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class bitacoraPage : ContentPage
    {
        private readonly BitacoraViewModel viewModel = new();

        public bitacoraPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
            viewModel.SolicitarScrollInicio += ViewModel_SolicitarScrollInicio;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            AjustarDiseno(Width);

            // InicializarAsync no vuelve a consultar al regresar desde detalle.
            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarDiseno(width);
        }

        private void AjustarDiseno(double width)
        {
            if (width <= 0)
                return;

            if (ContenidoBitacora != null)
            {
                ContenidoBitacora.Padding = width < 600
                    ? new Thickness(12, 12, 12, 10)
                    : width < 900
                        ? new Thickness(18, 16, 18, 12)
                        : new Thickness(24, 20, 24, 14);
            }

            if (PaginacionBitacora != null)
            {
                double paddingHorizontal = width < 600
                    ? 24
                    : width < 900
                        ? 36
                        : 48;

                PaginacionBitacora.WidthRequest = Math.Min(
                    620,
                    Math.Max(0, width - paddingHorizontal));
            }
        }

        private async void ViewModel_SolicitarScrollInicio(
            object? sender,
            EventArgs e)
        {
            if (RegistrosCollectionView == null ||
                viewModel.Registros.Count == 0)
            {
                return;
            }

            // Permite que CollectionView materialice la página nueva antes de
            // posicionar el primer registro al inicio visible.
            await Task.Delay(60);

            RegistrosCollectionView.ScrollTo(
                0,
                position: ScrollToPosition.Start,
                animate: false);
        }
    }
}
