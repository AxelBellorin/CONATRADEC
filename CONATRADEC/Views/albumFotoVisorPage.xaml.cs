using CONATRADEC.Models;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Fotos), "Fotos")]
    [QueryProperty(
        nameof(FotoSeleccionadaId),
        "FotoSeleccionadaId")]
    [QueryProperty(nameof(TituloAlbum), "TituloAlbum")]
    public partial class albumFotoVisorPage : ContentPage
    {
        private readonly AlbumFotoVisorViewModel viewModel =
            new();

        public List<AlbumFotoResponse>? Fotos
        {
            set => viewModel.EstablecerFotos(value);
        }

        public int FotoSeleccionadaId
        {
            set => viewModel.FotoSeleccionadaId = value;
        }

        public string TituloAlbum
        {
            set => viewModel.TituloAlbum = value;
        }

        public albumFotoVisorPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }
    }
}
