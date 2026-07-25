using CONATRADEC.Models;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Pais), "Pais")]
    [QueryProperty(nameof(TitlePage), "TitlePage")]
    public partial class departamentoPage : ContentPage
    {
        private readonly DepartamentoViewModel
            viewModel = new();

        private bool paginaVisible;
        private bool permisosCargados;
        private int cantidadColumnasActual;

        public departamentoPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;
        }

        public string TitlePage
        {
            set =>
                viewModel.TitlePage =
                    value;
        }

        public PaisRequest Pais
        {
            set
            {
                viewModel.PaisRequest =
                    value ??
                    new PaisRequest();

                if (paginaVisible &&
                    permisosCargados)
                {
                    _ = IntentarInicializarAsync();
                }
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            paginaVisible = true;

            viewModel.ActualizarPermisos();
            permisosCargados = true;

            AjustarCantidadColumnas(Width);

            await IntentarInicializarAsync();
        }

        protected override void OnDisappearing()
        {
            paginaVisible = false;

            viewModel.CancelarCarga();

            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(
                width,
                height);

            AjustarCantidadColumnas(width);
        }

        private async Task IntentarInicializarAsync()
        {
            if (!paginaVisible ||
                !permisosCargados ||
                !viewModel.PaisValido)
            {
                return;
            }

            await viewModel.InicializarAsync();
        }

        private void AjustarCantidadColumnas(
            double width)
        {
            if (width <= 0 ||
                DepartamentosGridLayout == null)
            {
                return;
            }

            int nuevasColumnas =
                width >= 1280
                    ? 3
                    : width >= 760
                        ? 2
                        : 1;

            if (cantidadColumnasActual ==
                nuevasColumnas)
            {
                return;
            }

            cantidadColumnasActual =
                nuevasColumnas;

            DepartamentosGridLayout.Span =
                nuevasColumnas;
        }
    }
}
