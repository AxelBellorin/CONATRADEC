using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;

namespace CONATRADEC.Views
{
    public partial class matrizPermisosPage : ContentPage
    {
        private readonly MatrizPermisosViewModel viewModel = new();

        private bool anchoCompactoActual;

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

            AjustarDistribucion(Width, Height);

            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarOperaciones();

            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);

            AjustarDistribucion(width, height);
        }

        /// <summary>
        /// La matriz prioriza el listado de permisos.
        ///
        /// En teléfono se limita más la altura del panel superior para
        /// dejar mayor espacio visible al listado de permisos. Tablet y
        /// escritorio conservan exactamente la distribución estable actual.
        /// </summary>
        private void AjustarDistribucion(
            double width,
            double height)
        {
            if (width <= 0 ||
                height <= 0 ||
                PanelSuperiorScroll == null)
            {
                return;
            }

            bool esTelefono =
                DeviceInfo.Current.Idiom ==
                DeviceIdiom.Phone;

            bool alturaCompacta =
                height < 760;

            double porcentajeSuperior;
            double minimoSuperior;
            double maximoSuperior;

            if (esTelefono)
            {
                /*
                 * En teléfono el panel superior funciona como una zona
                 * auxiliar desplazable. Se limita su altura para priorizar
                 * la matriz de permisos, que es el contenido principal.
                 */
                porcentajeSuperior = 0.28;
                minimoSuperior = 150;
                maximoSuperior = 220;
            }
            else
            {
                /*
                 * Tablet y Windows conservan la distribución estable que
                 * ya funciona correctamente en pantallas amplias.
                 */
                porcentajeSuperior =
                    alturaCompacta
                        ? 0.34
                        : 0.42;

                minimoSuperior =
                    alturaCompacta
                        ? 175
                        : 245;

                maximoSuperior =
                    alturaCompacta
                        ? 250
                        : 390;
            }

            PanelSuperiorScroll.MaximumHeightRequest =
                Math.Clamp(
                    height * porcentajeSuperior,
                    minimoSuperior,
                    maximoSuperior);

            PermisosList.MinimumHeightRequest =
                esTelefono
                    ? 220
                    : alturaCompacta
                        ? 230
                        : 290;

            bool anchoCompacto =
                width < 720;

            if (anchoCompactoActual == anchoCompacto)
                return;

            anchoCompactoActual = anchoCompacto;

            BotonGuardar.Text =
                anchoCompacto
                    ? "Guardar"
                    : "Guardar cambios";

            BotonRevertir.Padding =
                anchoCompacto
                    ? new Thickness(11, 8)
                    : new Thickness(16, 10);

            BotonGuardar.Padding =
                anchoCompacto
                    ? new Thickness(12, 8)
                    : new Thickness(18, 10);

            AccionesInferioresGrid.Padding =
                alturaCompacta
                    ? new Thickness(0, 1)
                    : new Thickness(0, 4);
        }
    }
}