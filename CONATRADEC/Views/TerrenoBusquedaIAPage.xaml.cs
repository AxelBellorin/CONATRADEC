using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class TerrenoBusquedaIAPage : ContentPage
    {
        private readonly TerrenoBusquedaIAViewModel viewModel;

        public TerrenoBusquedaIAPage()
        {
            InitializeComponent();
            viewModel = new TerrenoBusquedaIAViewModel();
            viewModel.PaginaCargada += OnPaginaCargada;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            viewModel.ActivarPagina();
            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarOperaciones();
            base.OnDisappearing();
        }

        private void OnPaginaCargada(object? sender, EventArgs e)
        {
            Dispatcher.Dispatch(() =>
            {
                if (viewModel.Resultados.Count == 0)
                    return;

                TerrenosCollection.ScrollTo(
                    0,
                    position: ScrollToPosition.Start,
                    animate: false);
            });
        }
    }
}
