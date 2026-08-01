using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class propietariosPage : ContentPage
    {
        private readonly PropietariosViewModel viewModel;

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
