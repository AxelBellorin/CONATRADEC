using CONATRADEC.ViewModels;
using Microsoft.Maui.Storage;

namespace CONATRADEC.Views
{
    public partial class publicacionFormPage : ContentPage, IQueryAttributable
    {
        private readonly PublicacionFormViewModel viewModel = new();
        private int publicacionId;

        public publicacionFormPage()
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
                publicacionId = Math.Max(0, id);
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();

            bool puedeAcceder = publicacionId > 0
                ? viewModel.CanEdit
                : viewModel.CanAdd;

            ContenidoPrincipal.IsVisible = puedeAcceder;
            ContenidoSinPermiso.IsVisible = !puedeAcceder;

            if (puedeAcceder)
                await viewModel.InicializarAsync(publicacionId);
        }

        private async void OnSeleccionarPortadaClicked(
            object sender,
            EventArgs e)
        {
            if (viewModel.IsBusy)
                return;

            try
            {
                FileResult? archivo = await FilePicker.Default.PickAsync(
                    new PickOptions
                    {
                        PickerTitle = "Seleccione la portada",
                        FileTypes = FilePickerFileType.Images
                    });

                if (archivo != null)
                    await viewModel.SeleccionarPortadaAsync(archivo);
            }
            catch
            {
                await DisplayAlert(
                    "Imagen",
                    "No fue posible abrir el selector de imágenes.",
                    "Aceptar");
            }
        }
    }
}
