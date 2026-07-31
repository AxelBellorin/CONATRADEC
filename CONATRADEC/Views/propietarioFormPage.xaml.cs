using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class propietarioFormPage : ContentPage
    {
        public propietarioFormPage()
        {
            InitializeComponent();
            BindingContext = new PropietarioFormViewModel();
        }
    }
}
