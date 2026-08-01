using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class propietariosPage : ContentPage
    {
        private readonly PropietariosViewModel viewModel;

        public propietariosPage()
        {
            InitializeComponent();

            /*
             * La ruta es secundaria, pero la pantalla ya posee su propio botón
             * de catálogo. Se oculta también por código la barra nativa para
             * cubrir WinUI después de reconstruir el Shell por inactividad.
             */
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

            viewModel =
                new PropietariosViewModel();

            BindingContext =
                viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            Shell.SetNavBarIsVisible(
                this,
                false);

            await viewModel.InicializarAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            /*
             * Atrás del sistema usa la misma salida del botón visible.
             */
            if (viewModel.RegresarCommand.CanExecute(null))
            {
                viewModel.RegresarCommand.Execute(null);
            }

            return true;
        }
    }
}
