using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui;

namespace CONATRADEC.Views
{
    public partial class AnalisisGuardadoDetallePage : ContentPage
    {
        private readonly AnalisisGuardadoDetalleViewModel viewModel =
            new();

        private readonly Command editarMismaInterfazCommand;

        public AnalisisGuardadoDetallePage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;

            editarMismaInterfazCommand =
                new Command(async () => await EditarMismaInterfazAsync());

            InitializeComponent();
            BindingContext = viewModel;

            Loaded += AnalisisGuardadoDetallePage_Loaded;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("MainPage");

            if (!viewModel.CanView)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para visualizar análisis.",
                    "Aceptar");

                await Shell.Current.GoToAsync("..");
            }
        }

        private void AnalisisGuardadoDetallePage_Loaded(
            object? sender,
            EventArgs e)
        {
            Button? botonEditar = BuscarBotonEditar(this);

            if (botonEditar != null)
                botonEditar.Command = editarMismaInterfazCommand;
        }

        private async Task EditarMismaInterfazAsync()
        {
            if (viewModel.IsBusy ||
                viewModel.Resumen == null)
            {
                return;
            }

            if (!viewModel.CanEdit)
            {
                await GlobalService.MostrarToastAsync(
                    "No tiene permisos para editar análisis.");
                return;
            }

            try
            {
                viewModel.IsBusy = true;

                (bool success, string message) =
                    await AnalisisEdicionService.Instance.PrepararAsync(
                        viewModel.Resumen.AnalisisSueloCalculoId,
                        viewModel.Resumen);

                if (!success)
                {
                    await DisplayAlert(
                        "No se pudo abrir",
                        message,
                        "Aceptar");

                    return;
                }

                await Shell.Current.GoToAsync(
                    "//NuevoAnalisisFormPage");
            }
            finally
            {
                viewModel.IsBusy = false;
            }
        }

        private static Button? BuscarBotonEditar(
            IVisualTreeElement elemento)
        {
            if (elemento is Button boton &&
                string.Equals(
                    boton.Text,
                    "Editar",
                    StringComparison.OrdinalIgnoreCase))
            {
                return boton;
            }

            foreach (IVisualTreeElement hijo
                     in elemento.GetVisualChildren())
            {
                Button? encontrado =
                    BuscarBotonEditar(hijo);

                if (encontrado != null)
                    return encontrado;
            }

            return null;
        }
    }
}
