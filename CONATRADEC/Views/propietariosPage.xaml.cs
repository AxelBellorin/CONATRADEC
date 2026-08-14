using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class propietariosPage : ContentPage
    {
        private PropietariosViewModel viewModel;
        private bool paginaMostrada;

        public propietariosPage()
        {
            InitializeComponent();

            Shell.SetNavBarIsVisible(this, false);
            Shell.SetBackButtonBehavior(
                this,
                new BackButtonBehavior
                {
                    IsVisible = false,
                    IsEnabled = false
                });

            viewModel = new PropietariosViewModel();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetNavBarIsVisible(this, false);

            /*
             * En administración, al volver de crear/editar un propietario se
             * reconstruye el listado desde cero. En modo selector se conserva
             * únicamente el contexto ModoSeleccion requerido por el terreno.
             */
            if (paginaMostrada)
            {
                string? modoSeleccion =
                    viewModel.ModoSeleccionTexto;

                viewModel.CancelarCarga();
                viewModel = new PropietariosViewModel
                {
                    ModoSeleccionTexto = modoSeleccion
                };
                BindingContext = viewModel;
            }
            else
            {
                paginaMostrada = true;
            }

            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }

        protected override bool OnBackButtonPressed()
        {
            if (viewModel.RegresarCommand.CanExecute(null))
                viewModel.RegresarCommand.Execute(null);

            return true;
        }
    }
}
