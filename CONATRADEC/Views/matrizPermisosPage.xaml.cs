using CONATRADEC.Models;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class matrizPermisosPage : ContentPage
    {
        private readonly MatrizPermisosViewModel viewModel = new();
        private bool ajustandoSelector;
        private bool navegacionSuscrita;
        private bool protegiendoNavegacion;

        public matrizPermisosPage()
        {
            InitializeComponent();

            BindingContext = viewModel;
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisosPagina();

            if (!viewModel.CanView)
                return;

            SuscribirNavegacion();
            AjustarDistribucion(Width);
            await viewModel.IniciarVisitaAsync();
            SincronizarSelector();
        }

        protected override void OnDisappearing()
        {
            DesuscribirNavegacion();
            viewModel.FinalizarVisita();
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarDistribucion(width);
        }

        /// <summary>
        /// El Picker no cambia directamente el ViewModel porque antes de
        /// abandonar un rol pueden existir permisos pendientes de guardar.
        /// El ViewModel confirma el descarte y solo entonces acepta el cambio.
        /// </summary>
        private async void RolPicker_SelectedIndexChanged(
            object? sender,
            EventArgs e)
        {
            if (ajustandoSelector ||
                sender is not Picker picker)
            {
                return;
            }

            RolResponse? solicitado =
                picker.SelectedItem as RolResponse;

            bool aceptado =
                await viewModel.CambiarRolAsync(solicitado);

            if (!aceptado ||
                picker.SelectedItem != viewModel.RolSeleccionado)
            {
                SincronizarSelector();
            }
        }

        private void SincronizarSelector()
        {
            if (RolPicker == null)
                return;

            ajustandoSelector = true;

            try
            {
                RolPicker.SelectedItem =
                    viewModel.RolSeleccionado;
            }
            finally
            {
                ajustandoSelector = false;
            }
        }

        private void SuscribirNavegacion()
        {
            if (navegacionSuscrita || Shell.Current == null)
                return;

            Shell.Current.Navigating += Shell_Navigating;
            navegacionSuscrita = true;
        }

        private void DesuscribirNavegacion()
        {
            if (!navegacionSuscrita || Shell.Current == null)
                return;

            Shell.Current.Navigating -= Shell_Navigating;
            navegacionSuscrita = false;
        }

        /// <summary>
        /// Protege las salidas iniciadas desde la navegación global incluida en
        /// FooterTemplate. Login y SinPermisos nunca se bloquean porque son
        /// destinos de seguridad y cierre de sesión.
        /// </summary>
        private async void Shell_Navigating(
            object? sender,
            ShellNavigatingEventArgs e)
        {
            if (protegiendoNavegacion ||
                !e.CanCancel ||
                !viewModel.TieneCambiosPendientes ||
                EsNavegacionDeSeguridad(e))
            {
                return;
            }

            var deferral = e.GetDeferral();
            if (deferral == null)
                return;

            protegiendoNavegacion = true;

            try
            {
                bool permitir =
                    await viewModel
                        .ConfirmarDescarteParaNavegacionExternaAsync();

                if (!permitir)
                    e.Cancel();
            }
            finally
            {
                protegiendoNavegacion = false;
                deferral.Complete();
            }
        }

        private static bool EsNavegacionDeSeguridad(
            ShellNavigatingEventArgs e)
        {
            string destino =
                e.Target?.Location?.OriginalString ??
                string.Empty;

            return destino.Contains(
                       "LoginPage",
                       StringComparison.OrdinalIgnoreCase) ||
                   destino.Contains(
                       "SinPermisosPage",
                       StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Intercepta el botón físico de Android y el equivalente de Windows
        /// para no perder silenciosamente cambios pendientes.
        /// </summary>
        protected override bool OnBackButtonPressed()
        {
            _ = RegresarDesdeSistemaAsync();
            return true;
        }

        private async Task RegresarDesdeSistemaAsync()
        {
            await viewModel.IntentarRegresarConfiguracionAsync();
        }

        /// <summary>
        /// La adaptación depende del ancho real disponible. Una ventana Windows
        /// angosta recibe el mismo tratamiento que cualquier pantalla angosta,
        /// sin depender de DeviceIdiom.
        /// </summary>
        private void AjustarDistribucion(double width)
        {
            if (width <= 0 ||
                ContenidoPrincipal == null)
            {
                return;
            }

            bool anchoMuyCompacto = width < 520;
            bool anchoCompacto = width < 820;

            if (anchoMuyCompacto)
            {
                ContenidoPrincipal.Padding =
                    new Thickness(10, 10, 10, 18);

                BotonGuardar.Text = "Guardar";
                BotonGuardar.Padding = new Thickness(10, 8);
                BotonRevertir.Padding = new Thickness(10, 8);
                AccionesInferioresGrid.ColumnSpacing = 7;
            }
            else if (anchoCompacto)
            {
                ContenidoPrincipal.Padding =
                    new Thickness(15, 14, 15, 22);

                BotonGuardar.Text = "Guardar";
                BotonGuardar.Padding = new Thickness(13, 9);
                BotonRevertir.Padding = new Thickness(13, 9);
                AccionesInferioresGrid.ColumnSpacing = 10;
            }
            else
            {
                ContenidoPrincipal.Padding =
                    new Thickness(24, 20, 24, 26);

                BotonGuardar.Text = "Guardar cambios";
                BotonGuardar.Padding = new Thickness(16, 9);
                BotonRevertir.Padding = new Thickness(14, 9);
                AccionesInferioresGrid.ColumnSpacing = 12;
            }

        }
    }
}
