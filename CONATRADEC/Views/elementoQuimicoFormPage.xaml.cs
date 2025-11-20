namespace CONATRADEC.Views
{
    using static CONATRADEC.Models.FormMode;
    using CONATRADEC.Models;
    using CONATRADEC.ViewModels;

    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(ElementoQuimico), "ElementoQuimico")]
    public partial class elementoQuimicoFormPage : ContentPage
    {
        private ElementoQuimicoFormViewModel viewModel = new ElementoQuimicoFormViewModel();
        private ElementoQuimicoRequest elementoRQ;

        public FormModeSelect Mode
        {
            set => viewModel.Mode = value;
        }

        public ElementoQuimicoRequest ElementoQuimico
        {
            get => elementoRQ;
            set => viewModel.ElementoQuimico = value;
        }

        public elementoQuimicoFormPage()
        {
            try
            {
                Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
                BindingContext = viewModel;
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
