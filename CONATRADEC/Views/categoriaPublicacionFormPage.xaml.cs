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
            int categoriaId = 0;

            if (query.TryGetValue(
                    "CategoriaId",
                    out object? valorId))
            {
                categoriaId = ConvertirId(valorId);
            }
            else if (query.TryGetValue(
                         "Categoria",
                         out object? valorCategoria) &&
                     valorCategoria is
                         CategoriaPublicacionCatalogoResponse categoria)
            {
                /*
                 * Compatibilidad con navegaciones anteriores que enviaban el
                 * DTO completo. Solo se conserva su identificador; la edición
                 * siempre vuelve a consultar el registro fresco en la API.
                 */
                categoriaId = categoria.CategoriaPublicacionId;
            }

            viewModel.Preparar(categoriaId);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();

            bool tienePermiso = viewModel.PuedeAcceder;
            ContenidoPrincipal.IsVisible = tienePermiso;
            ContenidoSinPermiso.IsVisible = !tienePermiso;

            if (!tienePermiso)
                return;

            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }

        private static int ConvertirId(object? valor)
        {
            if (valor is int id)
                return id;

            return int.TryParse(
                    valor?.ToString(),
                    out int convertido)
                ? convertido
                : 0;
        }
    }
}
