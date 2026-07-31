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
        }

        protected override bool OnBackButtonPressed()
        {
            /*
             * Regresar retira el formulario actual de la pila. No abre una
             * nueva lista de propietarios.
             */
            if (viewModel.CancelarCommand.CanExecute(null))
                viewModel.CancelarCommand.Execute(null);

            return true;
        }
    }
}
