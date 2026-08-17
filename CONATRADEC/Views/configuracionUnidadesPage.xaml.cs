using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class configuracionUnidadesPage :
        ContentPage
    {
        private const double AnchoMaximoContenido =
            1120d;

        private const string ClaveVisita =
            "configuracionUnidadesPage";

        private readonly ConfiguracionUnidadesViewModel
            viewModel = new();

        private bool navegacionShellSuscrita;
        private bool salidaExternaPendiente;

        public configuracionUnidadesPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext =
                viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            salidaExternaPendiente = false;
            SuscribirNavegacionShell();
            AjustarDisenoResponsivo();

            /*
             * La misma instancia puede volver a recibir OnAppearing durante
             * una visita. Solo el primer ingreso de una visita fuerza datos
             * frescos; mientras el usuario permanezca en este módulo se
             * conserva el estado ya cargado.
             */
            bool nuevaVisita =
                InterfazVisitaCacheService
                    .AsegurarVisita(
                        ClaveVisita);

            await viewModel
                .InicializarAsync(
                    forzarRecarga: nuevaVisita);

            /*
             * La carga puede cambiar el tamaño deseado de Pickers, tarjetas y
             * mensajes. El ajuste se hace inmediatamente con el ancho real,
             * sin esperas artificiales ni tareas diferidas.
             */
            AjustarDisenoResponsivo();

            if (viewModel.CanView)
                return;

            InterfazVisitaCacheService
                .FinalizarVisita(
                    ClaveVisita);

            await DisplayAlert(
                "Acceso denegado",
                "No tiene permisos para consultar la configuración de unidades y conversiones.",
                "Aceptar");

            await Shell.Current
                .GoToAsync("..");
        }

        protected override void OnDisappearing()
        {
            /*
             * Esta interfaz no tiene páginas hijas. Una navegación Shell desde
             * ella representa una salida real del módulo; la visita se libera
             * para que el siguiente ingreso consulte nuevamente al servidor.
             */
            if (salidaExternaPendiente)
            {
                InterfazVisitaCacheService
                    .FinalizarVisita(
                        ClaveVisita);

                salidaExternaPendiente = false;
            }

            DesuscribirNavegacionShell();
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(
                width,
                height);

            AjustarDisenoResponsivo();
        }

        private void ContenidoScroll_SizeChanged(
            object? sender,
            EventArgs e)
        {
            AjustarDisenoResponsivo();
        }

        private void AjustarDisenoResponsivo()
        {
            AjustarPaddingContenido();
            AjustarAnchoContenido();
        }

        private void AjustarPaddingContenido()
        {
            if (ContenidoScroll?.Parent is not Grid contenedor)
                return;

            double anchoVentana =
                Width;

            if (anchoVentana <= 0)
                return;

            /*
             * OnIdiom sigue considerando Desktop a una ventana WinUI angosta.
             * El padding responde al ancho real para mantener espacio útil en
             * Windows redimensionado, tablet y móvil.
             */
            Thickness paddingObjetivo =
                anchoVentana < 600
                    ? new Thickness(
                        12,
                        12,
                        12,
                        20)
                    : anchoVentana < 900
                        ? new Thickness(
                            18,
                            16,
                            18,
                            24)
                        : new Thickness(
                            26,
                            22,
                            26,
                            30);

            Thickness actual =
                contenedor.Padding;

            if (Math.Abs(
                    actual.Left -
                    paddingObjetivo.Left) < 0.5 &&
                Math.Abs(
                    actual.Top -
                    paddingObjetivo.Top) < 0.5 &&
                Math.Abs(
                    actual.Right -
                    paddingObjetivo.Right) < 0.5 &&
                Math.Abs(
                    actual.Bottom -
                    paddingObjetivo.Bottom) < 0.5)
            {
                return;
            }

            contenedor.Padding =
                paddingObjetivo;
        }

        private void AjustarAnchoContenido()
        {
            if (ContenidoScroll == null ||
                ContenidoPrincipal == null)
            {
                return;
            }

            double anchoDisponible =
                ContenidoScroll.Width;

            if (anchoDisponible <= 0)
                return;

            double anchoObjetivo =
                Math.Min(
                    AnchoMaximoContenido,
                    anchoDisponible);

            if (Math.Abs(
                    ContenidoPrincipal.WidthRequest -
                    anchoObjetivo) < 0.5)
            {
                return;
            }

            ContenidoPrincipal.WidthRequest =
                anchoObjetivo;

            ContenidoPrincipal.MaximumWidthRequest =
                AnchoMaximoContenido;

            ContenidoPrincipal.HorizontalOptions =
                LayoutOptions.Center;

            ContenidoPrincipal.InvalidateMeasure();
        }

        private void SuscribirNavegacionShell()
        {
            if (navegacionShellSuscrita ||
                Shell.Current == null)
            {
                return;
            }

            Shell.Current.Navigating +=
                Shell_Navigating;

            navegacionShellSuscrita = true;
        }

        private void DesuscribirNavegacionShell()
        {
            if (!navegacionShellSuscrita ||
                Shell.Current == null)
            {
                return;
            }

            Shell.Current.Navigating -=
                Shell_Navigating;

            navegacionShellSuscrita = false;
        }

        private void Shell_Navigating(
            object? sender,
            ShellNavigatingEventArgs e)
        {
            /*
             * No existen subrutas internas en Unidades y conversiones.
             * Cualquier navegación Shell iniciada mientras la página está
             * activa termina la visita actual.
             */
            salidaExternaPendiente = true;
        }
    }
}
