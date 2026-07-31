using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class propietariosPage : ContentPage
    {
        private readonly PropietariosViewModel viewModel;

        public propietariosPage()
        {
            InitializeComponent();

            viewModel = new PropietariosViewModel();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.InicializarAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            /*
             * El botón Atrás del sistema debe ejecutar la misma salida
             * determinista que el botón visible. Así no se recorren las copias
             * antiguas de Propietarios que hubieran quedado en la pila.
             */
            if (viewModel.RegresarCommand.CanExecute(null))
                viewModel.RegresarCommand.Execute(null);

            return true;
        }
    }
}
