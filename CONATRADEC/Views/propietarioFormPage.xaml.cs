using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class propietarioFormPage : ContentPage
    {
        private readonly PropietarioFormViewModel viewModel;

        public propietarioFormPage()
        {
            InitializeComponent();

            viewModel = new PropietarioFormViewModel();
            BindingContext = viewModel;

            OcultarNavegacionNativa();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            /*
             * Shell puede reconstruir el comportamiento de regreso cuando
             * termina una navegación dinámica. Se vuelve a ocultar aquí para
             * impedir que Windows o Android muestren la flecha nativa.
             */
            OcultarNavegacionNativa();
        }

        protected override bool OnBackButtonPressed()
        {
            /*
             * El botón Atrás del sistema ejecuta el mismo comando que
             * "← Propietarios" y retira este formulario de la pila.
             */
            if (viewModel.CancelarCommand.CanExecute(null))
                viewModel.CancelarCommand.Execute(null);

            return true;
        }

        private void OcultarNavegacionNativa()
        {
            Shell.SetNavBarIsVisible(
                this,
                false);

            Shell.SetBackButtonBehavior(
                this,
                new BackButtonBehavior
                {
                    IsVisible = false,
                    IsEnabled = false
                });
        }
    }
}
