using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class albumFotosPage : ContentPage
    {
        private readonly AlbumFotosViewModel viewModel = new();

        public albumFotosPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }
    }
}
