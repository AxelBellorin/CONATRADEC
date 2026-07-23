using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class noticiaDetallePage : ContentPage, IQueryAttributable
    {
        private readonly NoticiaDetalleViewModel viewModel = new();
        private int publicacionId;

        public noticiaDetallePage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            if (query.TryGetValue("PublicacionId", out object? value) &&
                int.TryParse(value?.ToString(), out int id))
            {
                publicacionId = id;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();
            ContenidoPrincipal.IsVisible = viewModel.CanView;
            ContenidoSinPermiso.IsVisible = !viewModel.CanView;

            if (viewModel.CanView && publicacionId > 0)
                await viewModel.InicializarAsync(publicacionId);
        }
    }
}
