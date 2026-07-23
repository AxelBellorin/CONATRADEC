using CONATRADEC.Models;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class categoriaPublicacionFormPage :
        ContentPage,
        IQueryAttributable
    {
        private readonly CategoriaPublicacionFormViewModel viewModel = new();

        public categoriaPublicacionFormPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            CategoriaPublicacionCatalogoResponse? categoria = null;

            if (query.TryGetValue("Categoria", out object? valor) &&
                valor is CategoriaPublicacionCatalogoResponse item)
            {
                categoria = item;
            }

            viewModel.Preparar(categoria);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();

            bool tienePermiso = viewModel.PuedeGuardar;
            ContenidoPrincipal.IsVisible = tienePermiso;
            ContenidoSinPermiso.IsVisible = !tienePermiso;
        }
    }
}
