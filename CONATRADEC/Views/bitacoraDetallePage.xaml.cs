using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class bitacoraDetallePage : ContentPage, IQueryAttributable
    {
        private readonly BitacoraDetalleViewModel viewModel = new();

        public bitacoraDetallePage()
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            if (!query.TryGetValue("BitacoraId", out object? valor))
                return;

            Guid bitacoraId = valor switch
            {
                Guid id => id,
                string texto when Guid.TryParse(texto, out Guid id) => id,
                _ => Guid.Empty
            };

            if (bitacoraId != Guid.Empty)
                _ = viewModel.CargarAsync(bitacoraId);
        }
    }
}
