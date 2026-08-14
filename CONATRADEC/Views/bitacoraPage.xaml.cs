using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class bitacoraPage : ContentPage
    {
        private BitacoraViewModel viewModel = new();
        private bool paginaMostrada;

        public bitacoraPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            /*
             * El detalle de bitácora se abre sobre esta página. Al regresar se
             * crea una consulta nueva, incluyendo catálogos de filtros, fechas y
             * primera página de resultados.
             */
            if (paginaMostrada)
            {
                viewModel = new BitacoraViewModel();
                BindingContext = viewModel;
            }
            else
            {
                paginaMostrada = true;
            }

            await viewModel.InicializarAsync();
        }
    }
}
