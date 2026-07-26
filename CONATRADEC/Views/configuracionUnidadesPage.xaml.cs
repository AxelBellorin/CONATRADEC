using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class configuracionUnidadesPage :
        ContentPage
    {
        private const double AnchoMaximoContenido =
            1120d;

        private readonly ConfiguracionUnidadesViewModel
            viewModel = new();

        public configuracionUnidadesPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext =
                viewModel;

            Loaded += Pagina_Loaded;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            ProgramarAjusteAncho();

            await viewModel
                .InicializarAsync();

            /*
             * La carga de elementos y unidades cambia el tamaño deseado de
             * varios controles. Se vuelve a aplicar el ancho para impedir que
             * la primera tarjeta conserve una medición menor.
             */
            ProgramarAjusteAncho();

            if (viewModel.CanView)
                return;

            await DisplayAlert(
                "Acceso denegado",
                "No tiene permisos para consultar la configuración de unidades y conversiones.",
                "Aceptar");

            await Shell.Current
                .GoToAsync("..");
        }

        private void Pagina_Loaded(
            object? sender,
            EventArgs e)
        {
            ProgramarAjusteAncho();
        }

        private void ContenidoScroll_SizeChanged(
            object? sender,
            EventArgs e)
        {
            AjustarAnchoContenido();
        }

        private void ProgramarAjusteAncho()
        {
            Dispatcher.Dispatch(
                AjustarAnchoContenido);

            Dispatcher.DispatchDelayed(
                TimeSpan.FromMilliseconds(80),
                AjustarAnchoContenido);

            Dispatcher.DispatchDelayed(
                TimeSpan.FromMilliseconds(300),
                AjustarAnchoContenido);
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
    }
}
