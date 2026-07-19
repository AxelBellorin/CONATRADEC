using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui;

namespace CONATRADEC.Views
{
    public partial class NuevoAnalisisFormPage : ContentPage
    {
        private readonly NuevoAnalisisFormEdicionViewModel viewModel = new();

        public NuevoAnalisisFormPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (viewModel.EsModoEdicion)
            {
                viewModel.LoadPagePermissions("MainPage");

                if (!viewModel.CanView || !viewModel.CanEdit)
                {
                    await DisplayAlert(
                        "Acceso denegado",
                        "No tiene permisos para editar análisis de suelo.",
                        "Aceptar");

                    AnalisisEdicionService.Instance.Limpiar();
                    await Shell.Current.GoToAsync("//MainPage");
                    return;
                }
            }
            else
            {
                if (!PermissionService.Instance
                        .HasRead("NuevoAnalisisFormPage"))
                {
                    await DisplayAlert(
                        "Acceso denegado",
                        "No tiene permisos para ver el formulario de análisis de suelo.",
                        "Aceptar");

                    await Shell.Current.GoToAsync("//MainPage");
                    return;
                }

                viewModel.LoadPagePermissions("NuevoAnalisisFormPage");
            }

            await viewModel.InicializarPaginaAsync(true);

            Button? botonEnviar = BuscarBotonEnviar(this);

            if (botonEnviar != null)
                botonEnviar.Text = viewModel.TextoAccionFormulario;
        }

        private static Button? BuscarBotonEnviar(
            IVisualTreeElement elemento)
        {
            if (elemento is Button boton &&
                (string.Equals(
                     boton.Text,
                     "Enviar Análisis",
                     StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(
                     boton.Text,
                     "Continuar actualización",
                     StringComparison.OrdinalIgnoreCase)))
            {
                return boton;
            }

            foreach (IVisualTreeElement hijo
                     in elemento.GetVisualChildren())
            {
                Button? encontrado = BuscarBotonEnviar(hijo);

                if (encontrado != null)
                    return encontrado;
            }

            return null;
        }
    }
}
